# Redis Clone — Architecture Design

**Date:** 2026-04-17  
**Project:** codecrafters-redis-csharp  
**Target scope:** At least stage C (replication + WAIT), possibly streams  
**Language:** C# (.NET 10), async/await throughout  

---

## Overview

Layered architecture with clear single-responsibility boundaries. Each layer only depends on the one below it. All I/O is async. Multiple files — one class/concern per file, except command handlers which are grouped in one file to avoid ceremony.

---

## File Structure

```
src/
  Program.cs                  ← entry point, wires everything together
  Server.cs                   ← TcpListener loop, spawns one Task per client
  ConnectionHandler.cs        ← per-client async read/write loop
  Resp/
    RespValue.cs              ← discriminated union: SimpleString, BulkString, Array, Integer, Error
    RespParser.cs             ← reads bytes from NetworkStream → RespValue
    RespWriter.cs             ← RespValue → bytes back to client
  Commands/
    CommandDispatcher.cs      ← maps command name (string) → handler delegate
    CommandHandlers.cs        ← all handlers in one file (PING, ECHO, SET, GET, CONFIG, INFO, REPLCONF, PSYNC…)
  Store/
    RedisStore.cs             ← ConcurrentDictionary<string, StoreEntry> + expiry logic
    StoreEntry.cs             ← value + optional expiry timestamp
  Replication/
    ReplicationManager.cs     ← master/replica mode, replica list, propagation
    ReplicaConnection.cs      ← wraps a connected replica's stream
```

---

## Data Flow

```
TcpListener.AcceptTcpClientAsync()
  └─ Server spawns: Task.Run(() => connectionHandler.HandleAsync(client))

ConnectionHandler.HandleAsync(client):
  loop:
    bytes ──► RespParser.ParseAsync()       ──► RespValue (e.g. Array["SET","foo","bar"])
    RespValue ──► CommandDispatcher.Dispatch() ──► RespValue (response)
    response ──► RespWriter.WriteAsync()    ──► bytes back to client

    // if master and command mutates state:
    ReplicationManager.PropagateAsync(rawBytes) ──► all replica streams
```

- `ConnectionHandler` owns the lifecycle of one client socket — `await using` ensures cleanup on disconnect
- `CommandDispatcher` is stateless — handlers receive `Store` and `ReplicationManager` as constructor-injected dependencies
- Propagation happens after the response is sent to the client, mirroring real Redis behavior
- Replicas use the same `ConnectionHandler` loop; `ConnectionContext.IsReplica` suppresses sending responses for propagated commands

---

## Key Data Types

### RespValue
```csharp
abstract record RespValue;
record SimpleString(string Value) : RespValue;
record BulkString(string? Value) : RespValue;  // null = RESP null bulk string ($-1\r\n)
record RespInteger(long Value) : RespValue;
record RespArray(RespValue[] Items) : RespValue;
record RespError(string Message) : RespValue;
```

### StoreEntry
```csharp
record StoreEntry(string Value, DateTimeOffset? ExpiresAt)
{
    public bool IsExpired() => ExpiresAt.HasValue && DateTimeOffset.UtcNow > ExpiresAt.Value;
}
```

### ConnectionContext
```csharp
record ConnectionContext(NetworkStream Stream, bool IsReplica);
```
Passed into each handler so commands like `REPLCONF` and `PSYNC` can set replica mode without touching global state.

### ReplicationState
```csharp
record ReplicationState(string Role, string MasterId, long ReplicationOffset);
```

---

## Replication Design

### Master side
- `ReplicationManager` holds a `List<ReplicaConnection>` (one per connected replica)
- When a replica completes the `PSYNC` handshake, `ConnectionHandler` calls `ReplicationManager.AddReplica()`
- Every successful write command calls `ReplicationManager.PropagateAsync(rawRespBytes)` — fans out concurrently to all replica streams
- `ReplicationManager` tracks `ReplicationOffset` (total bytes propagated) for `WAIT` support

### Replica side
- On startup, if `--replicaof` args are present, `Server` connects to master before accepting client connections
- A dedicated background `Task` runs the replication receive loop: reads propagated commands from master and feeds them through `CommandDispatcher` (writes applied silently, no response sent)
- `REPLCONF ACK` sent on demand or periodically

### WAIT command
- Master sends `REPLCONF GETACK *` to all replicas, then waits up to timeout for enough ACK responses
- `ReplicationManager.WaitForReplicasAsync(count, timeout)` handles this with a `TaskCompletionSource` per replica

**Key invariant:** Replicas never respond to propagated commands — `ConnectionContext.IsReplica` gates this inside `ConnectionHandler`.

---

## Error Handling

| Situation | Behavior |
|---|---|
| Malformed RESP | `RespParser` throws `RespProtocolException` → `ConnectionHandler` catches, sends RESP Error, continues loop |
| Unknown command | `CommandDispatcher` returns `RespError("ERR unknown command '...'")` |
| Client disconnect | `IOException` or 0-byte read → `ConnectionHandler` breaks loop, calls `ReplicationManager.RemoveReplica()` in `finally` |
| Expired key on GET | Returns RESP null bulk string (`$-1\r\n`), lazy expiry only (no background sweeper) |
| Startup error (bad args, port busy) | Fail fast in `Program.cs` with clear console message |

Each `ConnectionHandler` Task is independent — one crashing client does not affect others.

---

## Layer Summary

| Layer | File(s) | Responsibility |
|---|---|---|
| Network | `Server.cs` | Accept TCP connections, spawn tasks |
| Connection | `ConnectionHandler.cs` | Per-client async loop, lifecycle |
| Protocol | `Resp/` | Parse and serialize RESP wire format |
| Commands | `Commands/` | Route and execute commands |
| Data | `Store/` | Key-value storage with lazy expiry |
| Replication | `Replication/` | Master/replica state and propagation |

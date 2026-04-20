# Opção C — Arquitetura com Pipeline (Futura)

> **Status:** Não implementada — registrada para referência futura.
> Avaliar quando implementar Pub/Sub, replicação ou suporte a RESP3.

## Motivação

A Opção B (Modular por Domínio) é suficiente para os estágios do CodeCrafters até
streams e comandos complexos. A Opção C passa a ser relevante quando:

- Implementar **Pub/Sub** (`SUBSCRIBE`, `PUBLISH`) — requer estado de conexão por cliente
- Implementar **replicação** (`REPLCONF`, `PSYNC`) — requer propagação de comandos
- Suportar **RESP3** — novo tipo de dados `Map`, `Set`, `Attribute`
- Adicionar **autenticação** (`AUTH`) ou **ACL**
- Precisar de **logging/observabilidade** transversal a todos os comandos

## Conceito: Pipeline de Middlewares

Modela o ciclo de vida de cada request como um pipeline explícito com middlewares,
inspirado no ASP.NET Core `IMiddleware`.

```
bytes recebidos
  → BufferedReader          ← resolve mensagens parciais no buffer TCP
  → RespDeserializer        ← bytes → RedisCommand (object model)
  → AuthMiddleware          ← valida credenciais (se AUTH configurado)
  → LoggingMiddleware       ← loga comando + latência
  → CommandRouter           ← encontra o ICommandHandler pelo nome
  → CommandHandler          ← executa com contexto de conexão
  → RespSerializer          ← RedisResult → bytes
bytes enviados
```

## Estrutura de tipos proposta

```csharp
// Modelo de comando desacoplado de string[]
public sealed record RedisCommand(string Name, IReadOnlyList<string> Args);

// Resultado fortemente tipado — elimina strings RESP hardcoded no handler
public abstract record RedisResult;
public sealed record BulkStringResult(string? Value)  : RedisResult;
public sealed record SimpleStringResult(string Value) : RedisResult;
public sealed record IntegerResult(long Value)         : RedisResult;
public sealed record ErrorResult(string Message)       : RedisResult;
public sealed record ArrayResult(IReadOnlyList<RedisResult> Items) : RedisResult;

// Interface de middleware
public interface IMiddleware
{
    Task<RedisResult> InvokeAsync(RedisCommand cmd, Func<Task<RedisResult>> next);
}

// Contexto de conexão por cliente (necessário para Pub/Sub e subscriptions)
public sealed class ConnectionContext
{
    public Guid ConnectionId { get; } = Guid.NewGuid();
    public bool IsSubscribed { get; set; }
    // ...
}
```

## Vantagens sobre Opção B

| Capacidade | Opção B | Opção C |
|---|---|---|
| Adicionar novo comando | ✅ Nova classe | ✅ Nova classe |
| Logging centralizado | ❌ Precisa tocar cada command | ✅ Um middleware |
| Auth / ACL | ❌ Validação espalhada | ✅ Middleware antes do router |
| Estado por conexão (Pub/Sub) | ❌ Não suportado | ✅ ConnectionContext |
| Suporte a RESP3 | ❌ Só strings | ✅ RedisResult tipado |
| Mensagens parciais no buffer | ❌ Parser atual quebra | ✅ BufferedReader resolve |

## O que precisaria ser feito

1. **Criar `RedisCommand` e `RedisResult`** — substituir `string[]` e strings RESP
2. **Criar `RespDeserializer`** — substitui `RespParser`, suporta todos os tipos RESP
3. **Criar `RespSerializer`** — converte `RedisResult` → bytes (substitui `RespWriter`)
4. **Criar `IMiddleware` e pipeline builder** — similar a `IApplicationBuilder`
5. **Migrar cada `ICommandHandler`** — retornar `RedisResult` em vez de `string`
6. **Criar `BufferedReader`** — acumular bytes de leituras parciais do socket
7. **Atualizar `Program.cs`** — construir e rodar o pipeline

## Estimativa de esforço

- **Migração incremental:** ~3-5 dias (manter Opção B rodando até pipeline estar estável)
- **Risco:** Médio — reescreve camada de protocolo mas preserva lógica de storage e commands

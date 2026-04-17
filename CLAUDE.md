# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a CodeCrafters "Build Your Own Redis" challenge implemented in C# (.NET 10). The goal is to build a Redis clone capable of handling commands like `PING`, `SET`, and `GET` by implementing the Redis Serialization Protocol (RESP).

## Commands

**Build:**
```sh
dotnet build --configuration Release --output /tmp/codecrafters-build-redis-csharp codecrafters-redis.csproj
```

**Run locally:**
```sh
./your_program.sh
```

**Submit to CodeCrafters:**
```sh
git push origin master
```

## Architecture

- Entry point: `src/Program.cs` — all server logic lives here (single-file approach for this challenge)
- The server listens on TCP port `6379` using `TcpListener`
- CodeCrafters compiles via `.codecrafters/compile.sh` and runs via `.codecrafters/run.sh`

## CodeCrafters Workflow

Stages are unlocked progressively. Each push to `master` triggers CodeCrafters to run automated tests against the binary. There are no local test commands — validation happens on CodeCrafters' servers after `git push origin master`.

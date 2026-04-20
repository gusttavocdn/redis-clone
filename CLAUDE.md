# CLAUDE.md

Este arquivo fornece orientações ao Claude Code (claude.ai/code) ao trabalhar com o código neste repositório.

## Visão Geral do Projeto

Este é o desafio "Construa Seu Próprio Redis" do CodeCrafters, implementado em C# (.NET 10). O objetivo é construir um clone do Redis capaz de lidar com comandos como `PING`, `SET` e `GET`, implementando o Protocolo de Serialização do Redis (RESP).

## Comandos

**Build:**
```sh
dotnet build --configuration Release --output /tmp/codecrafters-build-redis-csharp codecrafters-redis.csproj
```

**Executar localmente:**
```sh
./your_program.sh
```

**Rodar testes unitários:**
```sh
dotnet test testes/UnitTests/codecrafters-redis.UnitTests.csproj
```

**Rodar testes de integração:**
```sh
dotnet test testes/IntegrationTests/codecrafters-redis.IntegrationTests.csproj
```

**Rodar todos os testes:**
```sh
dotnet test codecrafters-redis.sln
```

**Enviar para o CodeCrafters:**
```sh
git push origin master
```

## Arquitetura

O projeto segue uma **arquitetura orientada a domínio por feature**, onde cada tipo de dado Redis possui sua própria pasta contendo store e comandos relacionados.

### Estrutura de pastas

```
src/
├── Program.cs                 ← ponto de entrada, bootstrapping mínimo
├── Core/                      ← abstrações compartilhadas e comandos genéricos
│   ├── IRedisStore.cs         ← contrato principal do store
│   ├── RedisStore.cs          ← facade que delega para os stores por tipo
│   ├── Result.cs              ← tipo Result<T> para erros esperados (sem exceções)
│   ├── ICommandHandler.cs     ← contrato de handler de comando
│   ├── CommandDispatcher.cs   ← roteamento de comandos por nome
│   ├── PingCommand.cs
│   ├── EchoCommand.cs
│   └── TypeCommand.cs
├── Protocol/                  ← serialização/deserialização RESP
│   ├── RespParser.cs
│   └── RespWriter.cs
├── Server/                    ← infraestrutura TCP e loop de I/O
│   └── RedisServer.cs
├── Strings/                   ← domínio: tipo string do Redis
│   ├── StringStore.cs
│   ├── SetCommand.cs
│   └── GetCommand.cs
└── Streams/                   ← domínio: tipo stream do Redis
    ├── StreamStore.cs
    ├── StreamEntry.cs
    ├── StreamId.cs
    └── XAddCommand.cs
```

### Regras da arquitetura

- **Novos tipos de dado** (Hash, List, Set, etc.) ganham uma pasta própria no padrão `src/<Tipo>/`, contendo o store e todos os comandos relacionados ao tipo.
- **Comandos sem tipo específico** (ex: `PING`, `ECHO`, `TYPE`) ficam em `src/Core/`.
- **Nenhum namespace adicional** — todo o código usa `namespace codecrafters_redis;`. A organização é por pasta, não por namespace.
- **`RedisStore`** é o único ponto de acesso ao estado — é uma facade que delega para os stores por tipo. Novos stores devem ser registrados nela.
- **`CommandDispatcher`** recebe os handlers via construtor. Novos comandos devem ser adicionados ao array de inicialização em `CommandDispatcher.cs`.
- O servidor escuta na porta TCP `6379` usando `TcpListener`.
- O CodeCrafters compila via `.codecrafters/compile.sh` e executa via `.codecrafters/run.sh`.

## Regras de Desenvolvimento

- **Toda implementação de feature ou mudança de comportamento deve ser acompanhada de testes unitários. E ser feita utilizado TDD.
- Os testes devem cobrir o caminho feliz e os casos de erro relevantes.
- Nenhuma feature é considerada concluída sem testes que validem seu comportamento e garantam que nada do sistema existente foi quebrado.
- **Todos os testes unitários devem usar [FluentAssertions](https://fluentassertions.com/) para asserções** — nunca `Assert.Equal`, `Assert.True` ou similares do xUnit diretamente.
- **Quando houver dependências externas a isolar, usar [NSubstitute](https://nsubstitute.github.io/)** para criação de mocks/stubs — nunca implementações manuais de interfaces falsas.
- **Todas as classes devem ser declaradas como `sealed` por padrão.** Remover o `sealed` apenas quando herança for explicitamente necessária e justificada.

## Testando Localmente no Windows

### 1. Subir o servidor

Abra um terminal PowerShell ou CMD na raiz do projeto:

```powershell
dotnet run --project codecrafters-redis.csproj
```

Você verá: `Redis server listening on port 6379...`

### 2. Testar comandos manualmente

Abra **outro** terminal e use este script PowerShell:

```powershell
$tcp = [System.Net.Sockets.TcpClient]::new("localhost", 6379)
$stream = $tcp.GetStream()
$writer = [System.IO.StreamWriter]::new($stream)
$reader = [System.IO.StreamReader]::new($stream)

# Envia PING
$writer.Write("*1`r`n`$4`r`nPING`r`n")
$writer.Flush()
$reader.ReadLine()   # Resposta esperada: +PONG

$tcp.Close()
```

### 3. Testar com redis-cli via Docker (opcional)

No Windows, usar `host.docker.internal` para acessar o servidor rodando no host:

```powershell
docker run --rm redis redis-cli -h host.docker.internal -p 6379 PING
# PONG

docker run --rm redis redis-cli -h host.docker.internal -p 6379 PING "hello"
# "hello"
```

---

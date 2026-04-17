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

- Ponto de entrada: `src/Program.cs` — toda a lógica do servidor está aqui (abordagem de arquivo único para este desafio)
- O servidor escuta na porta TCP `6379` usando `TcpListener`
- O CodeCrafters compila via `.codecrafters/compile.sh` e executa via `.codecrafters/run.sh`

## Regras de Desenvolvimento

- **Toda implementação de feature ou mudança de comportamento deve ser acompanhada de testes unitários.** Isso inclui novos comandos Redis, alterações em parsing RESP, mudanças na lógica de armazenamento e qualquer outra lógica de negócio.
- Os testes devem cobrir o caminho feliz e os casos de erro relevantes.
- Nenhuma feature é considerada concluída sem testes que validem seu comportamento e garantam que nada do sistema existente foi quebrado.

## Fluxo de Trabalho no CodeCrafters

As etapas são desbloqueadas progressivamente. Cada push para `master` aciona o CodeCrafters para executar testes automatizados contra o binário. Não há comandos de teste locais — a validação acontece nos servidores do CodeCrafters após `git push origin master`.

# RESP — Redis Serialization Protocol

O RESP (Redis Serialization Protocol) é o protocolo de comunicação usado pelo Redis para troca de mensagens entre cliente e servidor. Foi projetado para ser simples de implementar, rápido de parsear e legível como texto puro.

## Características gerais

- Baseado em texto (ASCII)
- Toda mensagem termina com `\r\n` (CRLF)
- O primeiro byte identifica o tipo do dado
- Clientes **sempre** enviam comandos como **Array de Bulk Strings**
- Servidores podem responder com qualquer tipo

---

## Tipos de dados

### Simple String — `+`

Strings sem quebras de linha. Usadas para respostas de sucesso simples.

```
+OK\r\n
+PONG\r\n
```

### Error — `-`

Indica um erro. O conteúdo descreve a mensagem de erro.

```
-ERR unknown command 'FOO'\r\n
-WRONGTYPE Operation against a key holding the wrong kind of value\r\n
```

### Integer — `:`

Número inteiro com sinal.

```
:0\r\n
:1000\r\n
:-1\r\n
```

### Bulk String — `$`

String binária de tamanho arbitrário. O tamanho em bytes é declarado antes do conteúdo.

```
$5\r\nhello\r\n
$0\r\n\r\n       ← string vazia
$-1\r\n          ← null bulk string (chave inexistente)
```

Formato:
```
$<tamanho>\r\n<dados>\r\n
```

### Array — `*`

Lista de elementos RESP. O número de elementos é declarado antes.

```
*0\r\n                         ← array vazio
*2\r\n:1\r\n:2\r\n             ← [1, 2]
*-1\r\n                        ← null array
```

Formato:
```
*<quantidade>\r\n<elemento1><elemento2>...
```

---

## Comandos do cliente

Clientes enviam comandos como arrays de bulk strings. Cada palavra do comando é um elemento do array.

### Exemplo: `PING`

```
*1\r\n
$4\r\n
PING\r\n
```

Resposta do servidor:
```
+PONG\r\n
```

### Exemplo: `SET foo bar`

```
*3\r\n
$3\r\n
SET\r\n
$3\r\n
foo\r\n
$3\r\n
bar\r\n
```

Resposta do servidor:
```
+OK\r\n
```

### Exemplo: `GET foo`

```
*2\r\n
$3\r\n
GET\r\n
$3\r\n
foo\r\n
```

Resposta (chave existe):
```
$3\r\n
bar\r\n
```

Resposta (chave não existe):
```
$-1\r\n
```

---

## Resumo dos prefixos

| Prefixo | Tipo          | Exemplo de uso                    |
|---------|---------------|-----------------------------------|
| `+`     | Simple String | `+OK\r\n`                         |
| `-`     | Error         | `-ERR mensagem\r\n`               |
| `:`     | Integer       | `:42\r\n`                         |
| `$`     | Bulk String   | `$5\r\nhello\r\n`                 |
| `*`     | Array         | `*1\r\n$4\r\nPING\r\n`            |

---

## Valores nulos

| Representação | Significado         |
|---------------|---------------------|
| `$-1\r\n`     | Bulk String nula    |
| `*-1\r\n`     | Array nulo          |

---

## Referências

- [Documentação oficial do RESP](https://redis.io/docs/latest/develop/reference/protocol-spec/)

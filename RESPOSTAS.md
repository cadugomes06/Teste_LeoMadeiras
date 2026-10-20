# Respostas — Partes 1, 3 e 4

## Parte 1 — Questões conceituais

> Até 10 linhas cada. Exemplos práticos do cenário (SVL, CorteCloud, Digibee, API Foundation, ERP) valem mais que definições.

### 1. Síncrono x assíncrono
_Diferença entre chamada HTTP síncrona e integração assíncrona (fila, webhook). Por que quase todas as integrações deste projeto são assíncronas?_

TODO

### 2. Mensagem duplicada
_A Digibee chamou a API Foundation duas vezes com o mesmo pagamento do pedido 1007. O que pode dar errado? O que é um endpoint idempotente e como garantir isso aqui?_

TODO

### 3. Falha no meio do caminho
_A API Foundation salvou o pedido, mas a chamada ao SVL deu timeout. Como evitar que o SVL nunca seja avisado? Estratégia simples._

TODO

### 4. Códigos HTTP
| Situação | Status | Por quê |
|---|---|---|
| Pedido criado com sucesso | TODO | TODO |
| Corpo sem o campo obrigatório `serviceId` | TODO | TODO |
| Token de autenticação ausente | TODO | TODO |
| Pedido informado não existe | TODO | TODO |
| Erro inesperado no banco de dados | TODO | TODO |

### 5. Intermediário (Digibee)
_Uma vantagem e uma desvantagem de ter uma plataforma de integração entre os sistemas._

TODO

---

## Parte 3 — Leitura de código (`ProcessarPagamentoAsync`)

### 3.1 Problemas encontrados
| # | Problema | Impacto em produção | Severidade (alto/médio/baixo) |
|---|---|---|---|
| 1 | TODO | TODO | TODO |
| 2 | TODO | TODO | TODO |
| 3 | TODO | TODO | TODO |
| 4 | TODO | TODO | TODO |
| 5 | TODO | TODO | TODO |

### 3.2 Método corrigido
```csharp
// TODO
```

### 3.3 E se o ERP enviar a mesma notificação de pagamento duas vezes? O código corrigido resolve?
TODO

---

## Parte 4 — SQL e modelagem

> Dialeto utilizado: TODO (ex.: SQL Server)

### 4.1 Quantidade de pedidos por `TipoPedido` criados em setembro de 2026
```sql
-- TODO
```

### 4.2 Pedidos `PAGO` há mais de 2 horas sem nenhuma notificação `CONCLUIDO` ("travados")
```sql
-- TODO
```

### 4.3 As 5 lojas com mais notificações em `FALHA`
```sql
-- TODO
```

### 4.4 Dois pedidos com o mesmo `PedidoSVL` é um problema? Que alteração faria?
TODO

### 4.5 Índices para a consulta 2 (roda a cada 5 min, milhões de linhas)
```sql
-- TODO
```

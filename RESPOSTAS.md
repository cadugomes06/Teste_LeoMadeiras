# Respostas — Partes 1, 3 e 4

## Parte 1 — Questões conceituais

> Até 10 linhas cada. Exemplos práticos do cenário (SVL, CorteCloud, Digibee, API Foundation, ERP) valem mais que definições.

### 1. Síncrono x assíncrono
_Diferença entre chamada HTTP síncrona e integração assíncrona (fila, webhook). Por que quase todas as integrações deste projeto são assíncronas?_

R: Chamadas síncronas são chamadas realizadas de uma aplicação para outra, onde ambas precisam estar disponíveis ao mesmo tempo para funcionar, a aplicação que faz a chamada fica aguardando até a outra responder, caso ocorra algum problema na chamada ou com a aplicação que recebeu a requisição, a requisição e o processamento são interrompidos. A requisição assíncrona não precisa aguardar o processamento imediato, ela apenas entrega a mensagem e o processamento pode ser feito depois, ela pode ficar guardada (em uma fila, por exemplo) e processada, ou tentada de novo, quando o destino voltar. O modelo de processamento assíncrono também evita sobrecarga entre os sistemas, uma vez que quem chama não precisa aguardar a resposta e quem recebe escolhe quando irá processar. Nessa aplicação é muito importante que requisições sejam feitas de forma assíncrona, porque o CorteCloud pode demorar para gerar os arquivos, levando muito tempo para uma resposta, o ERP só confirma o pagamento quando o cliente paga, sendo um evento disparado por um gatilho. Portanto o ideal é que os sistemas façam suas requisições de forma independente, cobrindo indisponibilidade ou falhas entre os sistemas.

### 2. Mensagem duplicada
_A Digibee chamou a API Foundation duas vezes com o mesmo pagamento do pedido 1007. O que pode dar errado? O que é um endpoint idempotente e como garantir isso aqui?_

R: Considerando o cenário apresentado, onde o ERP envia a notificação de pagamento para a API Foundation (pela Digibee, como diz a pergunta) do mesmo serviço duas vezes, pode acontecer da API Foundation enviar o serviço para a produção duas vezes, podendo causar um desperdício de trabalho e material na operação ou criar um controle de registro duplicado. Idempotência é processar a mesma mensagem uma vez ou mais e sempre levar ao mesmo resultado. Para garantir a idempotência você pode ter uma chave que identifica o evento como PedidoSVL e antes de processar verificar se o pagamento desse evento já foi tratado, caso sim, responder sucesso e não repetir os efeitos. Pode-se criar um índice único no banco de dados para essa chave, quando houver duas requisições simultâneas, a segunda será barrada mesmo que ambas tenham passado pela verificação.

### 3. Falha no meio do caminho
_A API Foundation salvou o pedido, mas a chamada ao SVL deu timeout. Como evitar que o SVL nunca seja avisado? Estratégia simples._

R: Para resolver esse problema, pode-se salvar, na mesma transação do pedido, um registro indicando que o pedido precisa ser enviado ao SVL (status pendente), dessa forma garantimos a atomicidade do processamento (padrão outbox). Posteriormente o envio pode ser feito ao SVL por um worker ou fila, buscando os registros pendentes de tempos em tempos e tentando o envio, podendo adicionar intervalos crescentes, se der certo, o status muda para concluído, se der timeout ou outro erro, continua pendente e soma 1 tentativa (até limite pré-estabelecido), limite atingido, mudar status para "falhou". 

### 4. Códigos HTTP
| Situação | Status | Por quê |
|---|---|---|
| Pedido criado com sucesso | 201 | 200 é genérico e pode ser usado para qualquer processamento, enquanto 201 poderia retornar com a url do pedido criado por exemplo. |
| Corpo sem o campo obrigatório `serviceId` | 400 (badrequest)| Quando o erro é de quem chamou, não respeitou o contrato estabelecido. |
| Token de autenticação ausente | 401, não autorizado | O cliente não se identificou. |
| Pedido informado não existe | 404 (não encontrado) | O recurso pedido não existe.
| Erro inesperado no banco de dados | 500 erro de processamento | Falha do servidor e não de quem chamou. |

### 5. Intermediário (Digibee)
_Uma vantagem e uma desvantagem de ter uma plataforma de integração entre os sistemas._

R: A vantagem é que você não precisa atender aos critérios de implementação da outra aplicação, nem ela a sua, ambas se adaptam ao padrão de comunicação da Digibee. A desvantagem na minha opinião é não conseguir garantir 100% que os dados estão consistentes ou atualizados, se a Digibee cair por exemplo, nenhum pedido chega ao CorteCloud, mesmo que a API Foundation e o CorteCloud estejam disponíveis, ter um intermediário também implica em latência extra, cada mensagem passa por mais camadas, a rastreabilidade pode ser mais complicada já que é preciso consultar os logs de vários sistemas.

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

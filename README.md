# API Foundation — recebimento de arquivos de produção

Simulação da **API Foundation** da Leo Madeiras recebendo da **Digibee** os links dos arquivos de produção gerados
pelo **CorteCloud** (Parte 2 do teste técnico). As respostas das Partes 1, 3 e 4 estão em [`RESPOSTAS.md`](RESPOSTAS.md).

O que a API faz:

- salva os links recebidos de cada serviço, **sem duplicar** nada quando a mesma mensagem chega de novo;
- pedidos **ZLPL/ZCVS**: "copia" cada arquivo novo para o armazenamento interno (implementação falsa, só registra em log);
- pedidos **ZMOB**: não copia nada e **notifica a LeomobAPI** (falsa, falha de propósito numa parte das chamadas),
  com controle de status `INICIAL → CONCLUIDO | PENDENTE → … → FALHA` e retentativas.

## Como rodar

Pré-requisito: **.NET 8** (SDK 8 ou mais novo para compilar e **runtime ASP.NET Core 8** para executar).

```bash
dotnet run --project src/LeoMadeiras.ApiFoundation.Api
```

A API sobe em `http://localhost:5041` com o **Swagger** em <http://localhost:5041/swagger>. O banco SQLite
(`apifoundation.db`) é criado na primeira execução já com os pedidos do enunciado:

| pedidoSVL | serviceId | tipoPedido |
|---|---|---|
| 1007 | 21763653 | ZLPL |
| 1008 | 21763654 | ZCVS |
| 1012 | 21763699 | ZMOB |

Para zerar os dados, apague `src/LeoMadeiras.ApiFoundation.Api/apifoundation.db*` (o banco é recriado ao subir).

### Com Docker

```bash
docker compose up --build
```

API em `http://localhost:8080` (Swagger em <http://localhost:8080/swagger>). O banco fica no volume
`apifoundation-dados`; `docker compose down -v` apaga os dados.

### Testes

```bash
dotnet test
```

51 testes: de serviço (regras de negócio com SQLite **em memória**, que aplica índices únicos e transações de
verdade) e de integração (a API inteira via `WebApplicationFactory`). Os cenários obrigatórios:

| Cenário obrigatório | Teste |
|---|---|
| Pedido ZMOB não aciona o armazenamento | `ArquivosProducaoServiceTests.Pedido_ZMOB_salva_os_links_mas_nao_aciona_o_armazenamento` |
| Pedido ZLPL aciona o armazenamento uma vez por arquivo | `ArquivosProducaoServiceTests.Pedidos_ZLPL_e_ZCVS_acionam_o_armazenamento_uma_vez_por_arquivo` |
| Envio repetido não duplica links nem notificações | `ArquivosProducaoServiceTests.Envio_repetido_de_pedido_ZMOB_nao_duplica_links_nem_notificacoes` (e `Envio_repetido_nao_duplica_links_nem_copias`) |
| Notificação vai para FALHA após a 3ª tentativa sem sucesso | `NotificacoesEndpointTests.Notificacao_ZMOB_vai_para_FALHA_apos_a_terceira_tentativa_sem_sucesso` (ponta a ponta via HTTP) |
| Pedido inexistente não impede os demais | `ArquivosProducaoServiceTests.Envio_com_um_pedido_inexistente_processa_os_demais_normalmente` |

## Endpoints

| Método e rota | O que faz | Respostas |
|---|---|---|
| `POST /api/arquivos-producao` | Recebe os links de um ou mais serviços | 200 (resumo por serviço) · 400 (fora do contrato) · 422 (`hasErrors = true`) |
| `POST /api/notificacoes/reprocessar` | Tenta de novo as notificações pendentes (o mesmo que o worker faz sozinho) | 200 (resumo) |
| `GET /api/pedidos/{pedidoSVL}` | Pedido, links recebidos e status da notificação mais recente | 200 · 400 · 404 |

Erros saem sempre em [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`);
erro inesperado vira 500 sem detalhes internos na resposta (o detalhe vai para o log).

### Exemplos

```bash
# Recebimento (o pedido 9999 não existe: vira falha do item e os demais são processados)
curl -X POST http://localhost:5041/api/arquivos-producao -H "Content-Type: application/json" -d '{
  "documentType": "filesToProduction",
  "hasErrors": false,
  "services": [
    { "serviceId": 21763653, "pedidoSVL": 1007,
      "links": { "corte": { "pdfSumario": ["https://arquivos.exemplo.com/1007/sumario.pdf"],
                            "pdfDetalhado": ["https://arquivos.exemplo.com/1007/detalhado.pdf"] },
                 "etiquetas": ["https://arquivos.exemplo.com/1007/etiqueta-1.png"],
                 "usinagem": ["https://arquivos.exemplo.com/1007/usinagem.xml"] } },
    { "serviceId": 21763699, "pedidoSVL": 1012,
      "links": { "etiquetas": ["https://arquivos.exemplo.com/1012/etiqueta-1.png"] } },
    { "serviceId": 1, "pedidoSVL": 9999, "links": {} }
  ]
}'
```

```json
{
  "processados": [
    { "pedidoSVL": 1007, "serviceId": 21763653, "tipoPedido": "ZLPL", "linksNovos": 4, "notificacao": null },
    { "pedidoSVL": 1012, "serviceId": 21763699, "tipoPedido": "ZMOB", "linksNovos": 1, "notificacao": "PENDENTE" }
  ],
  "falhas": [
    { "pedidoSVL": 9999, "serviceId": 1, "motivo": "Pedido 9999 não encontrado." }
  ]
}
```

Reenviar o mesmo corpo devolve `linksNovos: 0` e nenhuma cópia ou notificação nova.

```bash
curl -X POST http://localhost:5041/api/notificacoes/reprocessar
# {"reprocessadas":1,"concluidas":0,"pendentes":1,"falhas":0}

curl http://localhost:5041/api/pedidos/1012
```

Em Development a LeomobAPI falsa falha em 50% das chamadas (`LeomobFake:TaxaFalha` no
`appsettings.Development.json`; 0 = nunca falha, 1 = sempre falha), então `notificacao` pode vir `CONCLUIDO` ou
`PENDENTE`.

## Como está organizado

Clean Architecture em 4 projetos; o compilador impede dependência na direção errada
(`Api → Application/Infrastructure`, `Infrastructure → Application → Domain`):

```
src/
  LeoMadeiras.ApiFoundation.Domain/          entidades (Pedido, LinkArquivo, NotificacaoLeomob) e enums
  LeoMadeiras.ApiFoundation.Application/     regras de negócio (serviços), DTOs e interfaces (repositórios,
                                             IArmazenamentoArquivos, ILeomobApiClient) — sem EF nem HTTP
  LeoMadeiras.ApiFoundation.Infrastructure/  EF Core + SQLite, repositórios, implementações falsas
  LeoMadeiras.ApiFoundation.Api/             controllers finos, worker de reprocessamento, Program.cs
tests/LeoMadeiras.ApiFoundation.Tests/       xUnit (serviço + integração)
```

### Como as regras foram atendidas

1. **`hasErrors = true`** → 422, nada é processado.
2. **Pedido inexistente** → entra em `falhas` e os demais serviços seguem: cada serviço é processado isoladamente
   (um `try/catch` por serviço; um erro inesperado também vira falha só daquele item).
3. **`serviceId` divergente** → falha do item, nada é gravado.
4. **ZLPL/ZCVS** → `IArmazenamentoArquivos.CopiarAsync` uma vez por link **novo**.
5. **ZMOB** → não copia; salva os links e notifica a LeomobAPI.
6. **Notificação** → o registro `INICIAL` é gravado **na mesma transação dos links**, antes da chamada; sucesso vira
   `CONCLUIDO`, falha vira `PENDENTE` (+1 tentativa, erro guardado). O reprocessamento manda para `FALHA` na
   3ª tentativa sem sucesso.
7. **Idempotência** → chave natural `(pedido, categoria, url)` com **índice único** no banco. Antes de gravar, os
   links que o pedido já tem são descartados; só os novos são copiados/notificados, então um reenvio não tem efeito.
   Se dois envios iguais chegam **ao mesmo tempo**, o índice único barra o segundo, que vira falha do item
   ("reenvie para confirmar"). Um índice único filtrado garante no máximo uma notificação em aberto por pedido.

**Bônus**: Swagger, Docker Compose, logs estruturados (`PedidoSVL` em escopo em toda mensagem do processamento de
um pedido; em JSON fora de Development e em texto de uma linha em Development, que é o modo do `dotnet run` e do
compose) e `BackgroundService` que reprocessa as notificações sozinho a cada 30 s (o endpoint
manual continua existindo).

## Premissas

- **Sucesso parcial = 200** com a lista de `processados` e `falhas` (em vez de 207 Multi-Status, pouco suportado
  por plataformas de integração). Cabe à Digibee reenviar os itens que voltaram em `falhas`. Se o banco estiver
  fora do ar, todos os itens voltam em `falhas` (com 200).
- **O contrato é validado antes do `hasErrors`**: corpo fora do contrato é 400 mesmo com `hasErrors = true`.
  `serviceId`/`pedidoSVL` ausentes, lista vazia, item nulo e URL que não seja http(s) absoluta (até 2048 caracteres)
  são 400. `documentType` não é validado.
- **URLs são comparadas exatamente** (só com `Trim`); caminhos de URL diferenciam maiúsculas de minúsculas.
- **Mesmo `pedidoSVL` repetido num envio** é processado em sequência: aparece duas vezes em `processados`, a segunda
  com `linksNovos = 0`.
- **A cópia acontece antes de gravar os links**: se ela falha, nada é gravado e o reenvio copia de novo. A cópia é
  "pelo menos uma vez" (após uma falha um arquivo pode ser copiado de novo, sobrescrevendo o mesmo destino).
- **A LeomobAPI sempre recebe todos os links do pedido**, então a primeira tentativa e as retentativas enviam o mesmo
  conteúdo. Link novo num pedido ZMOB cria notificação nova só se não houver uma em aberto (`INICIAL`/`PENDENTE`);
  se houver, a retentativa dela já leva o link novo.
- **Contagem de tentativas**: toda chamada à LeomobAPI conta (a do recebimento é a 1ª). Falha no recebimento +
  2 reprocessamentos sem sucesso = `FALHA`.
- **Notificação parada em `INICIAL` por mais de 5 minutos** (o processo caiu no meio da tentativa) também é
  reprocessada. O limite precisa ser maior que o timeout de um cliente HTTP real da LeomobAPI.
- **Uma rodada de reprocessamento por vez** neste processo (worker e endpoint compartilham uma trava). Com várias
  instâncias da API seria preciso reservar a notificação no banco (ver melhorias).
- **Depois de gravado o `INICIAL`, a tentativa vai até o fim** mesmo se o cliente desistir da requisição, para a
  notificação não ficar parada.
- **`GET /api/pedidos/{pedidoSVL}`** devolve a notificação mais recente e expõe `ultimoErro` de propósito (endpoint
  de apoio interno, útil para suporte). Datas sempre em UTC.
- **Sem autenticação** (fora do escopo do enunciado).

## Decisões técnicas

| Decisão | Alternativa | Por quê |
|---|---|---|
| SQLite (arquivo) e SQLite **em memória** nos testes | Provider InMemory do EF; PostgreSQL | O InMemory do EF não aplica índice único nem transação, então testaria a idempotência "de mentira". Postgres exigiria infraestrutura para rodar |
| Idempotência por chave natural + índice único | Header `Idempotency-Key`; hash do payload | A Digibee não manda chave; a chave natural cobre também reenvios parciais e o índice protege contra concorrência |
| Chamada à LeomobAPI dentro da requisição, com `INICIAL` gravado antes | Só gravar e deixar o worker enviar | O enunciado pede INICIAL → chamada → status; o registro garante que o worker recupere falhas (padrão outbox simplificado) |
| `EnsureCreated()` no startup | Migrations | Um comando para rodar; migrations ficam como melhoria |
| Repositórios na Application, implementados com EF na Infrastructure | Expor o `DbContext` à Application | Application sem dependência de EF; a violação de índice único é traduzida para `RegistroDuplicadoException` na Infrastructure |

## O que eu melhoraria com mais tempo

- **Migrations** no lugar de `EnsureCreated` (hoje, mudar o modelo exige apagar o banco).
- **Reservar a notificação no banco** antes de reprocessar (`UPDATE … WHERE Status = 'PENDENTE'`) ou token de
  concorrência, para rodar várias instâncias da API/worker sem notificar duas vezes; processar em **lotes**.
- **Isolar falhas por notificação** no reprocessamento (hoje um erro de banco interrompe a rodada).
- **Clientes reais** de armazenamento e LeomobAPI com `HttpClient`, timeout curto e política de retry/circuit breaker
  (Polly), e status por arquivo copiado.
- **Autenticação** entre Digibee e API Foundation (ex.: token/OAuth via API gateway).
- **Unit of work** explícito (hoje os repositórios compartilham o `DbContext` da requisição).
- Carregar todos os pedidos de um envio numa só consulta e copiar arquivos em paralelo (com limite).
- Não registrar URLs completas em log (em produção podem ser pré-assinadas).
- Health checks, métricas e rastreamento distribuído (OpenTelemetry).

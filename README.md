# oficina-billing-service

Microsserviço de **Orçamento e Pagamento** da Oficina Mecânica Wagyu — Tech
Challenge Fase 4. Consome eventos do `oficina-os-service` para gerar
cobranças via **Mercado Pago** (sandbox) e publica o resultado do pagamento
de volta para a Saga.

Ver [ADR-001](docs/adr/ADR-001-banco-nosql-e-gateway-pagamento.md) para a
justificativa do banco NoSQL e da integração de pagamento.

## Responsabilidades

- Consumir `OrcamentoEnviadoEvent` (publicado pelo OS Service) e criar a
  cobrança + link de pagamento Mercado Pago
- Receber o webhook de confirmação/rejeição de pagamento do Mercado Pago
- Publicar `PagamentoAprovadoEvent` / `PagamentoRejeitadoEvent` para o OS
  Service avançar ou compensar a Saga
- Consumir `OrcamentoRejeitadoEvent` (cliente rejeitou o orçamento no OS
  Service) para cancelar/estornar a cobrança — compensação da Saga

## Arquitetura

```
src/
  BillingService.Domain/          Orcamento, StatusPagamento, eventos de integração
  BillingService.Application/     Use Cases, IEventPublisher, IMercadoPagoGateway
  BillingService.Infrastructure/  DynamoDB, SNS (publica), SQS (consome), Mercado Pago (HTTP)
  BillingService.API/             Controllers (consulta + webhook), Program.cs
tests/
  BillingService.Tests/           xUnit + FluentAssertions, com dublês de
                                   repositório/gateway/publisher (sem AWS/MP real)
```

## Banco de dados

**DynamoDB** (NoSQL) — banco próprio, não compartilhado com o OS Service
nem com o Execução Service. Nenhum outro serviço acessa esta tabela
diretamente; toda comunicação é via eventos (SNS/SQS).

## Fluxo

```
OS Service: POST /enviar-orcamento → OrcamentoEnviadoEvent ──(SNS/SQS)──┐
                                                                         ▼
                                              Billing: CriarOrcamentoUseCase
                                              (cria Orcamento + preferência MP)
                                                         │
                                      cliente paga via link do Mercado Pago
                                                         │
                                                         ▼
                                   Mercado Pago → POST /webhook/mercadopago
                                                         │
                                      RegistrarPagamentoUseCase
                                      publica PagamentoAprovadoEvent/
                                      PagamentoRejeitadoEvent ──(SNS)──► OS Service
```

Se a OS for cancelada no OS Service (orçamento rejeitado via API), o
`OrcamentoRejeitadoEvent` chega aqui e `CancelarOrcamentoUseCase` estorna a
cobrança pendente — compensação da Saga.

## Rodando localmente

```bash
cd src/BillingService.API
dotnet run
```

Sem `MercadoPago:AccessToken` configurado, o gateway gera um link
simulado — útil para testar o fluxo sem depender da rede do sandbox. Sem
`Messaging:SqsQueueUrl`, o consumidor de eventos fica desligado (logado um
aviso); use o endpoint `POST /api/Orcamentos/simular` para testar o fluxo
de criação de orçamento manualmente via Swagger/Postman.

## Testes

```bash
dotnet test
```

## Deploy

CI/CD em `.github/workflows/ci-cd.yml`, mesmo padrão SSH-para-EC2/K3s do OS
Service, em namespace Kubernetes próprio (`oficina-billing-service`).
Segredos necessários: `EC2_HOST`, `EC2_SSH_KEY`, e quando o quality gate for
ligado, `SONAR_TOKEN`.

## Status

🚧 Scaffolding completo (Dias 4-6 do roadmap da Fase 4): Domain/Application/
Infrastructure/API, testes unitários com dublês, Dockerfile, k8s, CI/CD.
Pendente: provisionar a tabela DynamoDB e a fila SQS reais via Terraform,
obter o Access Token de sandbox do Mercado Pago, e validar o fluxo de ponta
a ponta junto com o OS Service e o Execução Service.

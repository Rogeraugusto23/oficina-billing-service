# ADR-001: DynamoDB como banco e Mercado Pago como gateway de pagamento

## Status
Aceito

## Contexto
A Fase 4 exige que pelo menos um serviço use um banco NoSQL, e que o
Billing Service integre um gateway de pagamento real (Mercado Pago,
ambiente sandbox). O Billing Service modela apenas um agregado simples
(Orcamento: valores, status, referências ao pagamento) sem relacionamentos
complexos ou necessidade de transações multi-tabela — um caso de uso
clássico para um banco de documentos/chave-valor.

## Decisão
- **DynamoDB** como banco do Billing Service: item único por Orçamento,
  chave de partição `Id`. Consultas por `OrdemServicoId` e por
  `MercadoPagoPreferenceId` são feitas hoje via `Scan` (aceitável no volume
  de dados de um ambiente acadêmico); uma evolução para produção criaria
  índices secundários globais (GSI) para essas duas consultas.
- **Mercado Pago (Checkout Pro / API de Preferences, sandbox)** como
  gateway de pagamento: o Billing Service cria uma preferência de pagamento
  por orçamento e expõe o link de checkout (`init_point`); o retorno do
  pagamento chega via webhook (`/webhook/mercadopago`).
- Sem credenciais do Mercado Pago configuradas, o gateway retorna uma
  preferência simulada (ver `MercadoPagoGateway.CriarPreferenciaAsync`),
  permitindo exercitar toda a Saga em ambiente local/CI sem depender da
  rede do sandbox.

## Consequências
- O webhook implementado aqui é uma versão simplificada (recebe o
  resultado diretamente no corpo da notificação, validado por um segredo
  compartilhado). O fluxo real de produção do Mercado Pago envia apenas o
  ID do recurso alterado (`topic`/`id`) e espera que o receptor consulte o
  status via `GET /v1/payments/{id}` — ajuste necessário antes de usar isto
  fora de um ambiente de teste.
- A consulta por `Scan` no DynamoDB não escala para um volume alto de
  orçamentos; aceitável para o escopo do desafio, documentado aqui como
  dívida técnica conhecida.

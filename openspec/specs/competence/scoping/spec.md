## Purpose

Define como o resto do sistema — listagens, totais, orcamento mensal e
materializacao de gastos fixos — passa a delimitar uma competencia pela janela de
datas da regra de virada, em vez de pelo primeiro e ultimo dia do mes.

## Requirements

### Requirement: Janela de datas de uma competencia

O sistema SHALL derivar, para cada competencia, a janela de datas que pertence a
ela, a partir do dia de virada e dos encerramentos do usuario:

- Inicio: a data de encerramento da competencia anterior, quando ela estiver
  encerrada; caso contrario, o dia de virada do mes anterior (ou o primeiro dia
  do mes, quando nao ha dia de virada).
- Fim: o dia anterior a data de encerramento da propria competencia, quando ela
  estiver encerrada; caso contrario, o dia anterior ao dia de virada do proprio
  mes (ou o ultimo dia do mes, quando nao ha dia de virada).

As janelas de competencias consecutivas MUST ser contiguas e MUST NOT se
sobrepor: toda data pertence a exatamente uma competencia, e a janela de uma
competencia MUST conter exatamente as datas que a regra de resolucao classifica
nela.

Uma janela MAY ser vazia quando duas competencias sao encerradas no mesmo dia; a
consulta desta competencia MUST devolver resultado vazio, sem erro.

#### Scenario: Janela com dia de virada

- **WHEN** o dia de virada e 25 e a competencia consultada e 2026-09
- **THEN** a janela vai de 25/08/2026 a 24/09/2026

#### Scenario: Janela sem dia de virada

- **WHEN** o dia de virada e nulo e a competencia consultada e 2026-09
- **THEN** a janela vai de 01/09/2026 a 30/09/2026

#### Scenario: Janela de uma competencia encerrada

- **WHEN** o dia de virada e 25 e a competencia 2026-08 foi encerrada em 18/08/2026
- **THEN** a janela de 2026-08 vai de 25/07/2026 a 17/08/2026
- **AND** a janela de 2026-09 vai de 18/08/2026 a 24/09/2026

#### Scenario: Janelas consecutivas sao contiguas

- **WHEN** qualquer combinacao de dia de virada e encerramentos e aplicada a duas competencias consecutivas
- **THEN** o dia seguinte ao fim da janela da primeira e o inicio da janela da segunda

#### Scenario: Janela vazia

- **WHEN** as competencias 2026-08 e 2026-09 sao encerradas no mesmo dia e a competencia 2026-09 e consultada
- **THEN** a listagem de lancamentos e vazia e os totais sao zero

### Requirement: Listagens e totais do Ledger usam a janela da competencia

As consultas de lancamentos, totais mensais e totais por categoria SHALL
selecionar os lancamentos cuja data de ocorrencia cai dentro da janela da
competencia pedida, e nao entre o primeiro e o ultimo dia do mes do calendario.

#### Scenario: Listagem de lancamentos da competencia

- **WHEN** o dia de virada e 25 e o cliente lista os lancamentos da competencia 2026-09
- **THEN** um lancamento de 28/08/2026 aparece na lista
- **AND** um lancamento de 20/08/2026 nao aparece na lista
- **AND** um lancamento de 26/09/2026 nao aparece na lista

#### Scenario: Totais da competencia

- **WHEN** o dia de virada e 25, existe um gasto de R$ 100 em 28/08/2026 e um gasto de R$ 50 em 20/08/2026
- **THEN** o total de gastos variaveis de 2026-09 e R$ 100
- **AND** o total de gastos variaveis de 2026-08 e R$ 50

#### Scenario: Totais por categoria da competencia

- **WHEN** o dia de virada e 25 e a quebra por categoria de 2026-09 e consultada
- **THEN** ela considera exatamente os lancamentos da janela de 2026-09

#### Scenario: Comportamento preservado sem dia de virada

- **WHEN** o dia de virada e nulo
- **THEN** as listagens e os totais de uma competencia sao identicos aos do mes do calendario correspondente

### Requirement: Orcamento mensal usa a janela da competencia

O calculo de quanto ainda ha para gastar SHALL usar os totais da competencia
delimitada pela janela. Nenhuma outra regra do orcamento muda.

#### Scenario: Orcamento reflete a janela

- **WHEN** o dia de virada e 25 e existe um gasto de R$ 300 em 28/08/2026
- **THEN** esse gasto compoe o consumo do orcamento de 2026-09
- **AND** nao compoe o consumo do orcamento de 2026-08

#### Scenario: Orcamento da competencia corrente

- **WHEN** o dia de virada e 25 e hoje e 28/08/2026
- **THEN** o painel apresenta o orcamento da competencia 2026-09

### Requirement: Materializacao de gastos fixos dentro da janela

A ocorrencia mensal de um gasto fixo SHALL ser gerada com uma data que pertenca a
janela da competencia para a qual ela esta sendo materializada. A data escolhida
e a primeira data dentro da janela cujo dia coincide com o dia de vencimento do
gasto fixo, ajustado ao ultimo dia do mes quando esse dia nao existe. Se nenhuma
data da janela atende, o sistema SHALL usar o ultimo dia da janela.

A chave de idempotencia continua sendo a competencia, de modo que materializar a
mesma competencia mais de uma vez MUST NOT duplicar lancamentos.

#### Scenario: Vencimento dentro do primeiro mes da janela

- **WHEN** o dia de virada e 25, o gasto fixo vence no dia 28 e a competencia materializada e 2026-09
- **THEN** o lancamento gerado tem data 28/08/2026
- **AND** ele pertence a competencia 2026-09

#### Scenario: Vencimento dentro do segundo mes da janela

- **WHEN** o dia de virada e 25, o gasto fixo vence no dia 10 e a competencia materializada e 2026-09
- **THEN** o lancamento gerado tem data 10/09/2026

#### Scenario: Vencimento em dia inexistente no mes

- **WHEN** o dia de virada e nulo, o gasto fixo vence no dia 31 e a competencia materializada e 2026-02
- **THEN** o lancamento gerado tem data 28/02/2026

#### Scenario: Nenhuma data da janela corresponde ao vencimento

- **WHEN** a janela da competencia foi encurtada por um encerramento manual e nenhuma data dela cai no dia de vencimento do gasto fixo
- **THEN** o lancamento gerado tem a data do ultimo dia da janela
- **AND** ele pertence a competencia materializada

#### Scenario: Materializar a mesma competencia duas vezes

- **WHEN** a materializacao da competencia 2026-09 e executada duas vezes
- **THEN** existe exatamente um lancamento por gasto fixo ativo em 2026-09

#### Scenario: Materializacao automatica segue a regra

- **WHEN** o dia de virada e 25, hoje e 28/08/2026 e a rotina automatica de materializacao e executada
- **THEN** ela materializa a competencia 2026-09

### Requirement: Reposicionamento de gastos fixos apos mudanca de regra

A identidade de uma ocorrencia de gasto fixo SHALL ser o par (gasto fixo,
competencia). Quando uma alteracao do dia de virada ou um encerramento manual faz
a data de uma ocorrencia ja materializada sair da janela da sua competencia, uma
nova materializacao dessa competencia SHALL reposicionar a ocorrencia existente
para uma data dentro da janela, e MUST NOT criar uma segunda ocorrencia.

Alterar o dia de virada, encerrar ou reabrir uma competencia SHALL disparar a
rematerializacao da competencia corrente e da anterior, sem acao manual do
usuario.

#### Scenario: Ocorrencia deslocada volta para a janela

- **WHEN** a ocorrencia de 2026-08 de um gasto fixo que vence no dia 28 foi materializada em 28/08/2026 e o usuario passa a usar o dia de virada 25 — o que levaria essa data para a competencia 2026-09
- **THEN** a ocorrencia de 2026-08 passa a ter uma data dentro da janela de 2026-08
- **AND** existe exatamente uma ocorrencia desse gasto fixo em 2026-08
- **AND** nenhuma ocorrencia desse gasto fixo aparece em 2026-09 alem da propria materializacao de 2026-09

#### Scenario: Ocorrencia ja dentro da janela nao e alterada

- **WHEN** a materializacao de uma competencia e executada e a ocorrencia existente ja esta dentro da janela
- **THEN** a data e os demais dados da ocorrencia permanecem inalterados

#### Scenario: Encerramento manual encurta a janela

- **WHEN** a competencia 2026-08 e encerrada em 18/08/2026 e a ocorrencia de um gasto fixo que vence no dia 28 ja havia sido materializada em 28/08/2026 para 2026-08
- **THEN** essa ocorrencia e reposicionada para uma data dentro da nova janela de 2026-08

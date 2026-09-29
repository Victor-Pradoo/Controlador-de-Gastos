## Purpose

Define a qual competencia (mes de referencia) um lancamento pertence, para que o
ciclo do app acompanhe o ciclo financeiro real do usuario — fatura de cartao que
fecha no meio do mes — em vez do mes do calendario.

## ADDED Requirements

### Requirement: Dia de virada da competencia

O sistema SHALL permitir que cada usuario configure um dia de virada
(`ClosingDay`) que define quando a competencia muda. Lancamentos que ocorrem no
dia de virada ou depois dele pertencem a competencia seguinte.

O valor MUST ser um inteiro entre 2 e 31, ou nulo. Nulo significa "sem virada" e
faz a competencia coincidir com o mes do calendario — este e o valor padrao para
qualquer usuario que nunca configurou a regra.

O dia 1 MUST ser rejeitado: com virada no dia 1 todo lancamento cairia na
competencia seguinte, indefinidamente.

Quando o dia configurado nao existe no mes (dia 31 em um mes de 30 dias), o
sistema SHALL usar o ultimo dia daquele mes.

#### Scenario: Usuario configura o dia de virada

- **WHEN** o usuario salva o dia de virada 25
- **THEN** a configuracao e persistida para esse usuario
- **AND** consultar a configuracao devolve 25

#### Scenario: Dia de virada fora do intervalo permitido

- **WHEN** o usuario tenta salvar o dia de virada 0, 1 ou 32
- **THEN** a operacao falha com erro de validacao
- **AND** a configuracao anterior permanece inalterada

#### Scenario: Usuario desliga a virada

- **WHEN** o usuario salva o dia de virada como nulo
- **THEN** a competencia de todo lancamento passa a ser o mes do calendario da data em que ele ocorreu

#### Scenario: Configuracao nunca definida

- **WHEN** um usuario que nunca configurou a regra consulta sua configuracao
- **THEN** o dia de virada devolvido e nulo

### Requirement: Resolucao da competencia de uma data

O sistema SHALL determinar a competencia de um lancamento exclusivamente a partir
da sua data de ocorrencia, do dia de virada do usuario e dos encerramentos
manuais registrados. A competencia MUST NOT ser gravada no lancamento.

A resolucao segue duas etapas, nesta ordem:

1. Competencia base: com dia de virada nulo, e o mes do calendario da data; caso
   contrario, e o mes do calendario da data quando o dia da data e menor que o
   dia de virada, e o mes seguinte quando o dia da data e maior ou igual ao dia
   de virada.
2. Encerramentos: enquanto a competencia obtida estiver encerrada com data de
   encerramento menor ou igual a data do lancamento, avanca-se para a competencia
   seguinte.

A resolucao MUST ser deterministica e independente de quando o lancamento foi
registrado.

#### Scenario: Data antes do dia de virada

- **WHEN** o dia de virada e 25 e um lancamento ocorre em 20/08/2026
- **THEN** a competencia do lancamento e 2026-08

#### Scenario: Data no dia de virada

- **WHEN** o dia de virada e 25 e um lancamento ocorre em 25/08/2026
- **THEN** a competencia do lancamento e 2026-09

#### Scenario: Data depois do dia de virada, no mes seguinte

- **WHEN** o dia de virada e 25 e um lancamento ocorre em 03/09/2026
- **THEN** a competencia do lancamento e 2026-09

#### Scenario: Virada em dezembro atravessa o ano

- **WHEN** o dia de virada e 25 e um lancamento ocorre em 28/12/2026
- **THEN** a competencia do lancamento e 2027-01

#### Scenario: Dia de virada inexistente no mes

- **WHEN** o dia de virada e 31 e um lancamento ocorre em 28/02/2026
- **THEN** a competencia do lancamento e 2026-03

#### Scenario: Sem dia de virada configurado

- **WHEN** o dia de virada e nulo e um lancamento ocorre em 28/08/2026
- **THEN** a competencia do lancamento e 2026-08

#### Scenario: Origem do lancamento nao altera a regra

- **WHEN** o dia de virada e 25 e existem tres lancamentos em 28/08/2026 — um manual, um importado do banco e um gerado por gasto fixo
- **THEN** os tres pertencem a competencia 2026-09

### Requirement: Encerramento manual da competencia corrente

O sistema SHALL permitir que o usuario encerre a competencia corrente antes do
dia de virada. O encerramento registra a competencia encerrada e a data de hoje
como data de encerramento.

A partir dessa data, lancamentos que cairiam na competencia encerrada passam a
cair na competencia seguinte. Lancamentos com data anterior a data de
encerramento MUST continuar na competencia encerrada — encerrar o mes vale para o
que vem a partir de hoje, nao reclassifica o que ja aconteceu.

Somente a competencia corrente MAY ser encerrada.

#### Scenario: Usuario encerra o mes corrente

- **WHEN** hoje e 18/08/2026, a competencia corrente e 2026-08 e o usuario aciona "Encerrar os gastos deste mes"
- **THEN** a competencia 2026-08 fica registrada como encerrada em 18/08/2026
- **AND** a competencia corrente passa a ser 2026-09

#### Scenario: Lancamento apos o encerramento

- **WHEN** a competencia 2026-08 foi encerrada em 18/08/2026 e um lancamento ocorre em 20/08/2026
- **THEN** a competencia do lancamento e 2026-09

#### Scenario: Lancamento retroativo anterior ao encerramento

- **WHEN** a competencia 2026-08 foi encerrada em 18/08/2026 e o usuario registra hoje um lancamento com data 10/08/2026
- **THEN** a competencia do lancamento e 2026-08

#### Scenario: Encerrar uma competencia ja encerrada

- **WHEN** a competencia 2026-08 ja esta encerrada e o usuario tenta encerra-la de novo
- **THEN** a operacao falha com erro de conflito
- **AND** a data de encerramento original e preservada

#### Scenario: Encerrar duas competencias no mesmo dia

- **WHEN** a competencia 2026-08 e encerrada em 18/08/2026 e, no mesmo dia, a competencia 2026-09 tambem e encerrada
- **THEN** a competencia corrente passa a ser 2026-10
- **AND** um lancamento com data 20/08/2026 pertence a competencia 2026-10

### Requirement: Reabertura de uma competencia encerrada

O sistema SHALL permitir desfazer um encerramento. Reabrir uma competencia remove
o seu registro de encerramento, e a resolucao volta a valer apenas pelo dia de
virada e pelos encerramentos restantes.

#### Scenario: Usuario reabre o mes encerrado por engano

- **WHEN** a competencia 2026-08 foi encerrada em 18/08/2026, hoje e 20/08/2026, o dia de virada e 25 e o usuario reabre 2026-08
- **THEN** o encerramento deixa de existir
- **AND** a competencia corrente volta a ser 2026-08
- **AND** um lancamento com data 20/08/2026 volta a pertencer a 2026-08

#### Scenario: Reabrir uma competencia que nao esta encerrada

- **WHEN** o usuario tenta reabrir uma competencia sem encerramento registrado
- **THEN** a operacao falha com erro de nao encontrado

### Requirement: Competencia corrente

O sistema SHALL expor a competencia corrente do usuario como a competencia
resolvida para a data de hoje. Toda leitura que nao informa explicitamente uma
competencia MUST usar a competencia corrente.

Com dia de virada configurado, a competencia corrente SHALL avancar no dia de
virada — e nao na passagem do ultimo dia do mes para o primeiro do mes seguinte.
Nesse momento o ciclo em curso ja comecou dias antes e ja acumula lancamentos,
entao o painel nao aparece zerado na virada do calendario.

#### Scenario: A competencia corrente avanca no dia de virada

- **WHEN** o dia de virada e 25
- **THEN** a competencia corrente em 24/08/2026 e 2026-08
- **AND** a competencia corrente em 25/08/2026 e 2026-09

#### Scenario: A virada do calendario nao muda a competencia corrente

- **WHEN** o dia de virada e 25
- **THEN** a competencia corrente e 2026-09 tanto em 31/08/2026 quanto em 01/09/2026

#### Scenario: Leitura sem competencia informada

- **WHEN** o dia de virada e 25, hoje e 28/08/2026 e o cliente consulta os lancamentos sem informar a competencia
- **THEN** a resposta traz os lancamentos da competencia 2026-09

#### Scenario: Sem dia de virada configurado

- **WHEN** o dia de virada e nulo
- **THEN** a competencia corrente em 31/08/2026 e 2026-08
- **AND** a competencia corrente em 01/09/2026 e 2026-09

### Requirement: Mudanca de regra reclassifica o historico

Como a competencia e derivada e nunca gravada, alterar o dia de virada ou os
encerramentos SHALL reclassificar tambem os lancamentos ja registrados. O sistema
MUST avisar o usuario desse efeito antes de confirmar a alteracao do dia de
virada.

#### Scenario: Alterar o dia de virada move lancamentos ja registrados

- **WHEN** existe um lancamento em 28/08/2026 classificado em 2026-08 (sem dia de virada) e o usuario passa a configurar o dia de virada 25
- **THEN** o lancamento passa a pertencer a competencia 2026-09
- **AND** os totais de 2026-08 e de 2026-09 refletem a nova classificacao

#### Scenario: Aviso antes de alterar a regra

- **WHEN** o usuario altera o dia de virada na tela de configuracoes
- **THEN** a interface informa que a mudanca reclassifica lancamentos ja registrados antes de a alteracao ser salva

### Requirement: Isolamento por usuario

A regra de virada e os encerramentos SHALL ser proprios de cada usuario. A
configuracao ou o encerramento de um usuario MUST NOT afetar a competencia dos
lancamentos de outro.

#### Scenario: Dois usuarios com regras diferentes

- **WHEN** o usuario A tem dia de virada 25, o usuario B nao tem dia de virada, e cada um tem um lancamento em 28/08/2026
- **THEN** o lancamento de A pertence a competencia 2026-09
- **AND** o lancamento de B pertence a competencia 2026-08

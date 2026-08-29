# ADR 0003 — Pluggy como agregador de Open Finance

**Status:** aceito · **Data:** 2026-08-28

## Contexto

O objetivo do MVP é importar gastos direto do banco. No Brasil há dois caminhos:
virar uma instituição participante do Open Finance ou usar um agregador.

## Decisão

Usar a **Pluggy** por trás da porta `IBankDataProvider`. O domínio conhece só a
interface; o adaptador Pluggy é um detalhe substituível.

Acompanha um `FakeBankDataProvider` com extrato sintético determinístico, padrão em
desenvolvimento (`Banking:Pluggy:UseFakeProvider = true`).

## Alternativas consideradas

- **Open Finance direto.** Exige habilitação regulatória, certificados ICP-Brasil,
  registro no Diretório de Participantes e conformidade contínua. Inviável para um
  MVP pessoal.
- **Belvo.** Equivalente em capacidade. Pluggy escolhida pelo foco no mercado
  brasileiro e sandbox mais direto. A porta torna a troca barata.
- **Importação de OFX/CSV.** Zero integração, mas empurra trabalho manual para o
  usuário — que é exatamente o problema que o produto quer resolver.

## Consequências

**A favor:** integração em dias, não meses. As credenciais bancárias ficam no widget
do provedor e **nunca** passam por esta aplicação — o que reduz drasticamente a
superfície de risco.

**Contra:** dependência de terceiro (custo por conexão, disponibilidade, mudanças de
API). O `FakeBankDataProvider` mantém o desenvolvimento independente disso, e a
porta mantém a troca viável.

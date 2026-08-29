# ADR 0001 — Monólito modular no back-end

**Status:** aceito · **Data:** 2026-08-28

## Contexto

O app era um único `index.html` com `localStorage`. Precisamos de servidor para
integrar com Open Finance, e a ambição é um produto de uso pessoal que talvez
cresça — mas com um desenvolvedor só.

## Decisão

Monólito modular: um processo, um deploy, com fronteiras internas explícitas.
Cinco módulos (Ledger, Budgeting, Recurrences, Categorization, Banking), cada um
com domínio, persistência e endpoints próprios.

A fronteira é imposta por referência de projeto: um módulo só referencia o
`*.Contracts` de outro. Testes de arquitetura falham o build se isso for violado.

## Alternativas consideradas

- **Camadas tradicionais** (Controllers / Services / Repositories). Mais simples de
  começar, mas o acoplamento cresce em silêncio: em seis meses "Services" é uma
  pasta com trinta arquivos que se conhecem todos.
- **Microsserviços.** Custo operacional (deploy, rede, observabilidade, consistência
  eventual) desproporcional para um app pessoal. Sem tráfego que justifique escalar
  partes separadamente.

## Consequências

**A favor:** um deploy só; refatorar dentro de um módulo é seguro; extrair um módulo
para serviço próprio depois é viável porque ninguém depende das entranhas dele.

**Contra:** mais projetos na solution (17) e mais cerimônia — cada módulo repete
`DbContext`, unit of work e registro. É o preço da fronteira ser real e não
apenas uma convenção de pasta.

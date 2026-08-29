# ADR 0002 — Front-end feature-driven com standalone components

**Status:** aceito · **Data:** 2026-08-28

## Contexto

O app legado tinha telas trocadas por `display: none` e uma função `goTo()`. Migrando
para Angular, a pergunta é como organizar o código.

## Decisão

Organização por **feature**, não por tipo de arquivo. Cada feature tem
`data-access/`, `feature/` (páginas smart), `ui/` (componentes dumb) e o seu próprio
arquivo de rotas, carregado por `loadChildren`.

Todos os componentes são standalone, `OnPush`, com estado em `signal`. Sem NgModules.

## Alternativas consideradas

- **Por tipo** (`components/`, `services/`, `pages/`). Simples no começo; com 6 telas
  já obriga a caçar arquivos em 4 pastas para entender uma tela.
- **NgModules.** O Angular caminhou para standalone; começar em 2026 com NgModule
  seria adotar o legado de saída.
- **NgRx.** Store global completo para um app com 5 telas é mais cerimônia do que
  benefício. Stores por feature com signals resolvem, e migrar depois é possível.

## Consequências

**A favor:** a feature é a unidade de organização **e** de carregamento — o build
gera um chunk por tela. Apagar uma feature é apagar uma pasta.

**Contra:** disciplina para não importar entre features. O que for comum sobe para
`shared/` — e essa promoção precisa ser consciente, ou `shared/` vira lixeira.

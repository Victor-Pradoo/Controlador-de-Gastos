# Front-end

Angular 20, standalone components, signals, organizado por feature.
Ver [../docs/architecture.md](../docs/architecture.md).

## Estrutura

```
src/app/
├── core/        # uma instância por app: API tokens, interceptors, shell
├── shared/      # reutilizável e sem estado: pipes, componentes dumb, modelos
└── features/
    └── <feature>/
        ├── <feature>.routes.ts   # entrada lazy
        ├── data-access/          # serviços HTTP + store de signals
        ├── feature/              # páginas roteadas (smart)
        └── ui/                   # componentes de apresentação (dumb)
```

## Comandos

```bash
npm start        # ng serve em :4200, com proxy de /api para :5089
npm run build    # build de produção
npx ng test      # testes unitários
```

A API precisa estar rodando (`dotnet run` no `backend/`) para as telas carregarem
dados. Sem ela, o app sobe e mostra os toasts de erro.

## Regras

- **Uma feature não importa de outra feature.** O que for comum sobe para `shared/`.
- **Componente nunca chama `HttpClient` direto** — sempre via serviço em `data-access/`.
- Páginas em `feature/` conhecem o store; componentes em `ui/` só recebem `input()`
  e emitem `output()`.
- Todo componente é `OnPush` e standalone; estado é `signal`.
- Estilos globais (tokens do tema, utilitários) ficam em `src/styles.scss`; o resto
  é escopado no componente.

## Criando uma feature

```bash
npx ng generate component features/minha-feature/feature/minha-feature-page
```

Crie `minha-feature.routes.ts` exportando as rotas e registre em `app.routes.ts`
com `loadChildren` — é isso que dá o chunk lazy próprio.

## Modelos

`src/app/shared/models/*` espelham os DTOs de `*.Contracts` do back-end. São
mantidos à mão: ao mudar um contrato, mude os dois lados. Se isso começar a doer,
gere os tipos a partir do OpenAPI da API (`/openapi/v1.json`).

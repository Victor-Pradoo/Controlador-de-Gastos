# App legado

`index.html` — a versão original: uma única página com todo o CSS, HTML e JS
inline, guardando os dados em `localStorage`.

Fica aqui como **referência viva** durante a migração:

- o tema visual (cores, fontes, espaçamentos) foi portado para
  `frontend/src/styles.scss`;
- as categorias e cores do objeto `CAT_COLORS` viraram o catálogo do módulo
  Categorization;
- as regras de orçamento (reserva, semáforo em 70%/90%) viraram
  `MonthlyBudget.Calculate`;
- o `injectFixedThisMonth()` virou o módulo Recurrences — agora idempotente.

Continua abrindo direto no navegador. Pode ser removido quando o novo app estiver
em uso e nada mais precisar ser consultado aqui.

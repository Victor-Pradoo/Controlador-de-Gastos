# Módulos

Cinco módulos, cada um dono de uma capacidade do produto. A coluna **Depende de**
lista apenas projetos `*.Contracts` — é a única dependência permitida entre módulos.

| Módulo | Responsabilidade | Depende de | Schema |
| --- | --- | --- | --- |
| **Ledger** | Extrato: gastos, entradas e fixos materializados | — | `ledger` |
| **Budgeting** | Salário, reserva e o cálculo de quanto ainda dá para gastar | Ledger | `budgeting` |
| **Recurrences** | Gastos fixos e sua geração mensal no extrato | Ledger | `recurrences` |
| **Categorization** | Catálogo de categorias e regras de auto-categorização | Ledger | `categorization` |
| **Banking** | Conexões de Open Finance e importação do extrato | Ledger, Categorization | `banking` |

---

## Ledger

O módulo central: todo lançamento do usuário vive aqui, venha ele da digitação
manual, de um gasto fixo ou do banco.

**Agregado** `Transaction` — `Kind` (Expense / Income / FixedExpense),
`Source` (Manual / Recurrence / BankSync), valor, categoria, data.

Duas decisões que carregam peso:

- **`ExternalId` com índice único por usuário.** É a chave de idempotência de tudo
  que é automático. Re-sincronizar o mesmo período ou materializar o mesmo mês
  duas vezes não duplica nada — o banco garante.
- **`IsEditable` só para `Manual`.** Lançamento importado não se apaga na mão; a
  fonte da verdade é o extrato ou o cadastro do fixo.

**Expõe** `ILedgerModuleApi`: registrar, listar por mês, totais do mês, totais por
categoria, checar `ExternalId`.

**Endpoints** `GET|POST /api/ledger/transactions`, `DELETE .../{id}`, `GET /api/ledger/summary`.

---

## Budgeting

Guarda `BudgetSettings` (salário líquido + taxa de reserva) e produz a visão que a
tela inicial mostra.

`MonthlyBudget.Calculate` é uma função pura — sem I/O, sem banco — justamente por
ser a regra que o usuário mais sente:

```
reserva     = salário × taxa
disponível  = salário − reserva
gasto       = variáveis + fixos − entradas
saldo       = disponível − gasto
semáforo    = verde < 70% ≤ amarelo < 90% ≤ vermelho
```

Os totais vêm do `Ledger` pelo contrato. É onde estão os testes mais densos do
back-end (`MonthlyBudgetTests`).

**Endpoints** `GET /api/budget`, `GET|PUT /api/budget/settings`.

---

## Recurrences

Substitui o `injectFixedThisMonth()` do app legado, com duas correções:

- **Idempotente** — materializar usa `ExternalKeyFor(month)` = `recurrence:{id}:{2026-08}`,
  então rodar de novo não duplica.
- **Desativar, não apagar** — o legado removia retroativamente os lançamentos de um
  fixo excluído; aqui o histórico permanece.

`DayOfMonth` é fixado ao último dia disponível em meses curtos (dia 31 em fevereiro
vira 28).

Um `BackgroundService` materializa a competência corrente ao subir e uma vez por
dia. Com mais de uma instância isso precisa virar job com lock distribuído.

**Endpoints** `GET|POST /api/fixed-expenses`, `DELETE .../{id}`, `POST .../materialize`.

---

## Categorization

Duas coisas:

1. **Catálogo** — as categorias e cores do app legado, agora servidas pela API.
   O front não mantém a lista duplicada em TypeScript.
2. **Sugestão** — regras do usuário ("se contém X, é Y") primeiro; depois
   heurísticas embutidas para o que aparece em qualquer extrato brasileiro
   (`ifood`, `uber`, `posto`, `netflix`…); nada casou, cai em `Outros` com
   confiança zero para a UI pedir confirmação.

Regras explícitas de propósito: o usuário entende e corrige. Modelo estatístico é
assunto pós-MVP.

**Endpoints** `GET /api/categories`, `GET|POST|DELETE /api/categories/rules`.

---

## Banking

A razão de ser deste MVP.

**Porta** `IBankDataProvider` — connect token, dados do item, transações do período.
**Adaptadores**:

- `PluggyBankDataProvider` — API real, com cache da `apiKey` (~2h).
- `FakeBankDataProvider` — extrato sintético determinístico, padrão em
  desenvolvimento. Dá para construir a UI inteira sem credencial.

O fluxo de sincronização:

```
provedor → transações do período
   ↓ já existe ExternalId no Ledger?  → pula
   ↓ Categorization sugere a categoria
   ↓ Ledger.RegisterAsync(Source = BankSync, ExternalId = "pluggy:{id}")
```

A janela de busca refaz os últimos 3 dias já sincronizados de propósito: extrato de
cartão consolida lançamentos com atraso, e a idempotência torna a sobreposição
inofensiva.

`BankConnection` guarda apenas o `ExternalItemId` do provedor — **credenciais do
banco nunca passam por esta aplicação**, ficam no widget da Pluggy.

**Endpoints** `POST /api/banking/connect-token`, `GET|POST /api/banking/connections`,
`POST /api/banking/connections/{id}/sync`.

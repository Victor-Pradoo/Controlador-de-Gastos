#!/usr/bin/env bash
# Cria uma migration em TODOS os modulos de uma vez.
#
# Cada modulo tem DbContext e schema proprios, entao cada um tem o seu proprio
# historico de migrations - por isso o loop em vez de um comando so.
#
#   ./scripts/add-migration.sh Inicial
set -euo pipefail

NAME="${1:?Informe o nome da migration. Ex: ./scripts/add-migration.sh Inicial}"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
STARTUP="$ROOT/src/Api/ControleDeGastos.Api"

# Garante a versao do dotnet-ef fixada em .config/dotnet-tools.json.
dotnet tool restore

modules=(
  "Ledger:LedgerDbContext"
  "Budgeting:BudgetingDbContext"
  "Recurrences:RecurrencesDbContext"
  "Categorization:CategorizationDbContext"
  "Banking:BankingDbContext"
)

for entry in "${modules[@]}"; do
  module="${entry%%:*}"
  context="${entry##*:}"

  echo "==> $module ($context)"
  dotnet dotnet-ef migrations add "$NAME" \
    --project "$ROOT/src/Modules/$module/ControleDeGastos.Modules.$module" \
    --startup-project "$STARTUP" \
    --context "$context" \
    --output-dir Infrastructure/Migrations
done

echo "Pronto. Revise as migrations antes de aplicar (dotnet ef database update)."

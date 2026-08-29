<#
.SYNOPSIS
  Cria uma migration em todos os modulos de uma vez.

.DESCRIPTION
  Cada modulo tem DbContext e schema proprios, e portanto o seu proprio historico
  de migrations - por isso o loop em vez de um comando so.

.EXAMPLE
  ./scripts/add-migration.ps1 Inicial
#>
param(
  [Parameter(Mandatory = $true)]
  [string]$Name
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$startup = Join-Path $root 'src/Api/ControleDeGastos.Api'

# Garante a versao do dotnet-ef fixada em .config/dotnet-tools.json.
dotnet tool restore

$modules = @(
  @{ Module = 'Ledger';         Context = 'LedgerDbContext' }
  @{ Module = 'Budgeting';      Context = 'BudgetingDbContext' }
  @{ Module = 'Recurrences';    Context = 'RecurrencesDbContext' }
  @{ Module = 'Categorization'; Context = 'CategorizationDbContext' }
  @{ Module = 'Banking';        Context = 'BankingDbContext' }
)

foreach ($m in $modules) {
  Write-Host "==> $($m.Module) ($($m.Context))"

  $project = Join-Path $root "src/Modules/$($m.Module)/ControleDeGastos.Modules.$($m.Module)"

  dotnet dotnet-ef migrations add $Name `
    --project $project `
    --startup-project $startup `
    --context $m.Context `
    --output-dir Infrastructure/Migrations

  if ($LASTEXITCODE -ne 0) { throw "Falha ao criar migration em $($m.Module)." }
}

Write-Host 'Pronto. Revise as migrations antes de aplicar (dotnet ef database update).'

# Deploy

O que existe hoje, o que falta para publicar, e como ligar cada peça.

## Estado atual

Pipeline pronto e **inerte**: as imagens constroem e o CD roda, mas nenhum host está
provisionado. Nada gasta nada até você preencher os secrets de um environment.

| Peça | Arquivo | Estado |
| --- | --- | --- |
| CI (testes + build das imagens) | [`.github/workflows/ci.yml`](../.github/workflows/ci.yml) | ✅ ativo |
| CD (verify → publish → migrate → deploy) | [`.github/workflows/cd.yml`](../.github/workflows/cd.yml) | ✅ escrito, disparo manual |
| Imagem da API | [`backend/Dockerfile`](../backend/Dockerfile) | ✅ |
| Imagem do front (nginx + proxy) | [`frontend/Dockerfile`](../frontend/Dockerfile) | ✅ |
| Pilha completa local | [`docker-compose.yml`](../docker-compose.yml) | ✅ perfil `full` |
| Host | — | ❌ a escolher |
| Banco acessível pela rede | — | ❌ a provisionar |
| Autenticação | — | ❌ ver abaixo |

## ⚠️ Antes de publicar com dados reais

Não existe login. [`DevCurrentUser`](../backend/src/Api/ControleDeGastos.Api/Authentication/DevCurrentUser.cs)
devolve `IsAuthenticated => true` para todo mundo e aceita o header `X-User-Id` para
assumir qualquer usuário. Numa URL pública, isso significa que **qualquer pessoa que
descubra o endereço lê e edita seus dados financeiros**.

A decisão em vigor é publicar **apenas com dados fictícios**, mantendo
`Banking__Pluggy__UseFakeProvider=true`. Antes de lançar qualquer valor real, faça uma
destas:

1. **Autenticação na borda** — App Service e Container Apps têm "Easy Auth" nativo
   (Entra ID, Google, GitHub), restrito à sua conta. Zero código.
2. **Autenticação de verdade** — item 2 do [roadmap](roadmap.md): módulo `Identity`,
   `ICurrentUser` lendo o claim `sub`, interceptor com Bearer. Nenhum módulo de
   negócio muda.

Nos dois casos, remova o override por `X-User-Id` — ele permite trocar de usuário
mesmo depois do login.

## Topologia

Front e API continuam **deployables separados** (a regra do [CLAUDE.md](../CLAUDE.md)),
unificados por um proxy. É o nginx do container do front que cria a mesma origem que
[`environment.ts`](../frontend/src/environments/environment.ts) assume com
`apiBaseUrl: '/api'` — e é por isso que **não há CORS em produção**.

```
            :8080
  navegador ───► web (nginx)
                  ├─ /          → arquivos do Angular
                  └─ /api/*     → api (ASP.NET, :8080)
                                    └─► SQL Server
```

Se algum dia publicar em domínios diferentes, dois ajustes são obrigatórios: trocar
`apiBaseUrl` para a URL absoluta da API e popular `Cors:AllowedOrigins`.

## Rodar a pilha inteira localmente

A forma mais barata de descobrir que um Dockerfile quebrou:

```bash
docker compose --profile full up --build
# http://localhost:8080
```

Sem o perfil, sobe só o banco (`docker compose up -d sqlserver`), para quando você
quiser rodar API e front na mão.

## Variáveis de ambiente

O .NET mapeia hierarquia de configuração para variável de ambiente com **duplo
sublinhado**: `ConnectionStrings__Database` sobrescreve `ConnectionStrings:Database`.

### API

| Variável | Valor em produção | Por quê |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` | desliga Scalar/OpenAPI |
| `ASPNETCORE_HTTP_PORTS` | `8080` | porta não privilegiada; a imagem não roda como root |
| `ConnectionStrings__Database` | *(secret)* | **nunca** no `appsettings.json` |
| `Database__AutoMigrate` | `false` | migrations são passo do pipeline, não do boot |
| `Auth__DevUserId` | GUID do seu usuário | enquanto não há login |
| `Banking__Pluggy__UseFakeProvider` | `true` | sem isso, precisa de credencial Pluggy |
| `Cors__AllowedOrigins__0` | — | só se front e API ficarem em domínios diferentes |

### Front (nginx)

| Variável | Valor |
| --- | --- |
| `API_UPSTREAM` | URL interna da API, ex. `http://api:8080` |
| `NGINX_PORT` | `8080` |

## Ligar o CD

O workflow roda em **disparo manual** (`workflow_dispatch`) de propósito: enquanto não
há login, deploy automático a cada commit é a receita para expor dados sem querer.
Quando a autenticação entrar, troque por `on: push: branches: [main]`.

### 1. Criar o environment

`Settings → Environments → New environment` → `staging`.

Aqui é onde entram *required reviewers*, se quiser aprovação manual — é o equivalente
ao **Approvals and Checks** do Azure DevOps.

### 2. Preencher os secrets

No environment criado:

| Secret | Quando |
| --- | --- |
| `DATABASE_CONNECTION_STRING` | sempre (o job `migrate` para com um aviso sem ele) |
| `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` | se for Azure via OIDC |

### 3. Descomentar o destino

Em [`cd.yml`](../.github/workflows/cd.yml), no job `deploy`, há blocos prontos para
Azure Container Apps e App Service. As imagens já vão para o `ghcr.io` antes disso,
então qualquer host que aceite container serve.

### 4. Rodar

`Actions → CD → Run workflow → staging`.

## O que o CD faz

```
verify ──► publish ──► migrate ──► deploy
  │           │           │           │
  testes    imagens     dotnet ef    troca a
  (com      no ghcr    database     versão
  SQL real)             update
```

`migrate` roda **antes** de `deploy`, e é um passo explícito porque
`Database:AutoMigrate` fica `false` em produção. Dois motivos: duas instâncias subindo
juntas disputam o lock de migration, e um rollback da aplicação não reverte o schema —
separar os dois deixa a ordem sob seu controle.

Cada módulo tem `DbContext` e histórico de migrations próprios, daí o loop pelos cinco
contextos.

## Escolher o host

Todos recebem as mesmas imagens do `ghcr.io`. O que muda é o custo e o cold start.

| Opção | Custo | Observação |
| --- | --- | --- |
| **Azure Container Apps** + Azure SQL | baixo, escala a zero | cold start de alguns segundos no 1º acesso |
| **Azure App Service** + Static Web Apps + Azure SQL | plano fixo | sempre quente; mais parecido com o que você faz no ADO |
| **Fly.io / Render** + Azure SQL | compute barato | banco em outra nuvem = latência em toda query |

O SQL Server é o que puxa o banco para o Azure em qualquer cenário — nem Fly.io nem
Render oferecem SQL Server gerenciado. Confira os preços atuais antes de decidir; eles
mudam com frequência.

## Azure DevOps → GitHub Actions

| Azure DevOps | GitHub Actions |
| --- | --- |
| `azure-pipelines.yml` | `.github/workflows/*.yml` (vários arquivos) |
| Stages | não existe; encadeie jobs com `needs:` |
| `pool: vmImage` | `runs-on:` |
| `- task: DotNetCoreCLI@2` | `- uses: actions/...` ou `- run:` |
| Variable Groups | Environments (variables + secrets) |
| Service Connections | credencial federada **OIDC** (`id-token: write`) |
| Approvals and Checks | Environment com *required reviewers* |
| Publish/Download Artifact | `actions/upload-artifact` / `download-artifact` |
| Deployment job + strategy | job comum com `environment:` |
| Triggers | `on: push:` / `on: pull_request:` |

A diferença que mais pega quem vem do ADO: **não existe Service Connection**. Em vez de
cadastrar uma credencial, você registra no Entra ID uma *federated credential* que
autoriza este repositório a pedir um token de curta duração. Nenhum segredo de longa
duração fica guardado no GitHub — é melhor que o modelo do ADO.

## Pendências conhecidas

- **Nenhuma imagem foi construída de verdade ainda.** Os Dockerfiles foram escritos e
  o YAML valida, mas esta máquina não tem Docker instalado. O job `images` do CI é o
  primeiro lugar onde eles serão exercitados.
- **`ModuleHostExtensions` guarda os módulos numa lista `static`.** Irrelevante com uma
  instância; se um dia escalar horizontalmente, revise (ver [modules.md](modules.md)).
- **`appsettings.json` versiona uma connection string de LocalDB.** Inofensiva (é local
  e usa Integrated Security), mas a de produção nunca deve seguir esse caminho.

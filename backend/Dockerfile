# Imagem da API. Contexto de build = raiz do repositorio, para alcancar o global.json
# (que fixa o SDK 10.0.400) junto com a solution.
#
#   docker build -f backend/Dockerfile -t controle-gastos-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# global.json fixa o SDK; os Directory.*.props valem para toda a arvore de projetos
# e o Packages.props e onde as versoes de pacote vivem (Central Package Management).
COPY global.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props backend/
COPY backend/src/ backend/src/

WORKDIR /src/backend

# Restore a partir do projeto da API, e nao da solution: o .slnx inclui os projetos
# de teste, que nao entram na imagem (ver .dockerignore). A API referencia todos os
# modulos, entao restaurar por ela ja traz tudo que o runtime precisa.
RUN dotnet restore src/Api/ControleDeGastos.Api/ControleDeGastos.Api.csproj

# A imagem de producao nao roda testes: quem reprova build quebrado e o CI.
RUN dotnet publish src/Api/ControleDeGastos.Api/ControleDeGastos.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Os arquivos ficam de root e a aplicacao roda como 'app': ela le e executa os
# binarios, mas nao consegue sobrescrever os proprios binarios em caso de invasao.
COPY --from=build /app/publish .

# Sem root. A imagem do .NET 8+ ja traz o usuario 'app' (uid 1654) e publica o uid
# em APP_UID - mas nao o ativa por padrao, entao a linha abaixo e necessaria.
# Nao crie o usuario na mao: 'useradd app' falha com exit 9 (nome ja em uso).
USER $APP_UID

# Porta alta e nao privilegiada, requisito de rodar como usuario comum.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# InvariantGlobalization=false no Directory.Build.props: a formatacao pt-BR depende
# do ICU, que a imagem aspnet ja traz. Nao troque por a imagem alpine sem conferir isso.

ENTRYPOINT ["dotnet", "ControleDeGastos.Api.dll"]

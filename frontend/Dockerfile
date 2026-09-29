# Imagem do front: Angular compilado servido por nginx, que tambem faz o proxy de
# /api para a API. E o nginx que cria a "mesma origem" que o environment.ts de
# producao assume (apiBaseUrl: '/api') - por isso nao ha CORS em producao.
#
# Contexto de build = raiz do repositorio, por simetria com backend/Dockerfile.
#
#   docker build -f frontend/Dockerfile -t controle-gastos-web .

FROM node:22-alpine AS build
WORKDIR /src

# package-lock primeiro: enquanto as dependencias nao mudarem, o npm ci vem do cache.
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

FROM nginx:1.27-alpine AS runtime

# O builder "application" do Angular 20 separa os arquivos do browser num subdiretorio.
COPY --from=build /src/dist/frontend/browser /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/templates/default.conf.template

# Resolvidos em tempo de execucao pelo entrypoint do nginx, que processa os templates.
# Assim a mesma imagem serve dev e producao: muda so a variavel.
ENV API_UPSTREAM=http://api:8080
ENV NGINX_PORT=8080

EXPOSE 8080

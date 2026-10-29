# Сборка из корня репозитория (там лежат .sln и .dockerignore):
#   docker build -f ReReviewer.Server/Dockerfile -t rereviewer-api .
# Обычно запускается через docker-compose.yml, который собирает две цели: final (API) и migrate (миграции EF).

# ---------- сборка ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только файлы проектов: пока зависимости не меняются, слой с restore берётся из кэша.
COPY ReReviewer.sln ./
COPY ReReviewer.Server/ReReviewer.Server.csproj ReReviewer.Server/
COPY ReReviewer.Application/ReReviewer.Application.csproj ReReviewer.Application/
COPY ReReviewer.Data/ReReviewer.Data.csproj ReReviewer.Data/
RUN dotnet restore ReReviewer.Server/ReReviewer.Server.csproj

COPY . .
RUN dotnet publish ReReviewer.Server/ReReviewer.Server.csproj -c Release -o /app/publish --no-restore

# ---------- бандл миграций EF ----------
# Один исполняемый файл, который применяет миграции к базе. Строку подключения берёт из
# конфигурации приложения, то есть из переменной окружения ConnectionStrings__ReReviewerDB.
FROM build AS migrations-bundle
RUN dotnet tool install --global dotnet-ef --version 10.0.5
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle \
        --project ReReviewer.Data \
        --startup-project ReReviewer.Server \
        --configuration Release \
        --output /app/efbundle

# ---------- образ для применения миграций ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrate
WORKDIR /app
COPY --from=migrations-bundle /app/efbundle .
USER $APP_UID
ENTRYPOINT ["./efbundle"]

# ---------- образ API ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Непривилегированный пользователь из базового образа .NET; Kestrel слушает HTTP на порту 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "ReReviewer.Server.dll"]

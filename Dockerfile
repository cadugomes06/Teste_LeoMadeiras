# Etapa 1: compila e publica com o SDK.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia só os .csproj primeiro: enquanto as dependências não mudarem, o restore fica em cache.
COPY Directory.Build.props ./
COPY src/LeoMadeiras.ApiFoundation.Domain/LeoMadeiras.ApiFoundation.Domain.csproj src/LeoMadeiras.ApiFoundation.Domain/
COPY src/LeoMadeiras.ApiFoundation.Application/LeoMadeiras.ApiFoundation.Application.csproj src/LeoMadeiras.ApiFoundation.Application/
COPY src/LeoMadeiras.ApiFoundation.Infrastructure/LeoMadeiras.ApiFoundation.Infrastructure.csproj src/LeoMadeiras.ApiFoundation.Infrastructure/
COPY src/LeoMadeiras.ApiFoundation.Api/LeoMadeiras.ApiFoundation.Api.csproj src/LeoMadeiras.ApiFoundation.Api/
RUN dotnet restore src/LeoMadeiras.ApiFoundation.Api/LeoMadeiras.ApiFoundation.Api.csproj

COPY src/ src/
RUN dotnet publish src/LeoMadeiras.ApiFoundation.Api/LeoMadeiras.ApiFoundation.Api.csproj -c Release -o /app/publish --no-restore

# Etapa 2: imagem final só com o runtime (menor, sem o SDK).
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Pasta do banco SQLite (montada como volume no docker-compose), gravável pelo usuário sem privilégios.
RUN mkdir -p /app/data && chown $APP_UID /app/data
USER $APP_UID

COPY --from=build /app/publish .
ENV ConnectionStrings__ApiFoundation="Data Source=/app/data/apifoundation.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "LeoMadeiras.ApiFoundation.Api.dll"]

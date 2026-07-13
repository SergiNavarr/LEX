# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar solo el csproj primero para cachear restore
COPY Lex.Api.csproj ./
RUN dotnet restore Lex.Api.csproj

# Copiar el resto del código y publicar
COPY . ./
RUN dotnet publish Lex.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar el output del build
COPY --from=build /app/publish ./

# Render usa el puerto 10000 por default; ASPNETCORE_URLS lo definimos como env var externa
EXPOSE 10000

# Crear directorio /tmp con permisos de escritura para SQLite
RUN mkdir -p /tmp && chmod 777 /tmp

ENTRYPOINT ["dotnet", "Lex.Api.dll"]

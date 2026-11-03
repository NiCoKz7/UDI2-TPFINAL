# ==========================================
# ETAPA 1: Compilación y Publicación (Build)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar configuración de SDK y archivo de proyecto, y restaurar dependencias
COPY global.json ./
COPY *.csproj ./
RUN dotnet restore

# Copiar todo el código fuente y compilar en modo Release
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# ETAPA 2: Entorno de Ejecución (Runtime)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

# Copiar los artefactos compilados desde la etapa 'build'
COPY --from=build /app/publish .

# Puertos internos de Kestrel: 5000 público y 8080 administrativo
EXPOSE 5000
EXPOSE 8080

# Kestrel escucha en todas las interfaces
ENV ASPNETCORE_URLS=http://+:5000

# Comando de inicio del contenedor
ENTRYPOINT ["dotnet", "PerfumesApi.dll"]

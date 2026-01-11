# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY ["PaymentMicroservicio.csproj", "./"]
RUN dotnet restore "PaymentMicroservicio.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar en modo Release
RUN dotnet build "PaymentMicroservicio.csproj" -c Release -o /app/build

# Publicar
FROM build AS publish
RUN dotnet publish "PaymentMicroservicio.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copiar archivos publicados
COPY --from=publish /app/publish .

# Exponer puerto
EXPOSE 8080

# Variable de entorno para el puerto
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Comando de inicio
ENTRYPOINT ["dotnet", "PaymentMicroservicio.dll"]

# Imagem de produção. Duas fases: a primeira compila, a segunda só leva o resultado —
# o SDK (cerca de 800 MB) não vai para o contentor final.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar primeiro os ficheiros de projeto e restaurar: enquanto as dependências não mudarem,
# esta camada fica em cache e o build é muito mais rápido.
COPY SmeCore.sln ./
COPY src/SmeCore.Domain/SmeCore.Domain.csproj src/SmeCore.Domain/
COPY src/SmeCore.Infrastructure/SmeCore.Infrastructure.csproj src/SmeCore.Infrastructure/
COPY src/SmeCore.Web/SmeCore.Web.csproj src/SmeCore.Web/
COPY tests/SmeCore.Tests/SmeCore.Tests.csproj tests/SmeCore.Tests/
RUN dotnet restore SmeCore.sln

COPY . .
RUN dotnet publish src/SmeCore.Web/SmeCore.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Dados de fuso horário: sem isto, "Europe/Lisbon" não existe na imagem e as datas seriam
# apresentadas em UTC (uma hora a menos no verão).
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=Europe/Lisbon \
    DOTNET_RUNNING_IN_CONTAINER=true \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Correr sem privilégios de root.
RUN useradd --uid 5678 --create-home aplicacao \
    && chown -R aplicacao:aplicacao /app
USER aplicacao

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD ["/bin/sh", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.0\\r\\n\\r\\n' >&3 && head -1 <&3 | grep -q '200'"]

ENTRYPOINT ["dotnet", "SmeCore.Web.dll"]

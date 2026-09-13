# Build context: solution/backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Fundo.Loans.Domain/Fundo.Loans.Domain.csproj src/Fundo.Loans.Domain/
COPY src/Fundo.Loans.Application/Fundo.Loans.Application.csproj src/Fundo.Loans.Application/
COPY src/Fundo.Loans.Infrastructure/Fundo.Loans.Infrastructure.csproj src/Fundo.Loans.Infrastructure/
COPY src/Fundo.Loans.Api/Fundo.Loans.Api.csproj src/Fundo.Loans.Api/

RUN dotnet restore src/Fundo.Loans.Api/Fundo.Loans.Api.csproj

COPY src/Fundo.Loans.Domain/ src/Fundo.Loans.Domain/
COPY src/Fundo.Loans.Application/ src/Fundo.Loans.Application/
COPY src/Fundo.Loans.Infrastructure/ src/Fundo.Loans.Infrastructure/
COPY src/Fundo.Loans.Api/ src/Fundo.Loans.Api/

RUN dotnet publish src/Fundo.Loans.Api/Fundo.Loans.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app .

USER app
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 \
    CMD ["dotnet", "Fundo.Loans.Api.dll", "--health-check"]

ENTRYPOINT ["dotnet", "Fundo.Loans.Api.dll"]

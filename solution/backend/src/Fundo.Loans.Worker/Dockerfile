# Build context: solution/backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/Fundo.Loans.Domain/Fundo.Loans.Domain.csproj src/Fundo.Loans.Domain/
COPY src/Fundo.Loans.Application/Fundo.Loans.Application.csproj src/Fundo.Loans.Application/
COPY src/Fundo.Loans.Infrastructure/Fundo.Loans.Infrastructure.csproj src/Fundo.Loans.Infrastructure/
COPY src/Fundo.Loans.Worker/Fundo.Loans.Worker.csproj src/Fundo.Loans.Worker/

RUN dotnet restore src/Fundo.Loans.Worker/Fundo.Loans.Worker.csproj

COPY src/Fundo.Loans.Domain/ src/Fundo.Loans.Domain/
COPY src/Fundo.Loans.Application/ src/Fundo.Loans.Application/
COPY src/Fundo.Loans.Infrastructure/ src/Fundo.Loans.Infrastructure/
COPY src/Fundo.Loans.Worker/ src/Fundo.Loans.Worker/

RUN dotnet publish src/Fundo.Loans.Worker/Fundo.Loans.Worker.csproj -c Release -o /app --no-restore

# No HTTP listener, so the plain .NET runtime image is enough (no ASP.NET Core framework).
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

ENV DOTNET_EnableDiagnostics=0

COPY --from=build /app .

USER app

ENTRYPOINT ["dotnet", "Fundo.Loans.Worker.dll"]

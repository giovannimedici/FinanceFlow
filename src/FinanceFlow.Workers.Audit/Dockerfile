FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FinanceFlow.Workers.Audit/FinanceFlow.Workers.Audit.csproj", "src/FinanceFlow.Workers.Audit/"]
COPY ["src/FinanceFlow.Application/FinanceFlow.Application.csproj", "src/FinanceFlow.Application/"]
COPY ["src/FinanceFlow.Domain/FinanceFlow.Domain.csproj", "src/FinanceFlow.Domain/"]

RUN dotnet restore "src/FinanceFlow.Workers.Audit/FinanceFlow.Workers.Audit.csproj"

COPY . .
WORKDIR "/src/src/FinanceFlow.Workers.Audit"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FinanceFlow.Workers.Audit.dll"]

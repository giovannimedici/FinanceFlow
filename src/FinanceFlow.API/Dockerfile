FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/FinanceFlow.API/FinanceFlow.API.csproj",           "src/FinanceFlow.API/"]
COPY ["src/FinanceFlow.Application/FinanceFlow.Application.csproj", "src/FinanceFlow.Application/"]
COPY ["src/FinanceFlow.Domain/FinanceFlow.Domain.csproj",     "src/FinanceFlow.Domain/"]
COPY ["src/FinanceFlow.Infrastructure/FinanceFlow.Infrastructure.csproj", "src/FinanceFlow.Infrastructure/"]

RUN dotnet restore "src/FinanceFlow.API/FinanceFlow.API.csproj"

COPY . .
WORKDIR "/src/src/FinanceFlow.API"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "FinanceFlow.API.dll"]
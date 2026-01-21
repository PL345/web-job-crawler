FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /build

COPY src/SharedDomain ./SharedDomain
COPY src/CrawlWorker ./CrawlWorker

RUN dotnet restore CrawlWorker/CrawlWorker.csproj
RUN dotnet publish CrawlWorker/CrawlWorker.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=builder /app/publish .

ENTRYPOINT ["dotnet", "CrawlWorker.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /build

COPY src/SharedDomain ./SharedDomain
COPY src/CrawlAPI ./CrawlAPI

RUN dotnet restore CrawlAPI/CrawlAPI.csproj
RUN dotnet publish CrawlAPI/CrawlAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=builder /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CrawlAPI.dll"]

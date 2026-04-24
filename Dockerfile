FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy solution and project files first for better caching
COPY TradingProject.ThirdParty.slnx Directory.Packages.props Directory.Build.props ./
COPY src/TradingProject.ThirdParty.Api/*.csproj src/TradingProject.ThirdParty.Api/
COPY src/TradingProject.ThirdParty.Application/*.csproj src/TradingProject.ThirdParty.Application/
COPY src/TradingProject.ThirdParty.Domain/*.csproj src/TradingProject.ThirdParty.Domain/
COPY src/TradingProject.ThirdParty.Infrastructure/*.csproj src/TradingProject.ThirdParty.Infrastructure/

RUN dotnet restore src/TradingProject.ThirdParty.Api/TradingProject.ThirdParty.Api.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish src/TradingProject.ThirdParty.Api/TradingProject.ThirdParty.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Default Redis configuration (can be overridden by K8s ConfigMap)
ENV REDIS__HOST=redis
ENV REDIS__PORT=6379

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TradingProject.ThirdParty.Api.dll"]

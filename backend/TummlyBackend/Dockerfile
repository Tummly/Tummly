# Build from repo root:
#   docker build -f backend/TummlyBackend/Dockerfile -t tummly-api .
#
# The csproj links pricebook files from ../../docs/product/billing-pack-v3.0/
# so those paths must exist in the build context (repo root), not only
# backend/TummlyBackend/.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY docs/product/billing-pack-v3.0/tummly_uk_billing_config_v3.0.json \
     docs/product/billing-pack-v3.0/current-pricebook-id \
     docs/product/billing-pack-v3.0/

COPY backend/TummlyBackend/TummlyBackend.csproj backend/TummlyBackend/
WORKDIR /src/backend/TummlyBackend
RUN dotnet restore

COPY backend/TummlyBackend/ .
RUN dotnet publish TummlyBackend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "TummlyBackend.dll"]

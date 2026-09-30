FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props NuGet.Config ./
COPY src/Catalog.Domain/Catalog.Domain.csproj src/Catalog.Domain/
COPY src/Catalog.Application/Catalog.Application.csproj src/Catalog.Application/
COPY src/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Catalog.Infrastructure/
COPY src/Catalog.Api/Catalog.Api.csproj src/Catalog.Api/
RUN dotnet restore src/Catalog.Api/Catalog.Api.csproj --configfile NuGet.Config

COPY src/ src/
RUN dotnet publish src/Catalog.Api/Catalog.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]

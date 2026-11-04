FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/BillingService.Domain/BillingService.Domain.csproj src/BillingService.Domain/
COPY src/BillingService.Application/BillingService.Application.csproj src/BillingService.Application/
COPY src/BillingService.Infrastructure/BillingService.Infrastructure.csproj src/BillingService.Infrastructure/
COPY src/BillingService.API/BillingService.API.csproj src/BillingService.API/
RUN dotnet restore src/BillingService.API/BillingService.API.csproj

COPY src/ src/
RUN dotnet publish src/BillingService.API/BillingService.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BillingService.API.dll"]

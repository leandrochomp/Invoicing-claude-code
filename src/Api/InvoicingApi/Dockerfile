FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props .
COPY src/Api/InvoicingApi/InvoicingApi.csproj src/Api/InvoicingApi/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore src/Api/InvoicingApi/InvoicingApi.csproj

COPY src/Api/InvoicingApi/ src/Api/InvoicingApi/
COPY src/Shared/ src/Shared/
RUN dotnet publish src/Api/InvoicingApi/InvoicingApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InvoicingApi.dll"]

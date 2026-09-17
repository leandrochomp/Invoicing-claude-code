FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Api/InvoicingApi/InvoicingApi.csproj src/Api/InvoicingApi/
RUN dotnet restore src/Api/InvoicingApi/InvoicingApi.csproj

COPY src/Api/InvoicingApi/ src/Api/InvoicingApi/
RUN dotnet publish src/Api/InvoicingApi/InvoicingApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InvoicingApi.dll"]

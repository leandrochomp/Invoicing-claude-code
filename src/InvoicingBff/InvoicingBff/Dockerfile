FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props .
COPY src/InvoicingBff/InvoicingBff/InvoicingBff.csproj src/InvoicingBff/InvoicingBff/
RUN dotnet restore src/InvoicingBff/InvoicingBff/InvoicingBff.csproj

COPY src/InvoicingBff/InvoicingBff/ src/InvoicingBff/InvoicingBff/
RUN dotnet publish src/InvoicingBff/InvoicingBff/InvoicingBff.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InvoicingBff.dll"]

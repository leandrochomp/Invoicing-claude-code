# Invoicing-claude-code

* C# 14.0
* WebAPI (net 10.0)
* GRPC
* BFF (Backend for Frontend) - TODO
* EF Core

# Design Solution:
* Vertical Slice Architecture

# Development Tools:
* dotnet cli
* Jetbrains Rider 2026.2.1 (Non-commercial)
* Ubuntu 26.04.1 LTS

# CLI Commands:
```shell 
dotnet new webapi -o InvoicingApi
dotnet new web -o InvoicingBff
```
* root folder:

```bash
dotnet new sln -n Invoicing
dotnet sln Invoicing.slnx add src/Api/InvoicingApi/InvoicingApi.csproj
```

# Environment Variables Setup

## Setting Up Local Environment Variables

This project uses environment variables to manage configuration settings, especially sensitive information like database credentials. To get started:

1. **Create your local environment file**:
   ```bash
   # Navigate to the src directory
   cd src
   
   # Copy the template file to create your own .env file
   cp .env.template .env
   ```

2. **Edit your .env file**:
   Open the `.env` file in your code editor and replace the placeholder values with your actual settings:
   ```
   # Database Configuration
   DB_HOST=postgres
   DB_PORT=5432
   DB_NAME=invoicing
   DB_USER=postgres
   DB_PASSWORD=your_actual_password_here
   ```

3. **Important Notes**:
    - The `.env` file contains sensitive information and should **never be committed** to the repository
    - The `.env.template` file is a template with placeholder values that is safe to commit
    - If you add new environment variables, remember to update the template file too

## Using Environment Variables

The application will automatically read values from your `.env` file when running with Docker Compose. You can also:

- Override values at runtime: `DB_PASSWORD=custom_password make up`
- Use different values per environment: Copy `.env` to `.env.development` or `.env.test`

## Available Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| DB_HOST | Database server hostname | postgres |
| DB_PORT | Database server port | 5432 |
| DB_NAME | Database name | invoicing |
| DB_USER | Database username | postgres |
| DB_PASSWORD | Database password | postgres |
| ENVIRONMENT | Application environment | Development |
| DB_MIN_POOL_SIZE | Minimum connection pool size | 1 |
| DB_MAX_POOL_SIZE | Maximum connection pool size | 20 |
| DB_INCLUDE_ERROR_DETAIL | Include detailed DB errors | true |

## Alternative: Using .NET User Secrets (Local Development)

1. **Initialize user secrets for each project**:
   ```bash
   cd src/Api/InvoicingApi
   dotnet user-secrets init
   
   cd ../../InvoicingGrpc
   dotnet user-secrets init
   
   cd ../InvoicingBff
   dotnet user-secrets init
   ```

2. **Add your database credentials to user secrets**:
   ```bash
   cd src/App/InvoicingApi
   dotnet user-secrets set "Database:Host" "localhost"
   dotnet user-secrets set "Database:Port" "5432"
   dotnet user-secrets set "Database:Name" "invoicing"
   dotnet user-secrets set "Database:User" "postgres"
   dotnet user-secrets set "Database:Password" "your_secure_password"
   
   # Repeat for other projects as needed
   ```

3. **View your stored secrets**:
   ```bash
   dotnet user-secrets list
   ```

User secrets are stored in your user profile directory, not in the project files, so they're never committed to source control. This is ideal for developer-specific settings when working directly with the .NET CLI or Visual Studio.

> **Note**: User secrets are for development only. For production, use environment variables, Docker secrets, or a secure vault service.

# Database Migrations

## Prerequisites
- PostgreSQL installed and running
- Connection string properly configured in appsettings.json

## Running Migrations

### Create a new migration
in the root folder of the solution, run the following command to create a new migration:
```bash
dotnet ef migrations add InitialCreate --project Shared --startup-project App/InvoicingApi
```

### Generate SQL script(optional)
in the root folder of the solution, run the following command to generate a SQL script for the migration:
```bash
dotnet ef migrations script --project Shared --startup-project App/InvoicingApi
```

### Apply migrations
in the root folder of the solution, run the following command to apply the migrations to the database:
```bash
dotnet ef database update --project Shared --startup-project App/InvoicingApi
```

### Rollback a migration
in the root folder of the solution, run the following command to rollback the last migration:
```bash
dotnet ef database remove --project Shared --startup-project App/InvoicingApi
```



# Defra.Database.Postgres

A .NET library for PostgreSQL database integration, supporting standard connection strings and AWS IAM authentication with Entity Framework Core.

## Features

- **Standard Connection**: Connect using traditional PostgreSQL connection strings.
- **IAM Authentication**: Secure connection to AWS RDS using IAM roles and tokens.
- **Read/Write Splitting**: Support for separate read-write and read-only database contexts.
- **Resilience**: Built-in retry policy for transient database errors.
- **EF Core Integration**: Pre-configured `DbContext` setup for PostgreSQL.

## Installation

Register the services in your `Program.cs` or `Startup.cs`:

```csharp
using Defra.Database.Postgres;

public void ConfigureServices(IServiceCollection services)
{
    // Add PostgreSQL services
    services.AddPostgresDatabase(Configuration);
}

public void Configure(IApplicationBuilder app)
{
    // Optional: Use database (checks connection)
    app.UsePostgresDatabase();
}
```

## Configuration

The library uses `IConfiguration` to retrieve settings. You can configure it via `appsettings.json`, environment variables, or other configuration providers.

### Standard Connection Strings

Add the following connection strings to your configuration:

```json
{
  "ConnectionStrings": {
    "ReadWritePostgresConnection": "Host=your-host;Database=your-db;Username=your-user;Password=your-password",
    "ReadOnlyPostgresConnection": "Host=your-readonly-host;Database=your-db;Username=your-user;Password=your-password"
  }
}
```

- `ReadWritePostgresConnection`: Used by `PostgresDbContext`.
- `ReadOnlyPostgresConnection`: Used by `ReadOnlyPostgresDbContext`. If not provided, it falls back to the read-write connection.

### AWS IAM Authentication

To use IAM authentication, configure the `PostgresConfiguration` section:

```json
{
  "PostgresConfiguration": {
    "UseIamAuthentication": true,
    "ReadWriteHost": "your-rds-endpoint",
    "ReadOnlyHost": "your-readonly-rds-endpoint",
    "Port": 5432,
    "Name": "your-database-name",
    "User": "iam-database-user"
  },
  "AWS": {
    "Region": "eu-west-2"
  }
}
```

If `UseIamAuthentication` is `true`, the library will ignore standard connection strings and use the provided host and user information to generate IAM tokens for authentication.

## Usage

Inject the appropriate context into your services:

```csharp
public class MyService(PostgresDbContext dbContext, ReadOnlyPostgresDbContext readOnlyDbContext)
{
    public async Task DoWork()
    {
        // Use dbContext for write operations
        // Use readOnlyDbContext for optimized read operations (no tracking)
    }
}
```

## SonarCloud Analysis

This project uses SonarCloud for code quality analysis.

**Important:** To avoid conflicts between CI-based analysis and SonarCloud Automatic Analysis, ensure that **Automatic Analysis** is **disabled** in the SonarCloud project settings:
1. Go to your project in SonarCloud.
2. Navigate to **Administration** > **Analysis Method**.
3. Toggle **Automatic Analysis** to **OFF**.

The CI analysis is performed via GitHub Actions using the `.github/workflows/sonar.yml` workflow and the `sonar.cake` script.



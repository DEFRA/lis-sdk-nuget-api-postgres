# Defra.Lis.Postgres

An opinionated Entity Framework Core integration for PostgreSQL. It registers read/write and read-only contexts, configures transient-failure retries, and supports either connection strings or AWS RDS IAM database authentication.

## Installation

```bash
dotnet add package Defra.Lis.Postgres
```

The package includes the `Defra.Lis.Database` and `Defra.Lis.Entities` project dependencies.

## Registration

```csharp
using Defra.Database.Postgres;

builder.Services.AddPostgresDatabase(builder.Configuration);

var app = builder.Build();

// Optional startup connectivity check.
app.UsePostgresDatabase();
```

`AddPostgresDatabase` registers:

- `PostgresDbContext` for read/write access;
- `ReadOnlyPostgresDbContext` with `QueryTrackingBehavior.NoTracking`;
- an `IDataSourceFactory<NpgsqlDataSource>`; and
- an AWS RDS token generator when IAM authentication is enabled.

The provider retries selected transient PostgreSQL and connection failures up to five times, with a maximum retry delay of 10 seconds, and applies a 60-second command timeout.

## Connection-string authentication

The default mode reads named connection strings:

```json
{
  "ConnectionStrings": {
    "ReadWritePostgresConnection": "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres",
    "ReadOnlyPostgresConnection": "Host=readonly.example;Port=5432;Database=app;Username=postgres;Password=postgres"
  }
}
```

`ReadWritePostgresConnection` is required. `ReadOnlyPostgresConnection` is optional and falls back to the read/write connection when omitted.

## AWS RDS IAM authentication

Enable IAM authentication with the `PostgresConfiguration` section:

```json
{
  "PostgresConfiguration": {
    "UseIamAuthentication": true,
    "ReadWriteHost": "database.cluster.example.eu-west-2.rds.amazonaws.com",
    "ReadOnlyHost": "database.cluster-ro.example.eu-west-2.rds.amazonaws.com",
    "Port": 5432,
    "Name": "app",
    "User": "app_user"
  },
  "AWS": {
    "Region": "eu-west-2"
  }
}
```

The implementation uses an `AWSCredentials` service from dependency injection when one is registered; otherwise it resolves credentials from the AWS default credentials chain. `AWS:Region` defaults to `eu-west-2`. IAM connections require SSL, and authentication tokens are refreshed periodically.

Both `ReadWriteHost` and `ReadOnlyHost` must be configured in IAM mode.

## Using the contexts

```csharp
using Defra.Database.Postgres;
using Microsoft.EntityFrameworkCore;

public sealed class CustomerReader(ReadOnlyPostgresDbContext database)
{
    public Task<int> CountAsync(CancellationToken cancellationToken) =>
        database.Set<Customer>().CountAsync(cancellationToken);
}
```

Use `PostgresDbContext` for writes. Calls to `SaveChanges` or `SaveChangesAsync` on `ReadOnlyPostgresDbContext` throw `InvalidOperationException`.

Both contexts use the `public` schema and declare the PostgreSQL `pgcrypto` and `citext` extensions. When a tracked `BaseProcessingEntity` is modified through `PostgresDbContext`, `ProcessedAt` is updated to `DateTime.UtcNow` before saving.

## Constants

`ColumnTypes` provides common PostgreSQL column-type strings for entity mappings, and `PostgreExtensions` provides names for commonly used PostgreSQL extensions and SQL functions.

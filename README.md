# Defra database libraries

Reusable .NET 10 libraries for Entity Framework Core and PostgreSQL applications. The solution provides shared database abstractions, common entity base classes, and an opinionated PostgreSQL integration with optional AWS RDS IAM authentication.

## Packages

| Package | Purpose |
| --- | --- |
| `Defra.Lis.Database` | EF Core base context and abstractions for data sources and authentication tokens. |
| `Defra.Lis.Entities` | Reusable audit, processing, and lookup/type entity base classes. |
| `Defra.Lis.Postgres` | PostgreSQL contexts, dependency injection, read/write separation, retries, and AWS RDS IAM authentication. |

See the package documentation for more detail:

- [Database](src/Database/README.md)
- [Entities](src/Entities/README.md)
- [Postgres](src/Postgres/README.md)

## Quick start

Install the PostgreSQL package:

```bash
dotnet add package Defra.Lis.Postgres
```

Configure a connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ReadWritePostgresConnection": "Host=localhost;Port=5432;Database=app;Username=postgres;Password=postgres"
  }
}
```

Register the contexts in `Program.cs`:

```csharp
using Defra.Database.Postgres;

builder.Services.AddPostgresDatabase(builder.Configuration);

var app = builder.Build();
app.UsePostgresDatabase();
```

`PostgresDbContext` is registered for reads and writes. `ReadOnlyPostgresDbContext` uses no-tracking queries and rejects calls to `SaveChanges` and `SaveChangesAsync`. If no read-only connection is configured, it uses the read/write connection.

For AWS RDS IAM configuration and detailed behavior, see the [Postgres package README](src/Postgres/README.md).

## Development

The repository requires the .NET SDK selected by `global.json` (currently .NET 10).

```bash
dotnet restore Database.slnx
dotnet build Database.slnx
dotnet test Database.slnx
```

Packages can be created locally with:

```bash
dotnet pack Database.slnx --configuration Release
```

## SonarCloud

Pull requests run build, test, coverage, and SonarCloud quality-gate checks in GitHub Actions. SonarCloud Automatic Analysis should remain disabled to prevent it conflicting with CI analysis.

## Licence

This project is licensed under the terms in [LICENSE](LICENSE).

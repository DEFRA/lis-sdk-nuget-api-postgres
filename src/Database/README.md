# Defra.Lis.Database

Shared Entity Framework Core abstractions used by the Defra database packages. This package is provider-independent and is intended for libraries or applications that need the common contracts without the PostgreSQL implementation.

## Installation

```bash
dotnet add package Defra.Lis.Database
```

## API

All types are in the `Defra.Database` namespace.

### `DbContextBase<T>`

An abstract, strongly typed base class over EF Core's `DbContext`:

```csharp
using Defra.Database;
using Microsoft.EntityFrameworkCore;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContextBase<AppDbContext>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
}
```

### `IDataSourceFactory<TSource>`

Defines a factory that resolves a data source by connection identifier. The PostgreSQL package supplies an `NpgsqlDataSource` implementation and uses the identifiers `Default` and `ReadOnly`.

### `ITokenGenerationService`

Defines asynchronous generation of a database authentication token from a host, port, and username. The PostgreSQL package implements this contract using AWS RDS IAM authentication.

## Related packages

- `Defra.Lis.Entities` provides reusable entity base classes.
- `Defra.Lis.Postgres` provides the PostgreSQL implementation and service registration.

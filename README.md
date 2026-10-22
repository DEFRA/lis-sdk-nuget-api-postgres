# lis-sdk-nuget-api-postgres

Part of the Livestock Information Service (LIS), which lets livestock keepers, owners, agents and
delegates record and manage their animals.

This repository builds three shared NuGet packages that give LIS .NET APIs a common way to use
PostgreSQL with Entity Framework Core: connecting with a standard connection string or with AWS IAM
authentication, separate read-write and read-only database contexts, retry on transient errors, and
base classes for entities.

Used by: [lis-api-cattle](https://github.com/DEFRA/lis-api-cattle)

## Installation

| Package | Install | What it provides |
| ------- | ------- | ---------------- |
| [`Defra.Lis.Postgres`](https://www.nuget.org/packages/Defra.Lis.Postgres) | `dotnet add package Defra.Lis.Postgres` | Npgsql and Entity Framework Core setup: `PostgresDbContext`, `ReadOnlyPostgresDbContext`, IAM token generation, and the `AddPostgresDatabase` and `UsePostgresDatabase` extension methods |
| [`Defra.Lis.Database`](https://www.nuget.org/packages/Defra.Lis.Database) | `dotnet add package Defra.Lis.Database` | Provider-neutral abstractions: `DbContextBase<T>`, `IDataSourceFactory<TSource>` and `ITokenGenerationService` |
| [`Defra.Lis.Entities`](https://www.nuget.org/packages/Defra.Lis.Entities) | `dotnet add package Defra.Lis.Entities` | Base entity classes: `BaseAuditEntity`, `BaseProcessingEntity` and `BaseTypeEntity` |

Most services only need `Defra.Lis.Postgres`, which depends on the other two. Install
`Defra.Lis.Entities` on its own in a project that defines entities but does not talk to the
database. All three target `net10.0`.

The namespaces are `Defra.Database` (`Defra.Lis.Database`), `Defra.Database.Postgres`
(`Defra.Lis.Postgres`) and `Defra.Database.Entities` (`Defra.Lis.Entities`).

## Usage

### Register the services

```csharp
using Defra.Database.Postgres;

builder.Services.AddPostgresDatabase(builder.Configuration);

var app = builder.Build();

app.UsePostgresDatabase();
```

`AddPostgresDatabase` reads the configuration described under [Configuration](#configuration) and
registers:

- `PostgresDbContext`, for reads and writes
- `ReadOnlyPostgresDbContext`, for reads only
- `IDataSourceFactory<NpgsqlDataSource>`, which creates the connections for both contexts
- `ITokenGenerationService`, which creates IAM tokens when IAM authentication is on (otherwise it
  is registered as `null`)
- the `PostgresConfiguration` settings, as a singleton

`UsePostgresDatabase` is optional. It opens a connection to the read-write database at start-up
if it can connect. A bad host or bad credentials do not fail start-up, because it skips the connection
when `CanConnect()` returns false. It does not run migrations or register a health check.

### Use the contexts

```csharp
public class HoldingService(PostgresDbContext writeDb, ReadOnlyPostgresDbContext readDb)
{
    public Task<Holding?> FindAsync(Guid id) =>
        readDb.Set<Holding>().FirstOrDefaultAsync(h => h.Id == id);

    public async Task AddAsync(Holding holding)
    {
        writeDb.Add(holding);
        await writeDb.SaveChangesAsync();
    }
}
```

- `PostgresDbContext` is a normal tracking context. On save it sets `ProcessedAt` to the current
  UTC time on every modified `BaseProcessingEntity`.
- `ReadOnlyPostgresDbContext` does not track entities. `SaveChanges` and `SaveChangesAsync` throw
  `InvalidOperationException`.
- Both contexts use the `public` schema and enable the `pgcrypto` and `citext` PostgreSQL
  extensions.
- Both contexts retry up to 5 times, waiting up to 10 seconds between tries, on transient errors
  (serialisation failures, deadlocks, lock timeouts, too many connections, server shutdown, I/O
  errors and connection errors). Commands time out after 60 seconds.
- Sensitive data logging is on, except when IAM authentication is on.

`PostgreExtensions` and `ColumnTypes` hold constants for extension names, default SQL values and
column types, for use in your entity configurations.

### Base entities

Inherit from the base class that fits the table:

| Class | Properties | Use for |
| ----- | ---------- | ------- |
| `BaseAuditEntity` | `Id`, `CreatedAt`, `CreatedById`, `DeletedAt`, `DeletedById` | Records that are created and soft-deleted by a user |
| `BaseProcessingEntity` | `Id`, `ReceivedAt`, `ProcessedAt` | Records for messages or events that are received, then processed |
| `BaseTypeEntity` | `Id`, `Name`, `Description` | Reference (lookup) data |

```csharp
using Defra.Database.Entities;

public class Holding : BaseAuditEntity
{
    public string Cph { get; set; } = string.Empty;
}
```

The base classes only declare properties. Your code sets the values, apart from `ProcessedAt`
(see above).

### Abstractions

`Defra.Lis.Database` holds the types that do not depend on PostgreSQL:

- `DbContextBase<T>` is the abstract base for `PostgresDbContext` and `ReadOnlyPostgresDbContext`.
- `IDataSourceFactory<TSource>` creates a data source from a connection identifier (`Default` or
  `ReadOnly`).
- `ITokenGenerationService` creates a database password token for a host, port and user.

## Configuration

`AddPostgresDatabase` reads from `IConfiguration`, so you can use `appsettings.json`, environment
variables or any other provider. In environment variables, use `__` for each level, for example
`PostgresConfiguration__UseIamAuthentication`.

### Standard connection strings

Used when `PostgresConfiguration:UseIamAuthentication` is `false` or not set.

```json
{
  "ConnectionStrings": {
    "ReadWritePostgresConnection": "Host=localhost;Port=5432;Database=my-db;Username=my-user;Password=my-password",
    "ReadOnlyPostgresConnection": "Host=localhost;Port=5432;Database=my-db;Username=my-user;Password=my-password"
  }
}
```

- `ReadWritePostgresConnection` is required. `PostgresDbContext` uses it.
- `ReadOnlyPostgresConnection` is used by `ReadOnlyPostgresDbContext`. If you leave it out, the
  read-only context uses the read-write connection.

If a connection string is missing, the first use of the context throws `InvalidOperationException`.

### AWS IAM authentication

Set `UseIamAuthentication` to `true` to connect to Amazon RDS with short-lived IAM tokens instead
of a password. The connection strings are ignored.

```json
{
  "PostgresConfiguration": {
    "UseIamAuthentication": true,
    "ReadWriteHost": "my-db.cluster-abc.eu-west-2.rds.amazonaws.com",
    "ReadOnlyHost": "my-db.cluster-ro-abc.eu-west-2.rds.amazonaws.com",
    "Port": 5432,
    "Name": "my-db",
    "User": "my-iam-database-user"
  },
  "AWS": {
    "Region": "eu-west-2"
  }
}
```

There is no fallback in this mode. If you do not set `ReadOnlyHost`, the read-only context cannot
connect, so set both hosts. Connections always use SSL. The token is renewed every 10 minutes, and
retried after 30 seconds if renewal fails. AWS credentials come from an `AWSCredentials` instance
registered in dependency injection, or otherwise from the default AWS credentials chain.

### Settings and defaults

| Key | Default | Meaning |
| --- | ------- | ------- |
| `ConnectionStrings:ReadWritePostgresConnection` | none | Read-write connection string (standard mode) |
| `ConnectionStrings:ReadOnlyPostgresConnection` | read-write connection string | Read-only connection string (standard mode) |
| `PostgresConfiguration:UseIamAuthentication` | `false` | Use IAM tokens instead of a password |
| `PostgresConfiguration:ReadWriteHost` | none | Read-write host (IAM mode) |
| `PostgresConfiguration:ReadOnlyHost` | none | Read-only host (IAM mode); no fallback |
| `PostgresConfiguration:Port` | `5432` | Port (IAM mode) |
| `PostgresConfiguration:Name` | none | Database name (IAM mode) |
| `PostgresConfiguration:User` | none | Database user (IAM mode) |
| `AWS:Region` | `eu-west-2` | Region used to sign IAM tokens (IAM mode) |

## Prerequisites

- .NET SDK 10 (the projects target `net10.0`)
- [Cake](https://cakebuild.net/) (`dotnet tool install -g Cake.Tool`), to run the build script locally
- To try a change in a consuming service: a local clone of [lis-dev](https://github.com/DEFRA/lis-dev), which runs a local NuGet server

## Setup

Inside a lis-dev checkout, restore fails with NU1507, because lis-dev's `nuget.config` lists two package sources without a package source mapping. Build from a standalone clone, or pass `--configfile` pointing at a config that lists only nuget.org.

### Development

```bash
dotnet restore Database.slnx
dotnet build Database.slnx
```

### Test

Tests use xUnit v3, with Shouldly for assertions, NSubstitute for mocks and the EF Core in-memory
provider. There is nothing to install beyond the .NET SDK. They do not need a database.

## Running in development

This is a library, so it has no service of its own. To try a change in a consuming service before
publishing, pack it and push it to the local NuGet server that lis-dev runs on port 5555
(`nuget-server` in its compose stack, already listed in lis-dev's `nuget.config`):

```bash
dotnet pack Database.slnx -c Release -o ./artifacts /p:Version=0.0.0-local.1
dotnet nuget push "./artifacts/*.nupkg" --source http://localhost:5555/v3/index.json --api-key LOCAL-NUGET-API-KEY
```

Then reference that version from the consuming service.

To run a service against a local database, lis-dev's compose stack runs PostgreSQL 16
(`postgres:16-alpine`).

## Running tests

```bash
dotnet test Database.slnx
```

## Pipelines

Build, versioning and publishing run in GitHub Actions (`.github/workflows`) using Cake scripts (`*.cake`):

- **Push to a `feature/` or `hotfix/` branch**: build, test, pack and Sonar analysis. This does not publish anything.
- **Pull request, approval of a pull request, or push to `main`**: build, test, pack and Sonar
  analysis, then, if the Sonar quality gate passes, tag the commit, publish all three packages to
  [nuget.org](https://www.nuget.org/profiles/Defra.Lis) and create a GitHub release

Versions come from [GitVersion](https://gitversion.net/) (`GitVersion.yml`) and `version.cake`.
Pull request workflows publish prereleases with the ticket in upper case, such as
`1.2.0-LREG-123-alpha.4`. NuGet treats versions as case-insensitive, so this is the same version
as `1.2.0-lreg-123-alpha.4`.

### Running the build locally

```bash
dotnet cake ./build.cake --target=Default --product_name=Defra.Lis.Database --solution_file_name=Database.slnx --package_version=0.1.0
```

### SonarCloud

Sonar analysis runs in CI through `.github/workflows/sonar.yml` and `sonar.cake`. Turn
**Automatic Analysis** off in the SonarCloud project (**Administration** > **Analysis Method**), or
it conflicts with the CI analysis.

## Branching policy

The repo uses mainline development. Branch from `main` and name the branch:

- `feature/LREG-<ticket>-<short-kebab-description>`
- `hotfix/LREG-<ticket>-<short-kebab-description>`

`LREG` must be uppercase, because the GitHub ruleset rejects other branch names. Rebase onto `main`
rather than merging `main` in.

## Contributing to this project

- Follow the [Defra software development standards](https://github.com/DEFRA/software-development-standards).
- Code style is enforced by StyleCop (`stylecop.json`) and `dotnet format`, which the build runs.
- Open a pull request against `main`. CI must pass and the Sonar quality gate must be green before merge.

## Licence

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

The following attribution statement MUST be cited in your products and applications when using this information.

> Contains public sector information licensed under the Open Government licence v3

### About the licence

The Open Government Licence (OGL) was developed by the Controller of His Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.

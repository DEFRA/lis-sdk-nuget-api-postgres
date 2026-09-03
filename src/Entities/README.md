# Defra.Lis.Entities

Small, reusable base entity types for Entity Framework Core models.

## Installation

```bash
dotnet add package Defra.Lis.Entities
```

## Entity types

All types are in the `Defra.Database.Entities` namespace.

| Type | Properties |
| --- | --- |
| `BaseAuditEntity` | `Id`, `CreatedAt`, `CreatedById`, `DeletedAt`, `DeletedById` |
| `BaseProcessingEntity` | `Id`, `ReceivedAt`, `ProcessedAt` |
| `BaseTypeEntity` | `Id`, `Name`, `Description` |

Derive an application entity from the base class that matches its lifecycle:

```csharp
using Defra.Database.Entities;

public sealed class Submission : BaseProcessingEntity
{
    public string Reference { get; set; } = string.Empty;
}
```

The classes contain no persistence configuration or validation, so applications remain responsible for mapping properties, assigning initial values, and enforcing their own rules.

When `BaseProcessingEntity` instances are updated through `Defra.Lis.Postgres`'s `PostgresDbContext`, their `ProcessedAt` value is set to the current UTC time before changes are saved.

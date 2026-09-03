// <copyright file="PostgresDbContext.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Postgres;

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Defra.Lis.Database;
using Defra.Lis.Entities;

public class PostgresDbContext(DbContextOptions<PostgresDbContext> options)
    : DbContextBase<PostgresDbContext>(options)
{
    public override int SaveChanges()
    {
        SetProcessingDateTimes();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetProcessingDateTimes();

        return base.SaveChangesAsync(cancellationToken);
    }

    protected virtual void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Constants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasPostgresExtension(PostgreExtensions.PgCrypto);
        modelBuilder.HasPostgresExtension(PostgreExtensions.Citext);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureModel(modelBuilder);
    }

    private void SetProcessingDateTimes()
    {
        foreach (var entry in ChangeTracker.Entries<BaseProcessingEntity>()
                     .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.ProcessedAt = DateTime.UtcNow;
        }
    }
}

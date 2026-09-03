// <copyright file="ReadOnlyPostgresDbContext.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Postgres;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Defra.Lis.Database;

public class ReadOnlyPostgresDbContext(DbContextOptions<ReadOnlyPostgresDbContext> options)
    : DbContextBase<ReadOnlyPostgresDbContext>(options)
{
    public override int SaveChanges()
    {
        throw new InvalidOperationException("This context is read-only.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("This context is read-only.");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Constants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasPostgresExtension(PostgreExtensions.PgCrypto);
        modelBuilder.HasPostgresExtension(PostgreExtensions.Citext);
    }
}

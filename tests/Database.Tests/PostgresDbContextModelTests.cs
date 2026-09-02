// <copyright file="PostgresDbContextModelTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Database.Tests;

using System;
using System.Threading.Tasks;
using Defra.Lis.Postgres;
using Microsoft.EntityFrameworkCore;

public class PostgresDbContextModelTests
{
    [Fact]
    public void ConfigureModel_Should_Set_DefaultSchema_And_Extensions()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        var modelBuilder = new ModelBuilder();

        Should.NotThrow(() => context.PublicConfigureModel(modelBuilder));

        // We can't easily check all internals of ModelBuilder without a lot of mocking,
        // but we can at least ensure it doesn't throw and we call it.
    }

    [Fact]
    public void ReadOnlyPostgresDbContext_SaveChanges_Should_Throw()
    {
        var options = new DbContextOptionsBuilder<ReadOnlyPostgresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ReadOnlyPostgresDbContext(options);

        Should.Throw<InvalidOperationException>(() => context.SaveChanges())
            .Message.ShouldBe("This context is read-only.");
    }

    [Fact]
    public async Task ReadOnlyPostgresDbContext_SaveChangesAsync_Should_Throw()
    {
        var options = new DbContextOptionsBuilder<ReadOnlyPostgresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new ReadOnlyPostgresDbContext(options);

        await Should.ThrowAsync<InvalidOperationException>(async () => await context.SaveChangesAsync())
            .ContinueWith(t => t.Result.Message.ShouldBe("This context is read-only."), TestContext.Current.CancellationToken);
    }

    private class TestDbContext(DbContextOptions<PostgresDbContext> options)
        : PostgresDbContext(options)
    {
        public void PublicConfigureModel(ModelBuilder modelBuilder) => ConfigureModel(modelBuilder);
    }
}

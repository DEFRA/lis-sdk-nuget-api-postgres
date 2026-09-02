// <copyright file="PostgresDbContextTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Database.Tests;

using System;
using System.Threading.Tasks;
using Defra.Lis.Entities;
using Defra.Lis.Postgres;
using Microsoft.EntityFrameworkCore;

public class PostgresDbContextTests
{
    [Fact]
    public void SaveChanges_Should_Set_ProcessedAt_On_Modified_ProcessingEntity()
    {
        var options = CreateOptions();
        using var context = new TestDbContext(options);

        var entity = new TestProcessingEntity { Id = Guid.NewGuid(), ReceivedAt = DateTime.UtcNow.AddHours(-1) };
        context.ProcessingEntities.Add(entity);
        context.SaveChanges();

        var initialProcessedAt = entity.ProcessedAt;

        entity.ReceivedAt = DateTime.UtcNow; // Modify
        context.SaveChanges();

        entity.ProcessedAt.ShouldNotBe(initialProcessedAt);
        entity.ProcessedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Set_ProcessedAt_On_Modified_ProcessingEntity()
    {
        var options = CreateOptions();
        await using var context = new TestDbContext(options);

        var entity = new TestProcessingEntity { Id = Guid.NewGuid(), ReceivedAt = DateTime.UtcNow.AddHours(-1) };
        context.ProcessingEntities.Add(entity);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var initialProcessedAt = entity.ProcessedAt;

        entity.ReceivedAt = DateTime.UtcNow; // Modify
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        entity.ProcessedAt.ShouldNotBe(initialProcessedAt);
        entity.ProcessedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void SaveChanges_Should_Not_Set_CreatedById_When_No_Added_AuditEntities()
    {
        var options = CreateOptions();
        using var context = new TestDbContext(options);

        var entity = new TestAuditEntity { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, CreatedById = Guid.NewGuid() };
        context.AuditEntities.Add(entity);
        context.SaveChanges();

        var createdById = entity.CreatedById;

        entity.CreatedAt = DateTime.UtcNow.AddMinutes(1); // Modify
        context.SaveChanges();

        entity.CreatedById.ShouldBe(createdById);
    }

    private static DbContextOptions<PostgresDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private class TestProcessingEntity : BaseProcessingEntity;

    private class TestAuditEntity : BaseAuditEntity;

    private class TestDbContext(DbContextOptions<PostgresDbContext> options)
        : PostgresDbContext(options)
    {
#pragma warning disable S3459 // sets are required for entity framework
#pragma warning disable S1144 // sets are required for entity framework
        public DbSet<TestProcessingEntity> ProcessingEntities { get; set; }

        public DbSet<TestAuditEntity> AuditEntities { get; set; }
#pragma warning restore S3459
#pragma warning restore S1144

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestProcessingEntity>();
            modelBuilder.Entity<TestAuditEntity>();
        }
    }
}

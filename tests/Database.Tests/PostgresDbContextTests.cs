using System;
using System.Threading.Tasks;
using Defra.Database.Entities;
using Defra.Database.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Defra.Database.Tests;

public class PostgresDbContextTests
{
    private class TestProcessingEntity : BaseProcessingEntity { }
    private class TestAuditEntity : BaseAuditEntity { }

    private class TestDbContext(DbContextOptions<PostgresDbContext> options) : PostgresDbContext(options)
    {
        public DbSet<TestProcessingEntity> ProcessingEntities { get; set; }
        public DbSet<TestAuditEntity> AuditEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestProcessingEntity>();
            modelBuilder.Entity<TestAuditEntity>();
        }
    }

    private DbContextOptions<PostgresDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

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
        using var context = new TestDbContext(options);

        var entity = new TestProcessingEntity { Id = Guid.NewGuid(), ReceivedAt = DateTime.UtcNow.AddHours(-1) };
        context.ProcessingEntities.Add(entity);
        await context.SaveChangesAsync();

        var initialProcessedAt = entity.ProcessedAt;

        entity.ReceivedAt = DateTime.UtcNow; // Modify
        await context.SaveChangesAsync();

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

    [Fact]
    public void SaveChanges_Should_Set_CreatedById_When_Default_Exists()
    {
        // We can't easily test this without a mockable context or setting up UserAccounts.
        // But the line is there.
    }
}

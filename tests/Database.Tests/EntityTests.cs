using System;
using Defra.Database.Entities;

namespace Defra.Database.Tests;

public class EntityTests
{
    private class TestProcessingEntity : BaseProcessingEntity { }
    private class TestAuditEntity : BaseAuditEntity { }
    private class TestTypeEntity : BaseTypeEntity { }

    [Fact]
    public void BaseProcessingEntity_Should_Store_Properties()
    {
        var id = Guid.NewGuid();
        var receivedAt = DateTime.UtcNow;
        var processedAt = DateTime.UtcNow.AddMinutes(1);

        var entity = new TestProcessingEntity
        {
            Id = id,
            ReceivedAt = receivedAt,
            ProcessedAt = processedAt
        };

        entity.Id.ShouldBe(id);
        entity.ReceivedAt.ShouldBe(receivedAt);
        entity.ProcessedAt.ShouldBe(processedAt);
    }

    [Fact]
    public void BaseAuditEntity_Should_Store_Properties()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var createdById = Guid.NewGuid();
        var deletedAt = DateTime.UtcNow.AddDays(1);
        var deletedById = Guid.NewGuid();

        var entity = new TestAuditEntity
        {
            Id = id,
            CreatedAt = createdAt,
            CreatedById = createdById,
            DeletedAt = deletedAt,
            DeletedById = deletedById
        };

        entity.Id.ShouldBe(id);
        entity.CreatedAt.ShouldBe(createdAt);
        entity.CreatedById.ShouldBe(createdById);
        entity.DeletedAt.ShouldBe(deletedAt);
        entity.DeletedById.ShouldBe(deletedById);
    }

    [Fact]
    public void BaseTypeEntity_Should_Store_Properties()
    {
        var id = Guid.NewGuid();
        var name = "Test Name";
        var description = "Test Description";

        var entity = new TestTypeEntity
        {
            Id = id,
            Name = name,
            Description = description
        };

        entity.Id.ShouldBe(id);
        entity.Name.ShouldBe(name);
        entity.Description.ShouldBe(description);
    }
}

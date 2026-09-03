// <copyright file="BaseAuditEntity.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Entities;

public abstract class BaseAuditEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid CreatedById { get; set; }

    public DateTime? DeletedAt { get; set; }

    public Guid? DeletedById { get; set; }
}

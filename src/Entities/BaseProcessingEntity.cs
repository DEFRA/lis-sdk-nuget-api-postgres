// <copyright file="BaseProcessingEntity.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

using System;

namespace Defra.Database.Entities;

public abstract class BaseProcessingEntity
{
    public Guid Id { get; set; }

    public DateTime ReceivedAt { get; set; }

    public DateTime ProcessedAt { get; set; }
}
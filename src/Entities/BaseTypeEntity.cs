// <copyright file="BaseTypeEntity.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

using System;

namespace Defra.Database.Entities;

public abstract class BaseTypeEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
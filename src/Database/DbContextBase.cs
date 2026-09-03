// <copyright file="DbContextBase.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Database;

public abstract class DbContextBase<T>(DbContextOptions<T> options) : DbContext(options)
    where T : DbContext
{
}

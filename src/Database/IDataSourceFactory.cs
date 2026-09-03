// <copyright file="IDataSourceFactory.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Database;

public interface IDataSourceFactory<out TSource>
{
    TSource CreateDataSource(string connectionIdentifier);
}

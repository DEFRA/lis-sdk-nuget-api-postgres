// <copyright file="PostgresDataSourceFactoryTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Database.Tests;

using System;
using Defra.Lis.Postgres;
using NSubstitute;

public class PostgresDataSourceFactoryTests
    : IDisposable
{
    private readonly PostgresConfiguration defaultConfig;
    private readonly ITokenGenerationService tokenService;
    private readonly PostgresDataSourceFactory defaultFactory;

    public PostgresDataSourceFactoryTests()
    {
        defaultConfig = new PostgresConfiguration
        {
            ConnectionString = "Host=localhost;Database=test",
            ReadOnlyConnectionString = "Host=readonly;Database=test",
            ReadWriteHost = "default-host",
            ReadOnlyHost = "readonly-host",
            Port = 5432,
            Name = "test-db",
            User = "test-user",
        };
        tokenService = Substitute.For<ITokenGenerationService>();
        defaultFactory = new PostgresDataSourceFactory(defaultConfig, tokenService);
    }

    [Fact]
    public void CreateDataSource_Standard_Default_Returns_DataSource()
    {
        defaultConfig.UseIamAuthentication = false;
        var dataSource = defaultFactory.CreateDataSource("Default");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=localhost");
    }

    [Fact]
    public void CreateDataSource_Standard_ReadOnly_Returns_DataSource()
    {
        defaultConfig.UseIamAuthentication = false;
        var dataSource = defaultFactory.CreateDataSource("ReadOnly");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=readonly");
    }

    [Fact]
    public void CreateDataSource_Standard_ReadOnly_FallsBack_To_Default_If_Missing()
    {
        var config = new PostgresConfiguration { ConnectionString = "Host=fallback" };
        var factory = new PostgresDataSourceFactory(config, tokenService);

        var dataSource = factory.CreateDataSource("ReadOnly");

        dataSource.ConnectionString.ShouldContain("Host=fallback");
    }

    [Fact]
    public void CreateDataSource_Standard_ReadOnly_WithExplicitNull_FallsBack_To_Default()
    {
        var config = new PostgresConfiguration
        {
            ConnectionString = "Host=fallback",
            ReadOnlyConnectionString = null!,
        };
        var factory = new PostgresDataSourceFactory(config, tokenService);

        var dataSource = factory.CreateDataSource("ReadOnly");

        dataSource.ConnectionString.ShouldContain("Host=fallback");
    }

    [Fact]
    public void CreateDataSource_Throws_On_Unknown_Identifier()
    {
        Should.Throw<ArgumentException>(() => defaultFactory.CreateDataSource("Unknown"));
    }

    [Fact]
    public void CreateDataSource_Throws_On_Missing_ConnectionString()
    {
        var config = new PostgresConfiguration();
        var factory = new PostgresDataSourceFactory(config, tokenService);

        Should.Throw<InvalidOperationException>(() => factory.CreateDataSource("Default"));
    }

    [Fact]
    public void CreateDataSource_Caches_DataSource()
    {
        var ds1 = defaultFactory.CreateDataSource("Default");
        var ds2 = defaultFactory.CreateDataSource("Default");

        ds1.ShouldBeSameAs(ds2);
    }

    [Fact]
    public void CreateDataSource_Iam_Returns_DataSource()
    {
        defaultConfig.UseIamAuthentication = true;
        var dataSource = defaultFactory.CreateDataSource("Default");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=default-host");
    }

    [Fact]
    public void CreateDataSource_Iam_ReadOnly_Returns_DataSource()
    {
        defaultConfig.UseIamAuthentication = true;
        var dataSource = defaultFactory.CreateDataSource("ReadOnly");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=readonly-host");
    }

    [Fact]
    public void Dispose_Disposes_DataSources()
    {
        _ = defaultFactory.CreateDataSource("Default");
        defaultFactory.Dispose();

        Should.Throw<ObjectDisposedException>(() => defaultFactory.CreateDataSource("Default"));
    }

    [Fact]
    public void Dispose_Should_Be_Idempotent()
    {
        defaultFactory.Dispose();
        Should.NotThrow(() => defaultFactory.Dispose()); // Should not throw
    }

    public void Dispose()
    {
        defaultFactory.Dispose();
    }
}

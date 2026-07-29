using System;
using Defra.Database.Postgres;
using NSubstitute;

namespace Defra.Database.Tests;

public class PostgresDataSourceFactoryTests : IDisposable
{
    private readonly PostgresConfiguration _config;
    private readonly ITokenGenerationService _tokenService;
    private readonly PostgresDataSourceFactory _factory;

    public PostgresDataSourceFactoryTests()
    {
        _config = new PostgresConfiguration
        {
            ConnectionString = "Host=localhost;Database=test",
            ReadOnlyConnectionString = "Host=readonly;Database=test",
            ReadWriteHost = "default-host",
            ReadOnlyHost = "readonly-host",
            Port = 5432,
            Name = "test-db",
            User = "test-user"
        };
        _tokenService = Substitute.For<ITokenGenerationService>();
        _factory = new PostgresDataSourceFactory(_config, _tokenService);
    }

    [Fact]
    public void CreateDataSource_Standard_Default_Returns_DataSource()
    {
        _config.UseIamAuthentication = false;
        var dataSource = _factory.CreateDataSource("Default");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=localhost");
    }

    [Fact]
    public void CreateDataSource_Standard_ReadOnly_Returns_DataSource()
    {
        _config.UseIamAuthentication = false;
        var dataSource = _factory.CreateDataSource("ReadOnly");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=readonly");
    }

    [Fact]
    public void CreateDataSource_Standard_ReadOnly_FallsBack_To_Default_If_Missing()
    {
        var config = new PostgresConfiguration { ConnectionString = "Host=fallback" };
        var factory = new PostgresDataSourceFactory(config, _tokenService);

        var dataSource = factory.CreateDataSource("ReadOnly");

        dataSource.ConnectionString.ShouldContain("Host=fallback");
    }

    [Fact]
    public void CreateDataSource_Standard_ReadOnly_WithExplicitNull_FallsBack_To_Default()
    {
        var config = new PostgresConfiguration
        {
            ConnectionString = "Host=fallback",
            ReadOnlyConnectionString = null!
        };
        var factory = new PostgresDataSourceFactory(config, _tokenService);

        var dataSource = factory.CreateDataSource("ReadOnly");

        dataSource.ConnectionString.ShouldContain("Host=fallback");
    }

    [Fact]
    public void CreateDataSource_Throws_On_Unknown_Identifier()
    {
        Should.Throw<ArgumentException>(() => _factory.CreateDataSource("Unknown"));
    }

    [Fact]
    public void CreateDataSource_Throws_On_Missing_ConnectionString()
    {
        var config = new PostgresConfiguration();
        var factory = new PostgresDataSourceFactory(config, _tokenService);

        Should.Throw<InvalidOperationException>(() => factory.CreateDataSource("Default"));
    }

    [Fact]
    public void CreateDataSource_Caches_DataSource()
    {
        var ds1 = _factory.CreateDataSource("Default");
        var ds2 = _factory.CreateDataSource("Default");

        ds1.ShouldBeSameAs(ds2);
    }

    [Fact]
    public void CreateDataSource_Iam_Returns_DataSource()
    {
        _config.UseIamAuthentication = true;
        var dataSource = _factory.CreateDataSource("Default");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=default-host");
    }

    [Fact]
    public void CreateDataSource_Iam_ReadOnly_Returns_DataSource()
    {
        _config.UseIamAuthentication = true;
        var dataSource = _factory.CreateDataSource("ReadOnly");

        dataSource.ShouldNotBeNull();
        dataSource.ConnectionString.ShouldContain("Host=readonly-host");
    }

    [Fact]
    public void Dispose_Disposes_DataSources()
    {
        var dataSource = _factory.CreateDataSource("Default");
        _factory.Dispose();

        Should.Throw<ObjectDisposedException>(() => _factory.CreateDataSource("Default"));
    }

    [Fact]
    public void Dispose_Should_Be_Idempotent()
    {
        _factory.Dispose();
        _factory.Dispose(); // Should not throw
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}

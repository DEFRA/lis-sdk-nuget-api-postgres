#nullable enable
using System.Collections.Generic;
using Defra.Database.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Defra.Database.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPostgresDatabase_Should_Register_Services()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresConnection"] = "Host=localhost;Database=test",
                ["PostgresConfiguration:Port"] = "5432"
            })
            .Build();

        services.AddSingleton(Substitute.For<ILoggerFactory>());

        services.AddPostgresDatabase(configuration);

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<PostgresConfiguration>().ShouldNotBeNull();
        serviceProvider.GetService<IDataSourceFactory<Npgsql.NpgsqlDataSource>>().ShouldNotBeNull();
        serviceProvider.GetService<PostgresDbContext>().ShouldNotBeNull();
        serviceProvider.GetService<ReadOnlyPostgresDbContext>().ShouldNotBeNull();
    }

    [Fact]
    public void AddPostgresDatabase_WithIam_Should_Register_TokenService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PostgresConfiguration:UseIamAuthentication"] = "true",
                ["AWS:Region"] = "eu-west-1"
            })
            .Build();

        services.AddSingleton(Substitute.For<ILoggerFactory>());
        services.AddSingleton(Substitute.For<Amazon.Runtime.AWSCredentials>());

        services.AddPostgresDatabase(configuration);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<ITokenGenerationService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddPostgresDatabase_WithIam_DefaultRegion_Should_Register_TokenService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PostgresConfiguration:UseIamAuthentication"] = "true",
                // Region is missing, should default to eu-west-2
            })
            .Build();

        services.AddSingleton(Substitute.For<ILoggerFactory>());
        services.AddSingleton(Substitute.For<Amazon.Runtime.AWSCredentials>());

        services.AddPostgresDatabase(configuration);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<ITokenGenerationService>().ShouldNotBeNull();
    }

    [Fact]
    public void AddPostgresDatabase_Without_ConnectionString_Should_Not_Throw()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddSingleton(Substitute.For<ILoggerFactory>());
        services.AddPostgresDatabase(configuration);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<PostgresConfiguration>().ShouldNotBeNull();
    }

    [Fact]
    public void UsePostgresDatabase_Should_Not_Throw()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresConnection"] = "Host=localhost;Database=test",
            })
            .Build();

        services.AddSingleton(Substitute.For<ILoggerFactory>());
        services.AddPostgresDatabase(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var app = Substitute.For<Microsoft.AspNetCore.Builder.IApplicationBuilder>();
        app.ApplicationServices.Returns(serviceProvider);

        app.UsePostgresDatabase();
    }
}

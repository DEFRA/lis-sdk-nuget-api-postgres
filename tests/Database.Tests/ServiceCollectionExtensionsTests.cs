#nullable enable
using System.Collections.Generic;
using Defra.Database.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
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
                ["ConnectionStrings:ReadWritePostgresConnection"] = "Host=localhost;Database=test",
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

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("false", null, false)]
    [InlineData("false", "false", false)]
    [InlineData("true", null, false)]
    [InlineData("false", "true", true)]
    [InlineData("true", "true", true)]
    public void AddPostgresDatabase_Should_Only_Enable_Sensitive_Data_Logging_When_Configured(
        string? useIamAuthentication,
        string? enableSensitiveDataLogging,
        bool expected)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:ReadWritePostgresConnection"] = "Host=localhost;Database=test",
            ["PostgresConfiguration:ReadWriteHost"] = "localhost",
            ["PostgresConfiguration:ReadOnlyHost"] = "localhost",
            ["PostgresConfiguration:Name"] = "test",
            ["PostgresConfiguration:User"] = "test",
        };

        if (useIamAuthentication != null)
        {
            settings["PostgresConfiguration:UseIamAuthentication"] = useIamAuthentication;
        }

        if (enableSensitiveDataLogging != null)
        {
            settings["PostgresConfiguration:EnableSensitiveDataLogging"] = enableSensitiveDataLogging;
        }

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        services.AddSingleton(Substitute.For<ILoggerFactory>());
        services.AddSingleton(Substitute.For<Amazon.Runtime.AWSCredentials>());
        services.AddPostgresDatabase(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();

        IsSensitiveDataLoggingEnabled(scope.ServiceProvider.GetRequiredService<PostgresDbContext>()).ShouldBe(expected);
        IsSensitiveDataLoggingEnabled(scope.ServiceProvider.GetRequiredService<ReadOnlyPostgresDbContext>()).ShouldBe(expected);
    }

    [Fact]
    public void UsePostgresDatabase_Should_Not_Throw()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ReadWritePostgresConnection"] = "Host=localhost;Database=test",
            })
            .Build();

        services.AddSingleton(Substitute.For<ILoggerFactory>());
        services.AddPostgresDatabase(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var app = Substitute.For<Microsoft.AspNetCore.Builder.IApplicationBuilder>();
        app.ApplicationServices.Returns(serviceProvider);

        app.UsePostgresDatabase();
    }

    [Fact]
    public void UsePostgresDatabase_Should_Log_Warning_When_Pending_Model_Changes_Are_Reported()
    {
        var logger = Substitute.For<ILogger>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);

        var app = CreateAppWithPendingModelChanges(loggerFactory);

        Should.NotThrow(() => app.UsePostgresDatabase());

        loggerFactory.Received(1).CreateLogger("Defra.Database.Postgres");
        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("pending model changes")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void UsePostgresDatabase_Should_Not_Throw_When_Pending_Model_Changes_Are_Reported_Without_A_Logger_Factory()
    {
        var app = CreateAppWithPendingModelChanges(loggerFactory: null);

        Should.NotThrow(() => app.UsePostgresDatabase());
    }

    private static Microsoft.AspNetCore.Builder.IApplicationBuilder CreateAppWithPendingModelChanges(
        ILoggerFactory? loggerFactory)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new PendingModelChangesDbContext(
            new DbContextOptionsBuilder<PostgresDbContext>().UseInMemoryDatabase("pending-model-changes").Options));
        services.AddScoped<PostgresDbContext>(sp => sp.GetRequiredService<PendingModelChangesDbContext>());

        if (loggerFactory != null)
        {
            services.AddSingleton(loggerFactory);
        }

        var app = Substitute.For<Microsoft.AspNetCore.Builder.IApplicationBuilder>();
        app.ApplicationServices.Returns(services.BuildServiceProvider());

        return app;
    }

    private static bool IsSensitiveDataLoggingEnabled(DbContext context) =>
        context.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?
            .IsSensitiveDataLoggingEnabled ?? false;

    private sealed class PendingModelChangesDbContext(DbContextOptions<PostgresDbContext> options)
        : PostgresDbContext(options)
    {
        private DatabaseFacade? facade;

        public override DatabaseFacade Database => facade ??= new PendingModelChangesDatabaseFacade(this);
    }

    private sealed class PendingModelChangesDatabaseFacade(DbContext context) : DatabaseFacade(context)
    {
        public override bool CanConnect() =>
            throw new InvalidOperationException(
                "An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning'.");
    }
}

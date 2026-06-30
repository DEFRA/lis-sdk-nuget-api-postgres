using System;
using Amazon;
using Amazon.Runtime;
using Amazon.Runtime.Credentials;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Defra.Database.Postgres;

public static class ServiceCollectionExtensions
{
    private const int MaxRetryCount = 5;
    private const int MaxRetryDelay = 10;

    private const int CommandTimeout = 60;

    /// <summary>
    /// Common PostgreSQL SQLSTATEs often considered transient:
    /// 40001: Serialization failure
    /// 40P01: Deadlock detected
    /// 55P03: Lock not available
    /// 53300: Too many connections
    /// 57014: Query canceled
    /// 57P01: Admin shutdown
    /// 57P02: Crash shutdown
    /// 57P03: Cannot connect now
    /// 58030: I/O error
    /// 08000/08003/08006/08001/08004/08007/08P01: Connection-related errors (connection exception class 08).
    /// </summary>
    private static readonly string[] ErrorCodes =
    [
        "40001", "40P01", "55P03", "53300", "57014", "57P01", "57P02", "57P03", "58030",
        "08000", "08001", "08003", "08004", "08006", "08007", "08P01",
    ];

    public static IServiceCollection AddPostgresDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConfig = GetPostgresConfiguration(configuration);
        services.AddSingleton(postgresConfig);

        if (postgresConfig.UseIamAuthentication)
        {
            services.AddSingleton<ITokenGenerationService>(sp =>
            {
                var credentials = sp.GetService<AWSCredentials>()
                                  ?? DefaultAWSCredentialsIdentityResolver.GetCredentials();
                var region = RegionEndpoint.GetBySystemName(
                    configuration.GetValue<string>("AWS:Region") ?? "eu-west-2");
                return new TokenGenerationService(credentials, region);
            });
        }
        else
        {
            services.AddSingleton<ITokenGenerationService>(_ => null!);
        }

        services.AddScoped<IDataSourceFactory<NpgsqlDataSource>, PostgresDataSourceFactory>();

        services.AddDbContext<PostgresDbContext>((sp, options) =>
        {
            var dataSourceFactory = sp.GetRequiredService<IDataSourceFactory<NpgsqlDataSource>>();
            var dataSource = dataSourceFactory.CreateDataSource("Default");
            ConfigureNpgsql(sp, options, dataSource, postgresConfig.UseIamAuthentication);
        });

        services.AddDbContext<ReadOnlyPostgresDbContext>((sp, options) =>
        {
            var dataSourceFactory = sp.GetRequiredService<IDataSourceFactory<NpgsqlDataSource>>();
            var dataSource = dataSourceFactory.CreateDataSource("ReadOnly");
            ConfigureNpgsql(sp, options, dataSource, postgresConfig.UseIamAuthentication);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        return services;
    }

    public static void UsePostgresDatabase(this Microsoft.AspNetCore.Builder.IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        try
        {
            if (context.Database.CanConnect())
            {
                context.Database.OpenConnection();
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
        {
            // Ignore pending model changes warning during tests/dev if it's treated as error
        }
    }

    private static void ConfigureNpgsql(
        IServiceProvider sp,
        DbContextOptionsBuilder options,
        NpgsqlDataSource dataSource,
        bool isIamAuth)
    {
        options
            .UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>())
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseNpgsql(
                dataSource,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: MaxRetryCount,
                        maxRetryDelay: TimeSpan.FromSeconds(MaxRetryDelay),
                        errorCodesToAdd: ErrorCodes);
                    npgsqlOptions.CommandTimeout(CommandTimeout);
                })
            .EnableSensitiveDataLogging(!isIamAuth);
    }

    private static PostgresConfiguration GetPostgresConfiguration(IConfiguration configuration)
    {
        var postgresConfig = configuration
                                 .GetSection(nameof(PostgresConfiguration))
                                 .Get<PostgresConfiguration>()
                             ?? new PostgresConfiguration();

        postgresConfig.ConnectionString =
            configuration.GetConnectionString(Constants.ConnectionStringName) ?? string.Empty;
        postgresConfig.ReadOnlyConnectionString =
            configuration.GetConnectionString(Constants.ReadOnlyConnectionStringName) ?? string.Empty;

        return postgresConfig;
    }
}
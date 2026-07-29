using System;
using System.Collections.Generic;
using System.Threading;
using Npgsql;

namespace Defra.Database.Postgres;

public class PostgresDataSourceFactory(PostgresConfiguration configuration,
    ITokenGenerationService tokenGenerationService
    ) : IDataSourceFactory<NpgsqlDataSource>, IDisposable
{
    private const string DefaultConnectionIdentifier = "Default";
    private const string ReadOnlyConnectionIdentifier = "ReadOnly";

    private readonly Dictionary<string, NpgsqlDataSource> dataSources = new();
    private readonly SemaphoreSlim @lock = new(1, 1);
    private bool disposed;


    public NpgsqlDataSource CreateDataSource(string connectionIdentifier)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        // Use cached data source if available
        if (dataSources.TryGetValue(connectionIdentifier, out var existingDataSource))
        {
            return existingDataSource;
        }

        @lock.Wait();
        try
        {
            // Double-check after acquiring lock
            if (dataSources.TryGetValue(connectionIdentifier, out existingDataSource))
            {
                return existingDataSource;
            }

            NpgsqlDataSource dataSource;

            if (configuration.UseIamAuthentication)
            {
                dataSource = CreateIamAuthDataSource(connectionIdentifier);
            }
            else
            {
                dataSource = CreateStandardDataSource(connectionIdentifier);
            }

            dataSources[connectionIdentifier] = dataSource;
            return dataSource;
        }
        finally
        {
            @lock.Release();
        }
    }

    private NpgsqlDataSource CreateStandardDataSource(string connectionIdentifier)
    {
        var connectionString = connectionIdentifier switch
        {
            DefaultConnectionIdentifier => configuration.ConnectionString,
            ReadOnlyConnectionIdentifier => configuration.ReadOnlyConnectionString,
            _ => throw new ArgumentException($"Unknown connection identifier: {connectionIdentifier}"),
        };

        if (string.IsNullOrEmpty(connectionString) && connectionIdentifier == ReadOnlyConnectionIdentifier)
        {
            connectionString = configuration.ConnectionString;
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"Connection string for '{connectionIdentifier}' is missing.");
        }

        return NpgsqlDataSource.Create(connectionString);
    }

    private NpgsqlDataSource CreateIamAuthDataSource(string connectionIdentifier)
    {
        var host = connectionIdentifier switch
        {
            DefaultConnectionIdentifier => configuration.ReadWriteHost,
            ReadOnlyConnectionIdentifier => configuration.ReadOnlyHost,
            _ => throw new ArgumentException($"Unknown connection identifier: {connectionIdentifier}"),
        };

        var builder = new NpgsqlDataSourceBuilder
        {
            ConnectionStringBuilder =
            {
                Host = host,
                Port = configuration.Port,
                Database = configuration.Name,
                Username = configuration.User,
                SslMode = SslMode.Require, // AWS RDS requires SSL
                Pooling = true,
            },
        };

        // Register password provider that generates IAM tokens
        builder.UsePeriodicPasswordProvider(
            passwordProvider: async (_, _) =>
            {
                var token = await tokenGenerationService!.GenerateTokenAsync(
                    host,
                    configuration.Port,
                    configuration.User!);
                return token;
            },
            successRefreshInterval: TimeSpan.FromMinutes(10), // Refresh every 10 minutes
            failureRefreshInterval: TimeSpan.FromSeconds(30)); // Retry after 30 seconds on failure

        return builder.Build();
    }

#pragma warning disable SA1202
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var dataSource in dataSources.Values)
            {
                dataSource.Dispose();
            }

            dataSources.Clear();
            @lock.Dispose();
        }

        disposed = true;
    }
#pragma warning restore SA1202
}
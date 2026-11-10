namespace Defra.Database.Postgres;

public class PostgresConfiguration
{
    public bool UseIamAuthentication { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether EF Core includes parameter values and entity data in
    /// its logs and exception messages. Off by default, because those values can carry personal or
    /// sensitive data. Only turn it on for local debugging.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    public string ReadWriteHost { get; init; } = string.Empty;

    public string ReadOnlyHost { get; init; } = string.Empty;

    public int Port { get; init; } = 5432;

    public string Name { get; init; } = string.Empty;

    public string User { get; init; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public string ReadOnlyConnectionString { get; set; } = string.Empty;
}
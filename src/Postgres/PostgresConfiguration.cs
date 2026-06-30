namespace Defra.Database.Postgres;

public class PostgresConfiguration
{
    public bool UseIamAuthentication { get; set; } = false;

    public string DefaultHost { get; init; } = string.Empty;

    public string ReadOnlyHost { get; init; } = string.Empty;

    public int Port { get; init; } = 5432;

    public string Name { get; init; } = string.Empty;

    public string User { get; init; } = string.Empty;

    public string ConnectionString { get; set; } = string.Empty;

    public string ReadOnlyConnectionString { get; set; } = string.Empty;
}
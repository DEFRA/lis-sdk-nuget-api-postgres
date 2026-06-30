namespace Defra.Database;

public interface ITokenGenerationService
{
    Task<string> GenerateTokenAsync(string hostname, int port, string username);
}
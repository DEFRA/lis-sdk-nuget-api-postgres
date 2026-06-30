using System.Threading.Tasks;
using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;

namespace Defra.Database.Postgres;

public class TokenGenerationService(AWSCredentials credentials, RegionEndpoint region)
    : ITokenGenerationService
{
    public Task<string> GenerateTokenAsync(string hostname, int port, string username)
    {
        var token = RDSAuthTokenGenerator.GenerateAuthToken(credentials, region, hostname, port, username);
        return Task.FromResult(token);
    }
}
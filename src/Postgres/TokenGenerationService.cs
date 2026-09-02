// <copyright file="TokenGenerationService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Postgres;

using System.Threading.Tasks;
using Amazon;
using Amazon.RDS.Util;
using Amazon.Runtime;
using Defra.Lis.Database;

public class TokenGenerationService(
    AWSCredentials credentials,
    RegionEndpoint region)
    : ITokenGenerationService
{
    public Task<string> GenerateTokenAsync(string hostname, int port, string username)
    {
        var token = RDSAuthTokenGenerator.GenerateAuthToken(credentials, region, hostname, port, username);
        return Task.FromResult(token);
    }
}

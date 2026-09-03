// <copyright file="TokenGenerationServiceTests.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Database.Tests;

using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Defra.Lis.Postgres;

public class TokenGenerationServiceTests
{
    [Fact]
    public async Task GenerateTokenAsync_Should_Return_Token()
    {
        var credentials = new BasicAWSCredentials("accessKey", "secretKey");
        var region = RegionEndpoint.EUWest2;
        var service = new TokenGenerationService(credentials, region);

        var token = await service.GenerateTokenAsync("localhost", 5432, "user");

        token.ShouldNotBeNullOrEmpty();

        // RDS tokens usually start with the hostname or have a specific format
        token.ShouldContain("localhost");
    }
}

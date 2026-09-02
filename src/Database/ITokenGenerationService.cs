// <copyright file="ITokenGenerationService.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Database;

using System.Threading.Tasks;

public interface ITokenGenerationService
{
    Task<string> GenerateTokenAsync(string hostname, int port, string username);
}

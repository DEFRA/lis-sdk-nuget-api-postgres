// <copyright file="PostgreExtensions.cs" company="Defra">
// Copyright (c) Defra. All rights reserved.
// </copyright>

namespace Defra.Lis.Postgres;

public static class PostgreExtensions
{
    public const string UuidGenerator = "uuid-ossp";
    public const string UuidAlgorithm = "gen_random_uuid()";
    public const string PostGis = "postgis";
    public const string PgCrypto = "pgcrypto";
    public const string PgAudit = "pgaudit";
    public const string PgTerm = "pg_term";
    public const string FuzzyStrMatch = "fuzzystrmatch";
    public const string Citext = "citext";
    public const string Now = "now()";
}

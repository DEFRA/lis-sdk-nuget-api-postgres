using Defra.Database.Postgres;

namespace Defra.Database.Tests;

public class ConstantsAndExtensionsTests
{
    [Fact]
    public void ColumnTypes_Should_Have_Correct_Values()
    {
        ColumnTypes.UniqueIdentifier.ShouldBe("uuid");
        ColumnTypes.Boolean.ShouldBe("boolean");
        ColumnTypes.Varchar.ShouldBe("varchar");
        ColumnTypes.Text.ShouldBe("text");
        ColumnTypes.DateTimeOffSet.ShouldBe("timestamp with time zone");
        ColumnTypes.CiText.ShouldBe("citext");
    }

    [Fact]
    public void PostgreExtensions_Should_Have_Correct_Values()
    {
        PostgreExtensions.PgCrypto.ShouldBe("pgcrypto");
        PostgreExtensions.Citext.ShouldBe("citext");
        PostgreExtensions.Now.ShouldBe("now()");
        PostgreExtensions.UuidAlgorithm.ShouldBe("gen_random_uuid()");
    }
}

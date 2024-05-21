namespace Serilog.Redaction.Tests;

public record struct TestArray([property: HashedData]IReadOnlyList<TestArray.Child> MaskedChildren, IReadOnlyList<TestArray.Child> NotMaskedChildren)
{
    public record struct Child(string NotMasked, [property: HashedData] int Number);
}
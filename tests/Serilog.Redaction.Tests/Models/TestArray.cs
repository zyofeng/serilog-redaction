using Serilog.Redaction.Tests.Infrastructure;

namespace Serilog.Redaction.Tests.Models;

public readonly record struct TestArray(
    [property: HashedData] IReadOnlyList<TestArray.Child> MaskedChildren,
    IReadOnlyList<TestArray.Child> NotMaskedChildren)
{
    public readonly record struct Child(string NotMasked, [property: HashedData] int Number);
}

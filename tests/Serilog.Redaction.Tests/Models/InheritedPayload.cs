using Serilog.Redaction.Tests.Infrastructure;

namespace Serilog.Redaction.Tests.Models;

public record BasePayload
{
    public string? BaseMessage { get; init; }

    [HashedData] public string? BaseLastName { get; init; }
    [ErasedData] public string? BaseTaxNumber { get; init; }

    [HashedData] public virtual string? Email { get; init; }
}

// Classified properties exist only on the base record.
public sealed record DerivedWithoutClassifications : BasePayload
{
    public string? DerivedMessage { get; init; }
}

// Both base and derived records declare classified properties.
public sealed record DerivedWithClassifications : BasePayload
{
    public string? DerivedMessage { get; init; }

    [HashedData] public string? DerivedFirstName { get; init; }
}

// Overrides a classified property without re-applying the attribute.
public sealed record DerivedWithOverride : BasePayload
{
    public override string? Email { get; init; }
}

// Hides an unclassified base property with a classified one.
public sealed record DerivedWithHiddenProperty : BasePayload
{
    [HashedData] public new string? BaseMessage { get; init; }
}

public record BasePositionalPayload(string Message, [property: HashedData] string LastName);

public sealed record DerivedPositionalPayload(string Message, string LastName, [property: ErasedData] string TaxNumber)
    : BasePositionalPayload(Message, LastName);

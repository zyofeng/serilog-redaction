namespace Serilog.Redaction.Tests;

public record TestPayload
{
    public string Payload { get; init; } = default!;

    [HashedData] public string? LastName { get; init; }
    [ErasedData] public string? TaxNumber { get; init; }

    [HashedData] public Nested? Child { get; init; }
    public TestPayload? Recursive { get; init; }

    public record Nested
    {
        public string? Message { get; init; }

        [HashedData] public string? FirstName { get; init; }
    }
}
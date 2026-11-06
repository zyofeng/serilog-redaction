using Serilog.Redaction.Tests.Infrastructure;

namespace Serilog.Redaction.Tests.Models;

public sealed record TestPayload
{
    public string? Payload { get; init; }

    [HashedData] public string? LastName { get; init; }
    [ErasedData] public string? TaxNumber { get; init; }

    [HashedData] public Nested? Child { get; init; }
    public TestPayload? Recursive { get; init; }

    public sealed record Nested
    {
        public string? Message { get; init; }

        [HashedData] public string? FirstName { get; init; }
    }
}

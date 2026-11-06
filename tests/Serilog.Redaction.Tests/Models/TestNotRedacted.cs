namespace Serilog.Redaction.Tests.Models;

public sealed class TestNotRedacted
{
    public IReadOnlyList<string> Children { get; init; } = [];
}

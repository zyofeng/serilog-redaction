using Serilog.Events;

namespace Serilog.Redaction.Tests.Infrastructure;

internal static class LogEventPropertyValueExtensions
{
    public static bool HasProperty(this StructureValue structure, string name) =>
        structure.Properties.Any(p => p.Name == name);

    public static LogEventPropertyValue? Property(this StructureValue structure, string name) =>
        structure.Properties.SingleOrDefault(p => p.Name == name)?.Value;

    public static object? ScalarOf(this StructureValue structure, string name) =>
        (structure.Property(name) as ScalarValue)?.Value;
}

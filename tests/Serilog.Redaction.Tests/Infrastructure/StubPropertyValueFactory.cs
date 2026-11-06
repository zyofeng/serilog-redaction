using Serilog.Core;
using Serilog.Events;

namespace Serilog.Redaction.Tests.Infrastructure;

internal sealed class StubPropertyValueFactory : ILogEventPropertyValueFactory
{
    public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false) =>
        new ScalarValue(value);
}

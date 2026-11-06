using Microsoft.Extensions.Compliance.Redaction;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Redaction.Tests.Infrastructure;

internal static class LogCapture
{
    private const string PropertyName = "Value";

    public static LogEventPropertyValue Destructure(object? value) =>
        Destructure(value, config => config.Destructure.WithRedaction(TestRedactorProvider.Instance));

    public static LogEventPropertyValue Destructure(object? value, IRedactorProvider redactorProvider) =>
        Destructure(value, config => config.Destructure.WithRedaction(redactorProvider));

    public static LogEventPropertyValue DestructureWithoutProvider(object? value) =>
        Destructure(value, config => config.Destructure.WithRedaction());

    private static LogEventPropertyValue Destructure(object? value, Func<LoggerConfiguration, LoggerConfiguration> configure)
    {
        var sink = new CollectingSink();

        using (var logger = configure(new LoggerConfiguration()).WriteTo.Sink(sink).CreateLogger())
        {
            logger.Information($"{{@{PropertyName}}}", value);
        }

        return sink.Events.Single().Properties[PropertyName];
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}

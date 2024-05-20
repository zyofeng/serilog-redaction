using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Redaction.Tests;

internal sealed class DelegatingSink(Action<LogEvent> write) : ILogEventSink
{
    private readonly Action<LogEvent> _write = write ?? throw new ArgumentNullException(nameof(write));

    public void Emit(LogEvent logEvent)
    {
        _write(logEvent);
    }

    public static LogEvent Execute(object obj, string messageTemplate = "{@Expanded}")
    {
        LogEvent evt = null!;

        var cfg = new LoggerConfiguration();
        cfg.Destructure.WithRedaction(new MockRedactorProvider());
        cfg = cfg.WriteTo.Sink(new DelegatingSink(e => evt = e));

        var log = cfg.CreateLogger();
        log.Information(messageTemplate, obj);

        return evt;
    }

    public class MockRedactorProvider : IRedactorProvider
    {
        public Redactor GetRedactor(DataClassificationSet classification)
        {
            return classification switch
            {
                _ when classification.Equals(DataClassificationSet.FromDataClassification(Taxonomy.Personal)) => new
                    HmacRedactor(Options.Create(new HmacRedactorOptions
                    {
                        Key = Convert.ToBase64String(
                            "Some super secret key that's really long for security"u8.ToArray()),
                        KeyId = 123
                    })),
                _ when classification.Equals(DataClassificationSet.FromDataClassification(Taxonomy.Sensitive)) =>
                    new ErasingRedactor(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
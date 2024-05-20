using Microsoft.Extensions.Compliance.Redaction;
using Serilog.Configuration;

namespace Serilog.Redaction;

public static class LoggerConfigurationRedactionExtensions
{
    /// <summary>
    /// Adds a <see cref="RedactionDestructuringPolicy"/>
    /// </summary>
    /// <param name="configuration">The logger configuration to apply configuration to.</param>
    /// <param name="redactorProvider"></param>
    public static LoggerConfiguration WithRedaction(this LoggerDestructuringConfiguration configuration,
        IRedactorProvider? redactorProvider = null)
        => configuration.With(new RedactionDestructuringPolicy(redactorProvider ?? new NullRedactorProvider()));
}
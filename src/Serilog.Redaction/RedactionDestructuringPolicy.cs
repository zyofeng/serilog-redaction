using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Redaction;

public class RedactionDestructuringPolicy(IRedactorProvider redactorProvider) : IDestructuringPolicy
{
    private static readonly
        ConcurrentDictionary<Type, Func<object, ILogEventPropertyValueFactory, LogEventPropertyValue>?>
        DestructureFuncs = new();

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        var func = DestructureFuncs.GetOrAdd(value.GetType(), CreateDestructureFunc);
        result = func?.Invoke(value, propertyValueFactory) ?? default!;
        return func is not null;
    }

    private Func<object, ILogEventPropertyValueFactory, LogEventPropertyValue>? CreateDestructureFunc(Type type)
    {
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .ToList();

        var dataClassifications = properties
            .Select(pi => new
            {
                pi, pi.GetCustomAttribute<DataClassificationAttribute>(true)?.Classification
            })
            .Where(o => o.Classification != null)
            .ToDictionary(o => o.pi, o => o.Classification);

        if (!dataClassifications.Any())
            return null;

        return (obj, factory) =>
        {
            List<LogEventProperty> logEventProperties = [];
            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(obj);

                if (value != null && dataClassifications.TryGetValue(propertyInfo, out var dataClassification))
                {
                    var redactor = redactorProvider.GetRedactor(dataClassification!);

                    if (redactor.Redact(value) is { } redacted && !string.IsNullOrEmpty(redacted))
                        logEventProperties.Add(new LogEventProperty(propertyInfo.Name, new ScalarValue(redacted)));
                }
                else
                {
                    logEventProperties.Add(new LogEventProperty(propertyInfo.Name,
                        factory.CreatePropertyValue(value, true)));
                }
            }

            return new StructureValue(logEventProperties, type.Name);
        };
    }
}
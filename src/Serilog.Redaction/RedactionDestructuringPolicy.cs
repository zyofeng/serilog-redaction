using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Redaction;

public class RedactionDestructuringPolicy(IRedactorProvider redactorProvider) : IDestructuringPolicy
{
    // Only provider-independent reflection metadata is cached statically; redactors are resolved per instance.
    private static readonly ConcurrentDictionary<Type, TypeMetadata?> TypeMetadataCache = new();

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        var metadata = TypeMetadataCache.GetOrAdd(value.GetType(), CreateTypeMetadata);
        if (metadata is null)
        {
            result = null!;
            return false;
        }

        result = Destructure(value, metadata, propertyValueFactory);
        return true;
    }

    private StructureValue Destructure(object obj, TypeMetadata metadata, ILogEventPropertyValueFactory factory)
    {
        List<LogEventProperty> logEventProperties = [];
        foreach (var propertyInfo in metadata.Properties)
        {
            var value = propertyInfo.GetValue(obj);

            if (value != null && metadata.Classifications.TryGetValue(propertyInfo, out var dataClassification))
            {
                var redactor = redactorProvider.GetRedactor(dataClassification);

                if (redactor.Redact(value) is { } redacted && !string.IsNullOrEmpty(redacted))
                    logEventProperties.Add(new LogEventProperty(propertyInfo.Name, new ScalarValue(redacted)));
            }
            else
            {
                logEventProperties.Add(new LogEventProperty(propertyInfo.Name,
                    factory.CreatePropertyValue(value, true)));
            }
        }

        return new StructureValue(logEventProperties, metadata.TypeName);
    }

    private static TypeMetadata? CreateTypeMetadata(Type type)
    {
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var classifications = new Dictionary<PropertyInfo, DataClassification>();
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<DataClassificationAttribute>(true) is { } attribute)
                classifications[property] = attribute.Classification;
        }

        return classifications.Count == 0 ? null : new TypeMetadata(type.Name, properties, classifications);
    }

    private sealed class TypeMetadata(
        string typeName,
        IReadOnlyList<PropertyInfo> properties,
        IReadOnlyDictionary<PropertyInfo, DataClassification> classifications)
    {
        public string TypeName { get; } = typeName;
        public IReadOnlyList<PropertyInfo> Properties { get; } = properties;
        public IReadOnlyDictionary<PropertyInfo, DataClassification> Classifications { get; } = classifications;
    }
}

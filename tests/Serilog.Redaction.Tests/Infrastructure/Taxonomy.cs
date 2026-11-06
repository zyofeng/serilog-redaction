using Microsoft.Extensions.Compliance.Classification;

namespace Serilog.Redaction.Tests.Infrastructure;

public static class Taxonomy
{
    private const string TaxonomyName = "Serilog.Redaction.Tests";

    public static DataClassification Sensitive { get; } = new(TaxonomyName, nameof(Sensitive));
    public static DataClassification Personal { get; } = new(TaxonomyName, nameof(Personal));
}

public sealed class HashedDataAttribute() : DataClassificationAttribute(Taxonomy.Personal);

public sealed class ErasedDataAttribute() : DataClassificationAttribute(Taxonomy.Sensitive);

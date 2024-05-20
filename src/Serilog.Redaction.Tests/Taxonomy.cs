using Microsoft.Extensions.Compliance.Classification;

namespace Serilog.Redaction.Tests;

public static class Taxonomy
{
    public static string TaxonomyName => typeof(Taxonomy).FullName!;

    public static DataClassification Sensitive => new(TaxonomyName, nameof(Sensitive));
    public static DataClassification Personal => new(TaxonomyName, nameof(Personal));
}
public class HashedDataAttribute() : DataClassificationAttribute(Taxonomy.Personal);
public class ErasedDataAttribute() : DataClassificationAttribute(Taxonomy.Sensitive);

using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;

namespace Serilog.Redaction.Tests.Infrastructure;

internal sealed class TestRedactorProvider : IRedactorProvider
{
    public static TestRedactorProvider Instance { get; } = new();

    private static readonly DataClassificationSet PersonalSet = new(Taxonomy.Personal);
    private static readonly DataClassificationSet SensitiveSet = new(Taxonomy.Sensitive);

    private readonly Redactor _hmacRedactor = new HmacRedactor(Options.Create(new HmacRedactorOptions
    {
        Key = Convert.ToBase64String("Some super secret key that's really long for security"u8.ToArray()),
        KeyId = 123
    }));

    public Redactor GetRedactor(DataClassificationSet classifications)
    {
        if (classifications.Equals(PersonalSet))
            return _hmacRedactor;

        if (classifications.Equals(SensitiveSet))
            return ErasingRedactor.Instance;

        throw new ArgumentOutOfRangeException(nameof(classifications), classifications, "Unknown classification.");
    }
}

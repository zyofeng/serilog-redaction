using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Serilog.Events;
using Serilog.Redaction.Tests.Infrastructure;
using Serilog.Redaction.Tests.Models;

namespace Serilog.Redaction.Tests;

public class LoggerConfigurationRedactionExtensionsTests
{
    [Test]
    public async Task WithRedaction_WithoutProvider_UsesNullRedactor()
    {
        var payload = new NullProviderPayload("Mike", "1234567");

        var structure = await Assert.That(LogCapture.DestructureWithoutProvider(payload)).IsTypeOf<StructureValue>().And.IsNotNull();

        using (Assert.Multiple())
        {
            await Assert.That(structure.ScalarOf(nameof(NullProviderPayload.LastName))).IsEqualTo(payload.LastName);
            await Assert.That(structure.ScalarOf(nameof(NullProviderPayload.TaxNumber))).IsEqualTo(payload.TaxNumber);
        }
    }

    [Test]
    public async Task WithRedaction_DifferentProvidersForSameType_UseTheirOwnRedactor()
    {
        var payload = new CacheIsolationPayload("Mike");

        _ = LogCapture.Destructure(payload, new FixedRedactorProvider("first"));
        var structure = await Assert.That(LogCapture.Destructure(payload, new FixedRedactorProvider("second")))
            .IsTypeOf<StructureValue>().And.IsNotNull();

        await Assert.That(structure.ScalarOf(nameof(CacheIsolationPayload.LastName))).IsEqualTo("second");
    }

    private sealed class FixedRedactorProvider(string replacement) : IRedactorProvider
    {
        public Redactor GetRedactor(DataClassificationSet classifications) => new FixedRedactor(replacement);
    }

    private sealed class FixedRedactor(string replacement) : Redactor
    {
        public override int GetRedactedLength(ReadOnlySpan<char> input) => replacement.Length;

        public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
        {
            replacement.AsSpan().CopyTo(destination);
            return replacement.Length;
        }
    }
}

using Serilog.Events;
using Serilog.Redaction.Tests.Infrastructure;
using Serilog.Redaction.Tests.Models;

namespace Serilog.Redaction.Tests;

public class RedactionDestructuringPolicyTests
{
    private const string Payload = "Test Payload";
    private const string LastName = "Mike";
    private const string NestedPayload = "another payload";
    private const string NestedLastName = "Mike2";

    private static TestPayload CreatePayload() => new()
    {
        Payload = Payload,
        TaxNumber = "1234567",
        LastName = LastName,
        Child = new TestPayload.Nested { FirstName = "Test FirstName", Message = "Test Message" },
        Recursive = new TestPayload
        {
            Payload = NestedPayload,
            LastName = NestedLastName,
            TaxNumber = "7654321",
            Child = new TestPayload.Nested { FirstName = "Another FirstName" }
        }
    };

    [Test]
    public async Task TryDestructure_TypeWithClassifiedProperties_ReturnsTrue()
    {
        var policy = new RedactionDestructuringPolicy(TestRedactorProvider.Instance);

        var handled = policy.TryDestructure(CreatePayload(), new StubPropertyValueFactory(), out var result);

        await Assert.That(handled).IsTrue();
        await Assert.That(result).IsTypeOf<StructureValue>().And.IsNotNull();
    }

    [Test]
    public async Task TryDestructure_TypeWithoutClassifiedProperties_ReturnsFalse()
    {
        var policy = new RedactionDestructuringPolicy(TestRedactorProvider.Instance);

        var handled = policy.TryDestructure(new TestNotRedacted(), new StubPropertyValueFactory(), out _);

        await Assert.That(handled).IsFalse();
    }

    [Test]
    public async Task Destructure_UsesTypeNameAsTypeTag()
    {
        var structure = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();

        await Assert.That(structure.TypeTag).IsEqualTo(nameof(TestPayload));
    }

    [Test]
    public async Task Destructure_UnclassifiedProperty_IsPreserved()
    {
        var structure = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();

        await Assert.That(structure.ScalarOf(nameof(TestPayload.Payload))).IsEqualTo(Payload);
    }

    [Test]
    public async Task Destructure_HashedProperty_IsReplacedWithRedactedString()
    {
        var structure = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();
        var lastName = structure.ScalarOf(nameof(TestPayload.LastName));

        using (Assert.Multiple())
        {
            await Assert.That(lastName).IsTypeOf<string>();
            await Assert.That(lastName as string).IsNotNullOrEmpty();
            await Assert.That(lastName).IsNotEqualTo(LastName);
        }
    }

    [Test]
    public async Task Destructure_HashedProperty_IsDeterministic()
    {
        var first = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();
        var second = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();

        await Assert.That(second.ScalarOf(nameof(TestPayload.LastName)))
            .IsEqualTo(first.ScalarOf(nameof(TestPayload.LastName)));
    }

    [Test]
    public async Task Destructure_DifferentHashedValues_ProduceDifferentRedactions()
    {
        var first = await Assert.That(LogCapture.Destructure(CreatePayload() with { LastName = "Alice" }))
            .IsTypeOf<StructureValue>().And.IsNotNull();
        var second = await Assert.That(LogCapture.Destructure(CreatePayload() with { LastName = "Bob" }))
            .IsTypeOf<StructureValue>().And.IsNotNull();

        await Assert.That(second.ScalarOf(nameof(TestPayload.LastName)))
            .IsNotEqualTo(first.ScalarOf(nameof(TestPayload.LastName)));
    }

    [Test]
    public async Task Destructure_ErasedProperty_IsOmitted()
    {
        var structure = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();

        await Assert.That(structure.HasProperty(nameof(TestPayload.TaxNumber))).IsFalse();
    }

    [Test]
    public async Task Destructure_NullClassifiedProperty_IsLoggedAsNull()
    {
        var payload = CreatePayload() with { LastName = null };

        var structure = await Assert.That(LogCapture.Destructure(payload)).IsTypeOf<StructureValue>().And.IsNotNull();

        using (Assert.Multiple())
        {
            await Assert.That(structure.HasProperty(nameof(TestPayload.LastName))).IsTrue();
            await Assert.That(structure.ScalarOf(nameof(TestPayload.LastName))).IsNull();
        }
    }

    [Test]
    public async Task Destructure_ClassifiedComplexProperty_IsRedactedToScalarString()
    {
        var structure = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();

        var child = await Assert.That(structure.Property(nameof(TestPayload.Child))).IsTypeOf<ScalarValue>().And.IsNotNull();

        await Assert.That(child.Value as string).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Destructure_UnclassifiedNestedObject_IsRedactedRecursively()
    {
        var structure = await Assert.That(LogCapture.Destructure(CreatePayload())).IsTypeOf<StructureValue>().And.IsNotNull();
        var recursive = await Assert.That(structure.Property(nameof(TestPayload.Recursive)))
            .IsTypeOf<StructureValue>().And.IsNotNull();

        using (Assert.Multiple())
        {
            await Assert.That(recursive.ScalarOf(nameof(TestPayload.Payload))).IsEqualTo(NestedPayload);
            await Assert.That(recursive.ScalarOf(nameof(TestPayload.LastName))).IsNotEqualTo(NestedLastName);
            await Assert.That(recursive.HasProperty(nameof(TestPayload.TaxNumber))).IsFalse();
        }
    }
}

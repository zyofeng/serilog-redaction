using Serilog.Events;
using Serilog.Redaction.Tests.Infrastructure;
using Serilog.Redaction.Tests.Models;

namespace Serilog.Redaction.Tests;

public class CollectionRedactionTests
{
    private static readonly TestArray Instance = new(
        [new TestArray.Child("Some_Masked_String", 12345), new TestArray.Child("Some_Masked_String_2", 234567)],
        [new TestArray.Child("Some_Masked_String_3", 987654), new TestArray.Child("Some_Masked_String_4", 898766)]);

    [Test]
    public async Task ClassifiedCollection_IsRedactedToScalarString()
    {
        var structure = await Assert.That(LogCapture.Destructure(Instance)).IsTypeOf<StructureValue>().And.IsNotNull();

        var masked = await Assert.That(structure.Property(nameof(TestArray.MaskedChildren))).IsTypeOf<ScalarValue>().And.IsNotNull();

        await Assert.That(masked.Value as string).IsNotNullOrEmpty();
    }

    [Test]
    public async Task UnclassifiedCollection_ElementsAreRedactedIndividually()
    {
        var structure = await Assert.That(LogCapture.Destructure(Instance)).IsTypeOf<StructureValue>().And.IsNotNull();
        var sequence = await Assert.That(structure.Property(nameof(TestArray.NotMaskedChildren)))
            .IsTypeOf<SequenceValue>().And.IsNotNull();

        await Assert.That(sequence.Elements).Count().IsEqualTo(Instance.NotMaskedChildren.Count);

        for (var i = 0; i < sequence.Elements.Count; i++)
        {
            var expected = Instance.NotMaskedChildren[i];
            var element = await Assert.That(sequence.Elements[i]).IsTypeOf<StructureValue>().And.IsNotNull();

            using (Assert.Multiple())
            {
                await Assert.That(element.ScalarOf(nameof(TestArray.Child.NotMasked))).IsEqualTo(expected.NotMasked);
                await Assert.That(element.ScalarOf(nameof(TestArray.Child.Number))).IsTypeOf<string>();
            }
        }
    }

    [Test]
    [Arguments(0)]
    [Arguments(-1)]
    [Arguments(int.MaxValue)]
    public async Task ClassifiedValueTypeProperty_IsRedactedToString(int number)
    {
        var child = new TestArray.Child("value", number);

        var structure = await Assert.That(LogCapture.Destructure(child)).IsTypeOf<StructureValue>().And.IsNotNull();
        var redacted = structure.ScalarOf(nameof(TestArray.Child.Number));

        using (Assert.Multiple())
        {
            await Assert.That(redacted as string).IsNotNullOrEmpty();
            await Assert.That(redacted as string).IsNotEqualTo(number.ToString());
        }
    }

    [Test]
    public async Task CollectionOfUnclassifiedTypes_IsLoggedUnchanged()
    {
        IReadOnlyList<TestNotRedacted> instance =
        [
            new() { Children = ["test", "test1", "test2"] },
            new() { Children = ["a", "b"] }
        ];

        var sequence = await Assert.That(LogCapture.Destructure(instance)).IsTypeOf<SequenceValue>().And.IsNotNull();

        await Assert.That(sequence.Elements).Count().IsEqualTo(instance.Count);

        for (var i = 0; i < sequence.Elements.Count; i++)
        {
            var element = await Assert.That(sequence.Elements[i]).IsTypeOf<StructureValue>().And.IsNotNull();
            var children = await Assert.That(element.Property(nameof(TestNotRedacted.Children)))
                .IsTypeOf<SequenceValue>().And.IsNotNull();

            var values = children.Elements.OfType<ScalarValue>().Select(s => s.Value).ToList();

            await Assert.That(values).IsEquivalentTo(instance[i].Children);
        }
    }
}

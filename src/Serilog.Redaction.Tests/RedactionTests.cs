using FluentAssertions;
using Serilog.Events;

namespace Serilog.Redaction.Tests;

public class RedactionTests
{
    [Fact]
    public void Redactor_Tests()
    {
        TestPayload instance = new()
        {
            Payload = "Test Payload",
            TaxNumber = "1234567",
            LastName = "Mike",
            Child = new TestPayload.Nested
            {
                FirstName = "Test FirstName",
                Message = "Test Message"
            },
            Recursive = new TestPayload
            {
                Payload = "another payload",
                LastName = "Mike2",
                Child = new TestPayload.Nested
                {
                    FirstName = "Another FirstName"
                }
            }
        };
        var evt = DelegatingSink.Execute(instance);
        var expanded = (StructureValue)evt.Properties["Expanded"];

        var payload = expanded.Properties.SingleOrDefault(p => p.Name == "Payload");
        Assert.NotNull(payload);
        Assert.Equal(instance.Payload, ((ScalarValue)payload.Value).Value);

        Assert.Null(expanded.Properties.SingleOrDefault(p => p.Name == "TaxNumber"));

        var lastName = expanded.Properties.SingleOrDefault(p => p.Name == "LastName");
        Assert.NotNull(lastName);
        Assert.NotEqual(instance.LastName, ((ScalarValue)lastName.Value).Value);

        var child = expanded.Properties.SingleOrDefault(p => p.Name == "Child");
        Assert.NotNull(child);
        Assert.NotEmpty(((ScalarValue)child.Value).Value!.ToString()!);
    }

    [Fact]
    public void Array_Tests()
    {
        TestArray instance = new(
            [
                new TestArray.Child("Some_Masked_String", 12345),
                new TestArray.Child("Some_Masked_String_2", 234567)],
            [
                new TestArray.Child("Some_Masked_String_3", 987654),
                new TestArray.Child("Some_Masked_String_4", 898766)
            ]);

        var evt = DelegatingSink.Execute(instance);
        var expanded = (StructureValue)evt.Properties["Expanded"];

        var masked = expanded.Properties.SingleOrDefault(p => p.Name == "MaskedChildren");
        Assert.NotNull(masked);
        Assert.True(((ScalarValue)masked.Value).Value is string);
        
        var notMasked = expanded.Properties.SingleOrDefault(p => p.Name == "NotMaskedChildren");
        Assert.NotNull(notMasked?.Value);

        Assert.Equal(instance.NotMaskedChildren.Count, ((SequenceValue)notMasked.Value).Elements.Count);

        for (var i = 0; i < ((SequenceValue)notMasked.Value).Elements.Count; i++)
        {
            var child = ((SequenceValue)notMasked.Value).Elements[i];
            switch (child)
            {
                case StructureValue sv:
                    var childNotMasked = sv.Properties.SingleOrDefault(p => p.Name == "NotMasked");
                    var childNumber = sv.Properties.SingleOrDefault(p => p.Name == "Number");
                    Assert.NotNull(childNotMasked);
                    Assert.NotNull(childNumber);
                    Assert.Equal(instance.NotMaskedChildren[i].NotMasked, ((ScalarValue)childNotMasked.Value).Value);
                    Assert.NotEqual(instance.NotMaskedChildren[i].Number, ((ScalarValue)childNumber.Value).Value);
                    break;
                default:
                    Assert.Fail("Unknown type");
                    break;
            }
        }
    }

    [Fact]
    public void Array_NotRedacted()
    {
        IReadOnlyList<TestNotRedacted> instance =
        [
            new()
            {
                Children =
                    ["test", "test1", "test2"]
            },
            new() { Children = ["test", "test1", "test2"] }
        ];
        
        var evt = DelegatingSink.Execute(instance);
        var expanded = (SequenceValue)evt.Properties["Expanded"];
        
        for (var i = 0; i < expanded.Elements.Count; i++)
        {
            var child = expanded.Elements[i] as StructureValue;
            Assert.NotNull(child);
            
            var children = child.Properties.SingleOrDefault(p => p.Name == "Children")?.Value as SequenceValue;

            Assert.NotNull(children);

            children.Elements
                .Select(c => c is ScalarValue lep ? lep.Value : throw new ArgumentOutOfRangeException())
                .Should()
                .BeEquivalentTo(instance[i].Children);
        }
    }
}
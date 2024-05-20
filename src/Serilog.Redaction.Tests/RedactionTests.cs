using Serilog.Events;

namespace Serilog.Redaction.Tests;

public class RedactionTests
{
    [Fact]
    public void Redactor_Tests()
    {
        var evt = DelegatingSink.Execute(TestPayload.Instance);

        var expanded = (StructureValue)evt.Properties["Expanded"];

        var payload = expanded.Properties.SingleOrDefault(p => p.Name == "Payload");
        Assert.NotNull(payload);
        Assert.Equal(TestPayload.Instance.Payload, ((ScalarValue)payload.Value).Value);
        
        Assert.Null(expanded.Properties.SingleOrDefault(p => p.Name == "TaxNumber"));
        
        var lastName = expanded.Properties.SingleOrDefault(p => p.Name == "LastName");
        Assert.NotNull(lastName);
        Assert.NotEqual(TestPayload.Instance.LastName, ((ScalarValue)lastName.Value).Value);
        
        var child = expanded.Properties.SingleOrDefault(p => p.Name == "Child");
        Assert.NotNull(child);
        Assert.NotEmpty(((ScalarValue)child.Value).Value!.ToString()!);
    }
}

public record TestPayload
{
    public static readonly TestPayload Instance = new()
    {
        Payload = "Test Payload",
        TaxNumber = "1234567",
        LastName = "Mike",
        Child = new Nested
        {
            FirstName = "Test FirstName",
            Message = "Test Message"
        },
        Recursive = new TestPayload
        {
            Payload = "another payload",
            LastName = "Mike2",
            Child = new Nested
            {
                FirstName = "Another FirstName"
            }
        }
    };

    public string Payload { get; init; } = default!;

    [HashedData] public string? LastName { get; init; }
    [ErasedData] public string? TaxNumber { get; init; }

    [HashedData] public Nested? Child { get; init; }
    public TestPayload? Recursive { get; init; }

    public record Nested
    {
        public string? Message { get; init; }

        [HashedData] public string? FirstName { get; init; }
    }
}
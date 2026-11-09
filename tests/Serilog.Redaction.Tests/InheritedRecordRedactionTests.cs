using Serilog.Events;
using Serilog.Redaction.Tests.Infrastructure;
using Serilog.Redaction.Tests.Models;

namespace Serilog.Redaction.Tests;

public class InheritedRecordRedactionTests
{
    private const string BaseMessage = "base message";
    private const string BaseLastName = "BaseLast";
    private const string DerivedMessage = "derived message";
    private const string DerivedFirstName = "DerivedFirst";
    private const string Email = "someone@example.com";

    [Test]
    public async Task Destructure_DerivedWithoutClassifications_RedactsBaseProperties()
    {
        var payload = new DerivedWithoutClassifications
        {
            BaseMessage = BaseMessage,
            BaseLastName = BaseLastName,
            BaseTaxNumber = "1234567",
            Email = Email,
            DerivedMessage = DerivedMessage
        };

        var structure = await Assert.That(LogCapture.Destructure(payload)).IsTypeOf<StructureValue>().And.IsNotNull();

        using (Assert.Multiple())
        {
            await Assert.That(structure.TypeTag).IsEqualTo(nameof(DerivedWithoutClassifications));
            await Assert.That(structure.ScalarOf(nameof(BasePayload.BaseMessage))).IsEqualTo(BaseMessage);
            await Assert.That(structure.ScalarOf(nameof(DerivedWithoutClassifications.DerivedMessage))).IsEqualTo(DerivedMessage);
            await Assert.That(structure.ScalarOf(nameof(BasePayload.BaseLastName)) as string).IsNotNullOrEmpty();
            await Assert.That(structure.ScalarOf(nameof(BasePayload.BaseLastName))).IsNotEqualTo(BaseLastName);
            await Assert.That(structure.ScalarOf(nameof(BasePayload.Email))).IsNotEqualTo(Email);
            await Assert.That(structure.HasProperty(nameof(BasePayload.BaseTaxNumber))).IsFalse();
        }
    }

    [Test]
    public async Task Destructure_DerivedWithClassifications_RedactsBaseAndDerivedProperties()
    {
        var payload = new DerivedWithClassifications
        {
            BaseMessage = BaseMessage,
            BaseLastName = BaseLastName,
            BaseTaxNumber = "1234567",
            DerivedMessage = DerivedMessage,
            DerivedFirstName = DerivedFirstName
        };

        var structure = await Assert.That(LogCapture.Destructure(payload)).IsTypeOf<StructureValue>().And.IsNotNull();

        using (Assert.Multiple())
        {
            await Assert.That(structure.ScalarOf(nameof(BasePayload.BaseMessage))).IsEqualTo(BaseMessage);
            await Assert.That(structure.ScalarOf(nameof(DerivedWithClassifications.DerivedMessage))).IsEqualTo(DerivedMessage);
            await Assert.That(structure.ScalarOf(nameof(BasePayload.BaseLastName)) as string).IsNotNullOrEmpty();
            await Assert.That(structure.ScalarOf(nameof(BasePayload.BaseLastName))).IsNotEqualTo(BaseLastName);
            await Assert.That(structure.ScalarOf(nameof(DerivedWithClassifications.DerivedFirstName)) as string).IsNotNullOrEmpty();
            await Assert.That(structure.ScalarOf(nameof(DerivedWithClassifications.DerivedFirstName))).IsNotEqualTo(DerivedFirstName);
            await Assert.That(structure.HasProperty(nameof(BasePayload.BaseTaxNumber))).IsFalse();
        }
    }

    [Test]
    public async Task Destructure_OverriddenClassifiedProperty_InheritsClassification()
    {
        var payload = new DerivedWithOverride { Email = Email };

        var structure = await Assert.That(LogCapture.Destructure(payload)).IsTypeOf<StructureValue>().And.IsNotNull();
        var emails = structure.Properties.Where(p => p.Name == nameof(BasePayload.Email)).ToList();

        using (Assert.Multiple())
        {
            await Assert.That(emails).Count().IsEqualTo(1);
            await Assert.That((emails[0].Value as ScalarValue)?.Value as string).IsNotNullOrEmpty();
            await Assert.That((emails[0].Value as ScalarValue)?.Value).IsNotEqualTo(Email);
        }
    }

    [Test]
    public async Task Destructure_HiddenProperty_UsesDerivedDeclarationOnce()
    {
        var payload = new DerivedWithHiddenProperty { BaseMessage = BaseMessage };

        var structure = await Assert.That(LogCapture.Destructure(payload)).IsTypeOf<StructureValue>().And.IsNotNull();
        var messages = structure.Properties.Where(p => p.Name == nameof(BasePayload.BaseMessage)).ToList();

        using (Assert.Multiple())
        {
            await Assert.That(messages).Count().IsEqualTo(1);
            await Assert.That((messages[0].Value as ScalarValue)?.Value as string).IsNotNullOrEmpty();
            await Assert.That((messages[0].Value as ScalarValue)?.Value).IsNotEqualTo(BaseMessage);
        }
    }

    [Test]
    public async Task Destructure_DerivedPositionalRecord_RedactsBaseAndDerivedProperties()
    {
        var payload = new DerivedPositionalPayload(BaseMessage, BaseLastName, "1234567");

        var structure = await Assert.That(LogCapture.Destructure(payload)).IsTypeOf<StructureValue>().And.IsNotNull();

        using (Assert.Multiple())
        {
            await Assert.That(structure.ScalarOf(nameof(BasePositionalPayload.Message))).IsEqualTo(BaseMessage);
            await Assert.That(structure.ScalarOf(nameof(BasePositionalPayload.LastName))).IsNotEqualTo(BaseLastName);
            await Assert.That(structure.HasProperty(nameof(DerivedPositionalPayload.TaxNumber))).IsFalse();
        }
    }

    [Test]
    public async Task Destructure_DerivedInstanceAsBaseProperty_RedactsDerivedProperties()
    {
        var payload = TestPayloadWrapper(new DerivedWithClassifications
        {
            BaseLastName = BaseLastName,
            DerivedFirstName = DerivedFirstName
        });

        var structure = await Assert.That(LogCapture.Destructure(payload)).IsTypeOf<StructureValue>().And.IsNotNull();
        var inner = await Assert.That(structure.Property("Inner")).IsTypeOf<StructureValue>().And.IsNotNull();

        using (Assert.Multiple())
        {
            await Assert.That(inner.TypeTag).IsEqualTo(nameof(DerivedWithClassifications));
            await Assert.That(inner.ScalarOf(nameof(BasePayload.BaseLastName))).IsNotEqualTo(BaseLastName);
            await Assert.That(inner.ScalarOf(nameof(DerivedWithClassifications.DerivedFirstName))).IsNotEqualTo(DerivedFirstName);
        }
    }

    private static object TestPayloadWrapper(BasePayload inner) => new { Inner = inner };
}

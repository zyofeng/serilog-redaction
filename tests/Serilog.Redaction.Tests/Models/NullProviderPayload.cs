using Serilog.Redaction.Tests.Infrastructure;

namespace Serilog.Redaction.Tests.Models;

public sealed record NullProviderPayload(
    [property: HashedData] string LastName,
    [property: ErasedData] string TaxNumber);

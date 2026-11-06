using Serilog.Redaction.Tests.Infrastructure;

namespace Serilog.Redaction.Tests.Models;

public sealed record CacheIsolationPayload([property: HashedData] string LastName);

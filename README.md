# Serilog.Redaction 
[Serilog](https://serilog.net) Redaction library for [Microsoft.Extensions.Compliance.Redaction](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.compliance.redaction)

# Getting Started
Install Serilog.Redaction as well as Microsoft.Extensions.Compliance.Redaction 
```powershell
Install-Package Serilog.Redaction
Install-Package Microsoft.Extensions.Compliance.Redaction 
```

# Usage
Create your Taxonomy:
```
public static class Taxonomy
{
    public static string TaxonomyName => typeof(Taxonomy).FullName!;
    
        public static DataClassification Sensitive => new(TaxonomyName, nameof(Sensitive));
        public static DataClassification Personal => new(TaxonomyName, nameof(Personal));
    }
    public class HashedDataAttribute() : DataClassificationAttribute(Taxonomy.Personal);
    public class ErasedDataAttribute() : DataClassificationAttribute(Taxonomy.Sensitive);
}
```
Decorate classes/properties with DataClassificationAttributes:
```
public record TestPayload
{

    [HashedData] public string? LastName { get; init; }
    [ErasedData] public string? TaxNumber { get; init; }
}
```
Add Redaction services:
```
var builder = WebApplication.CreateBuilder(args);
...
builder.Services.AddRedaction(x =>
{
    x.SetRedactor<ErasingRedactor>(new DataClassificationSet(Taxonomy.Personal));
    x.SetHmacRedactor(hmacOpts =>
    {
        hmacOpts.Key = Convert.ToBase64String("Some super secret key that's really long for security"u8.ToArray());
        hmacOpts.KeyId = 123;
    }, new DataClassificationSet(Taxonomy.Sensitive));
});
...
```
Configure LoggerConfiguration:
```
using Serilog.Redaction;
...
builder.Services.AddSerilog((sp, loggerConfiguration) =>
{
    loggerConfiguration.Destructure.WithRedaction(sp.GetRequiredService<IRedactorProvider>());
});
...
```
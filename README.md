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
    
    public static DataClassification Confidential => new(TaxonomyName, nameof(Confidential));
    public static DataClassification Restricted => new(TaxonomyName, nameof(Restricted));
    public static DataClassification Internal => new(TaxonomyName, nameof(Internal));
    public static DataClassification Public => new(TaxonomyName, nameof(Public));
    
    public class ConfidentialDataAttribute() : DataClassificationAttribute(Taxonomy.Confidential);
    public class RestrictedDataAttribute() : DataClassificationAttribute(Taxonomy.Restricted);
}
```
Decorate classes/properties with DataClassificationAttributes:
```
public record Client
{
    [RestrictedData] public string? LastName { get; init; }
    [ConfidentialData] public string? TaxNumber { get; init; }
}
```
Add Redaction services:
```
var builder = WebApplication.CreateBuilder(args);
...
builder.Services.AddRedaction(x =>
{
    x.SetRedactor<ErasingRedactor>(DataClassificationSet.FromDataClassification(Taxonomy.Confidential));
    x.SetHmacRedactor(hmacOpts =>
    {
        hmacOpts.Key = Convert.ToBase64String("Some super secret key that is really long for security"u8.ToArray());
        hmacOpts.KeyId = 123;
    }, DataClassificationSet.FromDataClassification(Taxonomy.Restricted));
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
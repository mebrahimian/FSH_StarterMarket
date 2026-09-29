using FSH.Framework.Core.Domain;

namespace Modules.MarketIntelligence.Domain;

public sealed class BenchmarkAsset :
    BaseEntity<int>,
    IGlobalEntity
{
    private BenchmarkAsset()
    {
    }

    public BenchmarkAsset(
        string code,
        string name,
        string source,
        string externalCode,
        string currency,
        string unit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalCode);

        Code = code.Trim();
        Name = name.Trim();
        Source = source.Trim();
        ExternalCode = externalCode.Trim();
        Currency = currency.Trim();
        Unit = unit.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string ExternalCode { get; private set; } = string.Empty;
    public string Currency { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }
    

    
}
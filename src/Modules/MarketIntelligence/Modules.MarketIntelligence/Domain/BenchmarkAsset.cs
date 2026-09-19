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
        string currency,
        string unit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);

        Code = code.Trim();
        Name = name.Trim();
        Currency = currency.Trim();
        Unit = unit.Trim();
        IsActive = true;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Currency { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }
}
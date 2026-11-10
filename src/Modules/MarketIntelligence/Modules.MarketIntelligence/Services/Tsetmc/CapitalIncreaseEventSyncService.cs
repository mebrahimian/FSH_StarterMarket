
using Microsoft.EntityFrameworkCore;
using FSH.Modules.MarketIntelligence.Data;
using Modules.MarketIntelligence.Domain;

namespace FSH.Modules.MarketIntelligence.Services.Tsetmc;

public sealed class CapitalIncreaseEventSyncService
{
    private readonly MarketIntelligenceDbContext _dbContext;

    public CapitalIncreaseEventSyncService(
        MarketIntelligenceDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<(int Inserted, int Updated)> SyncInstrumentAsync(
        int instrumentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(instrumentId);
        var changes = await _dbContext.InstrumentShareChanges
            .AsNoTracking()
            .Where(x => x.InstrumentId == instrumentId &&
                        x.OldShares > 1 &&
                        x.NewShares > x.OldShares)
            .GroupBy(x => new
            {
                x.InstrumentId,
                x.OldShares,
                x.NewShares
            })
            .Select(g => new
            {
                g.Key.InstrumentId,
                g.Key.OldShares,
                g.Key.NewShares,
                EffectiveDate = g.Max(x => x.EffectiveDate)
            })
            .ToListAsync(cancellationToken);

        var existing = await _dbContext.CapitalIncreaseEvents
            .Where(x => x.InstrumentId == instrumentId)
            .ToDictionaryAsync(x => (x.InstrumentId, x.OldShares, x.NewShares),
                                     cancellationToken);

        int inserted = 0;
        int updated = 0;

        foreach (var change in changes)
        {
            var key = (
                change.InstrumentId,
                change.OldShares,
                change.NewShares);

            if (existing.TryGetValue(key, out var capitalEvent))
            {
                if (change.EffectiveDate >
                    capitalEvent.AdjustmentEffectiveDate)
                {
                    capitalEvent.UpdateAdjustmentEffectiveDate(
                        change.EffectiveDate);

                    updated++;
                }

                continue;
            }

            var newEvent = new CapitalIncreaseEvent(
                change.InstrumentId,
                change.OldShares,
                change.NewShares,
                change.EffectiveDate);

            _dbContext.CapitalIncreaseEvents.Add(newEvent);

            existing.Add(key, newEvent);
            inserted++;
        }

        if (inserted > 0 || updated > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return (inserted, updated);
    }
}

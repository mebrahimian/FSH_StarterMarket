using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.CodalIncrementalSchedule;

public sealed record GetCodalIncrementalScheduleQuery
    : IQuery<CodalIncrementalScheduleDto>;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.MarketIntelligence.Contracts.v1.DataQualityIssues;

public sealed record GetDataQualityIssuesQuery : IQuery<IReadOnlyList<DataQualityIssueDto>>;
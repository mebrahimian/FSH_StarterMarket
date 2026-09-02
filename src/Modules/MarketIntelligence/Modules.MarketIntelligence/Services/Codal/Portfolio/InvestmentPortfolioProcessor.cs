using FSH.Framework.Shared.Utilities;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

public sealed class InvestmentPortfolioProcessor(
    InvestmentPortfolioReader reader,
    MarketIntelligenceDbContext dbContext,
    ILogger<InvestmentPortfolioProcessor> logger)
    : ICodalDisclosureProcessor
{
    public bool CanProcess(Disclosure disclosure)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        return disclosure.Rt == 2 &&
               disclosure.Let is 58 or 6;
    }

    public async Task ProcessAsync(
        Disclosure disclosure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disclosure);

        if (!CanProcess(disclosure) ||
            string.IsNullOrWhiteSpace(disclosure.Url))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(disclosure.Symbol))
        {
            throw new InvalidOperationException(
                $"Disclosure {disclosure.TracingNo} has no symbol.");
        }
        var aliasLookup = await dbContext.PortfolioCompanyAliases
    .Where(x => x.IsActive)
    .GroupBy(x => new
    {
        x.FSortName,
        x.IsListed
    })
    .Select(x => new
    {
        x.Key.FSortName,
        x.Key.IsListed,
        CompanyIds = x
            .Select(y => y.CompanyId)
            .Distinct()
            .ToList()
    })
    .ToDictionaryAsync(
        x => (x.FSortName, x.IsListed),
        x => x.CompanyIds,
        cancellationToken);

        string parentFSortSymbol = FSort.Normalize(disclosure.Symbol);

        int? parentCompanyId =
            await dbContext.CompanyMaster
                .Where(x =>
                    x.IsListed &&
                    x.FSortSymbol == parentFSortSymbol)
                .Select(x => (int?)x.CompanyId)
                .FirstOrDefaultAsync(cancellationToken);

        if (parentCompanyId is null)
        {
            return;
        }

        IReadOnlyList<InvestmentPortfolioSheetData> sheets =
            await reader.ReadAsync(
                disclosure.Url,
                cancellationToken);

        string? periodEndDate =
            sheets
                .Select(x => x.PeriodEndDate)
                .FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x));

        if (string.IsNullOrWhiteSpace(periodEndDate))
        {
            throw new InvalidOperationException(
                $"Portfolio period was not found. " +
                $"TracingNo: {disclosure.TracingNo}");
        }
        PortfolioSourceType sourceType = 
            disclosure.Let == 6
                ? PortfolioSourceType.FinancialStatement
                : PortfolioSourceType.MonthlyActivity;

        PortfolioAuditStatus auditStatus = PortfolioAuditStatus.None;

        if (sheets.Any(x => x.Metadata.MetaTableId is 1529 or 1530))
        {
            auditStatus = PortfolioAuditStatus.Audited;
        }
        else if (sheets.Any(x => x.Metadata.MetaTableId is 1507 or 1508))
        {
            auditStatus = PortfolioAuditStatus.Unaudited;
        }
        else if (sheets.Any(x => x.Metadata.MetaTableId is 1470 or 1471) &&
                       disclosure.Title?.Contains("حسابرسی نشده", StringComparison.Ordinal) == true)
        {
            auditStatus =  PortfolioAuditStatus.Unaudited;
        }
        else if (sheets.Any(x => x.Metadata.MetaTableId is 1470 or 1471) &&
                       disclosure.Title?.Contains("حسابرسی شده", StringComparison.Ordinal) == true)
        {
            auditStatus = PortfolioAuditStatus.Audited;
        }
        //
        // تست صورت مالی جدید پروسس نشده در کدال
        //
        if (sourceType == PortfolioSourceType.FinancialStatement && auditStatus == PortfolioAuditStatus.None)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(
                    "Portfolio audit status could not be resolved. " +
                    "TracingNo: {TracingNo}, Symbol: {Symbol}",
                    disclosure.TracingNo,
                    disclosure.Symbol);
            }

            return;
        }

        var entities = new List<InvestmentPortfolioPosition>();

        foreach (InvestmentPortfolioSheetData sheet in sheets)
        {
            IReadOnlyList<InvestmentPortfolioPositionData> positions =
                sheet.IsListed
                    ? MapListedRows(sheet.Rows)
                    : MapUnlistedRows(sheet.Rows);
           

            foreach (InvestmentPortfolioPositionData position in positions)
            {
                string fSortName = FSort.Normalize(position.CompanyName);

                int? childCompanyId = null;
                bool isListed = position.IsListed;

                bool hasListedMatch = aliasLookup.ContainsKey((fSortName, true));

                bool hasUnlistedMatch = aliasLookup.ContainsKey((fSortName, false));

                if (hasListedMatch != hasUnlistedMatch)
                {
                    bool resolvedIsListed = hasListedMatch;

                    if (aliasLookup.TryGetValue(
                            (fSortName, resolvedIsListed),
                            out List<int>? companyIds) &&
                        companyIds.Count == 1)
                    {
                        childCompanyId = companyIds[0];

                        isListed = resolvedIsListed;
                    }
                }
                entities.Add(
                    new InvestmentPortfolioPosition(
                        parentCompanyId: parentCompanyId.Value,
                        childCompanyId: childCompanyId,
                        rawCompanyName: position.CompanyName,
                        fSortName: fSortName,
                        periodEndDate: periodEndDate,
                        sourceType: sourceType,
                        auditStatus: auditStatus,
                        isListed: isListed,
                        rowSequence: position.RowSequence,
                        capital: ParseNullableDecimal(position.Capital),
                        nominalValue: ParseNullableDecimal(position.NominalValue),
                        beginningQuantity: ParseNullableDecimal(position.BeginningQuantity),
                        beginningCost: ParseNullableDecimal(position.BeginningCost),
                        beginningMarketValue: ParseNullableDecimal(position.BeginningMarketValue),
                        changeQuantity: ParseNullableDecimal(position.ChangeQuantity),
                        changeCost: ParseNullableDecimal(position.ChangeCost),
                        changeMarketValue: ParseNullableDecimal(position.ChangeMarketValue),
                        ownershipPercent: ParseNullableDecimal(position.OwnershipPercent),
                        endingQuantity: ParseNullableDecimal(position.EndingQuantity),
                        endingCost: ParseNullableDecimal(position.EndingCost),
                        endingMarketValue: ParseNullableDecimal(position.EndingMarketValue),
                        endingCostPerShare: ParseNullableDecimal(position.EndingCostPerShare),
                        endingMarketPrice: ParseNullableDecimal(position.EndingMarketPrice),
                        increaseDecrease: ParseNullableDecimal(position.IncreaseDecrease),
                        notes: position.Notes,
                        disclosureId: disclosure.Id,
                        tracingNo: disclosure.TracingNo,
                        publishDateTime: disclosure.PublishDateTime));
            }
        }

        List<InvestmentPortfolioPosition> existing = await dbContext.InvestmentPortfolioPositions
                            .Where(x =>
                                   x.ParentCompanyId == parentCompanyId.Value &&
                                   x.PeriodEndDate == periodEndDate &&
                                   x.SourceType == sourceType &&
                                   x.AuditStatus == auditStatus)
                            .ToListAsync(cancellationToken);

        DateTime? latestExistingPublishDateTime = existing.Count > 0
                            ? existing.Max(x => x.PublishDateTime)
                            : null;
         
        if (latestExistingPublishDateTime.HasValue &&
            (!disclosure.PublishDateTime.HasValue ||
             disclosure.PublishDateTime.Value < latestExistingPublishDateTime.Value))
        {
            return;
        }
        dbContext.InvestmentPortfolioPositions.RemoveRange(existing);

        await dbContext.InvestmentPortfolioPositions
            .AddRangeAsync(
                entities,
                cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Investment portfolio saved. " +
                "ParentCompanyId: {ParentCompanyId}, " +
                "Symbol: {Symbol}, " +
                "Period: {Period}, " +
                "TracingNo: {TracingNo}, " +
                "Positions: {PositionCount}",
                parentCompanyId.Value,
                disclosure.Symbol,
                periodEndDate,
                disclosure.TracingNo,
                entities.Count);
        }
    }

    internal static IReadOnlyList<InvestmentPortfolioPositionData> MapListedRows(
        IReadOnlyList<CodalTableRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var result =
            new List<InvestmentPortfolioPositionData>();

        foreach (CodalTableRow row in rows)
        {
            if (!IsDataRow(row))
            {
                continue;
            }

            string? beginningQuantity =
                GetValue(row, 4);

            string? changeQuantity =
                GetValue(row, 7);

            var position = new InvestmentPortfolioPositionData(
                    RowSequence: row.RowSequence,
                    IsListed: true,
                    CompanyName: GetValue(row, 1)!,
                    Capital: GetValue(row, 2),
                    NominalValue: GetValue(row, 3),
                    BeginningQuantity: beginningQuantity,
                    BeginningCost: GetValue(row, 5),
                    BeginningMarketValue: GetValue(row, 6),
                    ChangeQuantity: changeQuantity,
                    ChangeCost: GetValue(row, 8),
                    ChangeMarketValue: GetValue(row, 9),
                    OwnershipPercent: GetValue(row, 10),
                    EndingQuantity: AddNumericValues(beginningQuantity, changeQuantity),
                    EndingCost: GetValue(row, 11),
                    EndingMarketValue: GetValue(row, 12),
                    EndingCostPerShare: GetValue(row, 13),
                    EndingMarketPrice: GetValue(row, 14),
                    IncreaseDecrease: GetValue(row, 15),
                    Notes: null);

            if (IsZeroPosition(position))
            {
                continue;
            }

            result.Add(position);
        }

        return result;
    }

    internal static IReadOnlyList<InvestmentPortfolioPositionData> MapUnlistedRows(
        IReadOnlyList<CodalTableRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var result =
            new List<InvestmentPortfolioPositionData>();

        foreach (CodalTableRow row in rows)
        {
            if (!IsDataRow(row))
            {
                continue;
            }

            string? beginningQuantity =
                GetValue(row, 4);

            string? changeQuantity =
                GetValue(row, 6);

            var position = new InvestmentPortfolioPositionData(
                    RowSequence: row.RowSequence,
                    IsListed: false,
                    CompanyName: GetValue(row, 1)!,
                    Capital: GetValue(row, 2),
                    NominalValue: GetValue(row, 3),
                    BeginningQuantity: beginningQuantity,
                    BeginningCost: GetValue(row, 5),
                    BeginningMarketValue: null,
                    ChangeQuantity: changeQuantity,
                    ChangeCost: GetValue(row, 7),
                    ChangeMarketValue: null,
                    OwnershipPercent: GetValue(row, 8),
                    EndingQuantity: AddNumericValues(beginningQuantity, changeQuantity),
                    EndingCost: GetValue(row, 9),
                    EndingMarketValue: null,
                    EndingCostPerShare: GetValue(row, 10),
                    EndingMarketPrice: null,
                    IncreaseDecrease: null,
                    Notes: GetValue(row, 11));

            if (IsZeroPosition(position))
            {
                continue;
            }

            result.Add(position);
        }

        return result;
    }

    private static bool IsDataRow(
        CodalTableRow row)
    {
        string? companyName = GetValue(row, 1);

        if (string.IsNullOrWhiteSpace(companyName))
        {
            return false;
        }

        string trimmedName = companyName.Trim();

        if (string.Equals(trimmedName, "جمع", StringComparison.Ordinal) ||
                          trimmedName.StartsWith("جمع ", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (KeyValuePair<int, string?> value in row.Values)
        {
            if (value.Key <= 1 || string.IsNullOrWhiteSpace(value.Value))
            {
                continue;
            }

            string normalized = NormalizeNumeric(value.Value);

            if (decimal.TryParse(
                    normalized,
                    NumberStyles.Number |
                    NumberStyles.AllowLeadingSign |
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out _))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetValue(
        CodalTableRow row,
        int columnSequence)
    {
        return row.Values.TryGetValue(
            columnSequence,
            out string? value)
                ? value
                : null;
    }

    private static string? AddNumericValues(
        string? first,
        string? second)
    {
        if (string.IsNullOrWhiteSpace(first) &&
            string.IsNullOrWhiteSpace(second))
        {
            return null;
        }

        decimal result =
            ParseDecimal(first) +
            ParseDecimal(second);

        return result.ToString(
            CultureInfo.InvariantCulture);
    }

    private static decimal ParseDecimal(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        string normalized =
            NormalizeNumeric(value);

        if (!decimal.TryParse(
                normalized,
                NumberStyles.Number |
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal result))
        {
            throw new FormatException(
                $"Invalid portfolio numeric value '{value}'.");
        }

        return result;
    }

    private static decimal? ParseNullableDecimal(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized =
            NormalizeNumeric(value);

        if (!decimal.TryParse(
                normalized,
                NumberStyles.Number |
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out decimal result))
        {
            throw new FormatException(
                $"Invalid portfolio numeric value '{value}'.");
        }

        return result;
    }

    private static string NormalizeNumeric(
        string value)
    {
        return value
            .Trim()
            .Replace("٬", string.Empty, StringComparison.Ordinal)
            .Replace(",", string.Empty, StringComparison.Ordinal)
            .Replace("٫", ".", StringComparison.Ordinal)
            .Replace("−", "-", StringComparison.Ordinal)
            .Replace('۰', '0')
            .Replace('۱', '1')
            .Replace('۲', '2')
            .Replace('۳', '3')
            .Replace('۴', '4')
            .Replace('۵', '5')
            .Replace('۶', '6')
            .Replace('۷', '7')
            .Replace('۸', '8')
            .Replace('۹', '9')
            .Replace('٠', '0')
            .Replace('١', '1')
            .Replace('٢', '2')
            .Replace('٣', '3')
            .Replace('٤', '4')
            .Replace('٥', '5')
            .Replace('٦', '6')
            .Replace('٧', '7')
            .Replace('٨', '8')
            .Replace('٩', '9');
    }
    private static bool IsZeroPosition(InvestmentPortfolioPositionData position)
    {
        return
            ParseDecimal(position.BeginningQuantity) == 0m &&
            ParseDecimal(position.BeginningCost) == 0m &&
            ParseDecimal(position.BeginningMarketValue) == 0m &&
            ParseDecimal(position.ChangeQuantity) == 0m &&
            ParseDecimal(position.ChangeCost) == 0m &&
            ParseDecimal(position.ChangeMarketValue) == 0m &&
            ParseDecimal(position.OwnershipPercent) == 0m &&
            ParseDecimal(position.EndingQuantity) == 0m &&
            ParseDecimal(position.EndingCost) == 0m &&
            ParseDecimal(position.EndingMarketValue) == 0m &&
            ParseDecimal(position.EndingCostPerShare) == 0m &&
            ParseDecimal(position.EndingMarketPrice) == 0m &&
            ParseDecimal(position.IncreaseDecrease) == 0m;
    }
}

internal sealed record InvestmentPortfolioPositionData(
    int RowSequence,
    bool IsListed,
    string CompanyName,
    string? Capital,
    string? NominalValue,
    string? BeginningQuantity,
    string? BeginningCost,
    string? BeginningMarketValue,
    string? ChangeQuantity,
    string? ChangeCost,
    string? ChangeMarketValue,
    string? OwnershipPercent,
    string? EndingQuantity,
    string? EndingCost,
    string? EndingMarketValue,
    string? EndingCostPerShare,
    string? EndingMarketPrice,
    string? IncreaseDecrease,
    string? Notes);
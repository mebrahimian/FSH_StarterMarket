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
    ILogger<InvestmentPortfolioProcessor> logger,
    IPortfolioChildCompanyResolver childCompanyResolver)
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
        bool hasPortfolioSheet = sheets.Any(x => x.Metadata.MetaTableCode is 1470 or 1471);

        if (!hasPortfolioSheet)
        {
            return;
        }
        string? periodEndDate =
            sheets
                .Select(x => x.PeriodEndDate)
                .FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x));

        if (string.IsNullOrWhiteSpace(periodEndDate))
        {
            return;
        }
        PortfolioSourceType sourceType = disclosure.Let == 6
        ? PortfolioSourceType.FinancialStatement
        : PortfolioSourceType.MonthlyActivity;

        string? reportSymbol =
            sheets
                .Select(x => x.Metadata.ReportSymbol)
                .FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x));

        if (!string.IsNullOrWhiteSpace(reportSymbol) &&
            !string.Equals(FSort.Normalize(reportSymbol), FSort.Normalize(disclosure.Symbol), StringComparison.Ordinal))
        {
            int deletedPositions =
                await dbContext.InvestmentPortfolioPositions
                    .Where(x => x.DisclosureId == disclosure.Id)
                    .ExecuteDeleteAsync(cancellationToken);

            int deletedMetadata =
                await dbContext.InvestmentPortfolioReportMetadata
                    .Where(x => x.DisclosureId == disclosure.Id)
                    .ExecuteDeleteAsync(cancellationToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Portfolio report belongs to a different report symbol." +
                    "TracingNo: {TracingNo}, DisclosureSymbol: {DisclosureSymbol}, " +
                    "ReportSymbol: {ReportSymbol}, DeletedPositions: {DeletedPositions}, " +
                    "DeletedMetadata: {DeletedMetadata}",
                    disclosure.TracingNo,
                    disclosure.Symbol,
                    reportSymbol,
                    deletedPositions,
                    deletedMetadata);
            }

            return;
        }

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

        ///////////////////////////////////////////
        List<InvestmentPortfolioReportMetadata> existingMetadata = 
            await dbContext.InvestmentPortfolioReportMetadata
                           .Where(x => x.TracingNo == disclosure.TracingNo)
                           .ToListAsync(cancellationToken);

        foreach (CodalDatasourceMetadata metadata in
                 sheets.Select(sheet => sheet.Metadata))
        {
            InvestmentPortfolioReportMetadata? existingReportMetadata =
                existingMetadata.FirstOrDefault(x =>
                       x.SheetCode == metadata.SheetCode &&
                       x.MetaTableId == metadata.MetaTableId &&
                       x.MetaTableCode == metadata.MetaTableCode);

            if (existingReportMetadata is not null)
            {
                existingReportMetadata.UpdateReportHeader(
                    metadata.ReportSymbol,
                    metadata.ReportCompanyName,
                    metadata.RegisteredCapital,
                    metadata.UnauthorizedCapital);

                continue;
            }

            dbContext.InvestmentPortfolioReportMetadata.Add(
                new InvestmentPortfolioReportMetadata(
                disclosure.Id,
                disclosure.TracingNo,
                metadata.PeriodEndToDate,
                metadata.YearEndToDate,
                metadata.Period,
                metadata.Type,
                metadata.SheetCode,
                metadata.MetaTableId,
                metadata.MetaTableCode,
                metadata.TitleFa,
                metadata.TitleEn,
                sourceType,
                auditStatus,
                metadata.ReportSymbol,
                metadata.ReportCompanyName,
                metadata.RegisteredCapital,
                metadata.UnauthorizedCapital));
        }
        //////////////////////////////////////////
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
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
                try
                {
                    PortfolioChildCompanyResolution resolution =
                        await childCompanyResolver
                            .ResolveAsync(
                                position.CompanyName,
                                position.IsListed,
                                cancellationToken)
                            .ConfigureAwait(false);

                    entities.Add(
                        new InvestmentPortfolioPosition(
                            parentCompanyId: parentCompanyId.Value,
                            childCompanyId: resolution.CompanyId,
                            rawCompanyName: position.CompanyName,
                            fSortName: resolution.FSortName,
                            periodEndDate: periodEndDate,
                            sourceType: sourceType,
                            auditStatus: auditStatus,
                            isListed: resolution.IsListed,
                            rowSequence: position.RowSequence,
                            capital: ParseDecimal(position.Capital),
                            nominalValue: ParseDecimal(position.NominalValue),
                            beginningQuantity: ParseDecimal(position.BeginningQuantity),
                            beginningCost: ParseDecimal(position.BeginningCost),
                            beginningMarketValue: ParseDecimal(position.BeginningMarketValue),
                            changeQuantity: ParseDecimal(position.ChangeQuantity),
                            changeCost: ParseDecimal(position.ChangeCost),
                            changeMarketValue: ParseDecimal(position.ChangeMarketValue),
                            ownershipPercent: ParseDecimal(position.OwnershipPercent),
                            endingQuantity: ParseDecimal(position.EndingQuantity),
                            endingCost: ParseDecimal(position.EndingCost),
                            endingMarketValue: ParseDecimal(position.EndingMarketValue),
                            endingCostPerShare: ParseDecimal(position.EndingCostPerShare),
                            endingMarketPrice: ParseDecimal(position.EndingMarketPrice),
                            increaseDecrease: ParseDecimal(position.IncreaseDecrease),
                            notes: position.Notes,
                            disclosureId: disclosure.Id,
                            tracingNo: disclosure.TracingNo,
                            publishDateTime: disclosure.PublishDateTime));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"ResolveAsync failed. " +
                        $"CompanyName='{position.CompanyName}', " +
                        $"IsListed={position.IsListed}",
                        ex);
                }
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
        IReadOnlyList<InvestmentPortfolioPosition> previousPortfolio =
            await GetPreviousPortfolioAsync(parentCompanyId.Value,
                                            periodEndDate, cancellationToken);

        await ApplyHoldingEntryExitAsync(
            disclosure.Symbol,
            previousPortfolio,
            entities,
            periodEndDate,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

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
    private async Task<IReadOnlyList<InvestmentPortfolioPosition>>
        GetPreviousPortfolioAsync(
        int parentCompanyId,
        string currentPeriodEndDate,
        CancellationToken cancellationToken)
    {
        List<string> periods =
            await dbContext.InvestmentPortfolioPositions
                .AsNoTracking()
                .Where(x =>
                    x.ParentCompanyId == parentCompanyId &&
                    x.SourceType ==
                        PortfolioSourceType.MonthlyActivity)
                .Select(x => x.PeriodEndDate)
                .Distinct()
                .ToListAsync(cancellationToken);

        string? previousPeriodEndDate =
            periods
                .Where(x =>
                    string.CompareOrdinal(
                        x,
                        currentPeriodEndDate) < 0)
                .OrderByDescending(
                    x => x,
                    StringComparer.Ordinal)
                .FirstOrDefault();

        if (previousPeriodEndDate is null)
        {
            return [];
        }

        return await dbContext.InvestmentPortfolioPositions
            .AsNoTracking()
            .Where(x =>
                x.ParentCompanyId == parentCompanyId &&
                x.SourceType ==
                    PortfolioSourceType.MonthlyActivity &&
                x.PeriodEndDate ==
                    previousPeriodEndDate)
            .ToListAsync(cancellationToken);
    }
    private async Task ApplyHoldingEntryExitAsync(
    string parentSymbol,
    IReadOnlyList<InvestmentPortfolioPosition> previousPortfolio,
    IReadOnlyList<InvestmentPortfolioPosition> currentPortfolio,
    string currentPeriodEndDate,
    CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPeriodEndDate);

        int[] listedCompanyIds =
            previousPortfolio
                .Concat(currentPortfolio)
                .Where(x =>
                    x.IsListed &&
                    x.ChildCompanyId.HasValue)
                .Select(x => x.ChildCompanyId!.Value)
                .Distinct()
                .ToArray();

        int[] unlistedCompanyIds =
            previousPortfolio
                .Concat(currentPortfolio)
                .Where(x =>
                    !x.IsListed &&
                    x.ChildCompanyId.HasValue)
                .Select(x => x.ChildCompanyId!.Value)
                .Distinct()
                .ToArray();

        List<PortfolioHoldingAsset> holdingAssets =
            await dbContext.PortfolioHoldingAssets
                .AsNoTracking()
                .Where(x =>
                    (x.ListedCompanyId.HasValue &&
                     listedCompanyIds.Contains(x.ListedCompanyId.Value)) ||
                    (x.UnlistedCompanyId.HasValue &&
                     unlistedCompanyIds.Contains(x.UnlistedCompanyId.Value)))
                .ToListAsync(cancellationToken);

        Dictionary<int, int> listedAssetLookup = [];
        Dictionary<int, int> unlistedAssetLookup = [];

        foreach (PortfolioHoldingAsset asset in holdingAssets)
        {
            if (asset.ListedCompanyId.HasValue)
            {
                listedAssetLookup.Add(
                    asset.ListedCompanyId.Value,
                    asset.Id);
            }

            if (asset.UnlistedCompanyId.HasValue)
            {
                unlistedAssetLookup.Add(
                    asset.UnlistedCompanyId.Value,
                    asset.Id);
            }
        }

        int GetHoldingAssetId(
            InvestmentPortfolioPosition position)
        {
            if (!position.ChildCompanyId.HasValue)
            {
                return 0;
            }

            int childCompanyId =
                position.ChildCompanyId.Value;

            bool found;

            int holdingAssetId;

            if (position.IsListed)
            {
                found = listedAssetLookup.TryGetValue(
                    childCompanyId,
                    out holdingAssetId);
            }
            else
            {
                found = unlistedAssetLookup.TryGetValue(
                    childCompanyId,
                    out holdingAssetId);
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"HoldingAsset not found. " +
                    $"ChildCompanyId={childCompanyId}, " +
                    $"IsListed={position.IsListed}");
            }

            return holdingAssetId;
        }

        Dictionary<int, bool> previousHoldings =
            previousPortfolio
                .Where(x => x.ChildCompanyId.HasValue)
                .Select(GetHoldingAssetId)
                .Where(x => x > 0)
                .Distinct()
                .ToDictionary(
                    x => x,
                    _ => true);

        HashSet<int> enteredHoldingAssetIds = [];

        foreach (InvestmentPortfolioPosition current
                 in currentPortfolio)
        {
            if (!current.ChildCompanyId.HasValue)
            {
                continue;
            }

            int holdingAssetId =
                GetHoldingAssetId(current);

            if (previousHoldings.ContainsKey(
                    holdingAssetId))
            {
                previousHoldings[holdingAssetId] =
                    false;

                continue;
            }

            enteredHoldingAssetIds.Add(
                holdingAssetId);
        }

        string normalizedParentSymbol =
            parentSymbol.Trim();

        List<InvestmentPortfolioHoldingPeriod>
            activeHoldingPeriods =
                await dbContext
                    .InvestmentPortfolioHoldingPeriods
                    .Where(x =>
                        x.ParentSymbol ==
                            normalizedParentSymbol &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

        // Entry
        foreach (int holdingAssetId
                 in enteredHoldingAssetIds)
        {
            bool alreadyActive =
                activeHoldingPeriods.Any(x =>
                    x.HoldingAssetId ==
                        holdingAssetId);

            if (alreadyActive)
            {
                continue;
            }

            var holdingPeriod =
                new InvestmentPortfolioHoldingPeriod(
                    normalizedParentSymbol,
                    holdingAssetId,
                    currentPeriodEndDate);

            dbContext
                .InvestmentPortfolioHoldingPeriods
                .Add(holdingPeriod);

            activeHoldingPeriods.Add(
                holdingPeriod);
        }

        // Exit
        foreach (KeyValuePair<int, bool> previous
                 in previousHoldings)
        {
            if (!previous.Value)
            {
                continue;
            }

            InvestmentPortfolioHoldingPeriod?
                activePeriod =
                    activeHoldingPeriods
                        .FirstOrDefault(x =>
                            x.HoldingAssetId ==
                                previous.Key);

            if (activePeriod is null)
            {
                logger.LogWarning(
                    "Active HoldingPeriod not found for Exit. " +
                    "ParentSymbol: {ParentSymbol}, " +
                    "HoldingAssetId: {HoldingAssetId}, " +
                    "PeriodEndDate: {PeriodEndDate}",
                    normalizedParentSymbol,
                    previous.Key,
                    currentPeriodEndDate);

                continue;
            }

            activePeriod.Close(
                currentPeriodEndDate);
        }
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


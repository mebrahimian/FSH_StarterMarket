using FSH.Framework.Shared.Utilities;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;

internal sealed class PortfolioChildCompanyResolver(MarketIntelligenceDbContext dbContext) : IPortfolioChildCompanyResolver
{
    private Dictionary<string, List<CodalCompanyImport>>? _codalByName;
    private static readonly HashSet<string> ExcludedPortfolioNames =
        new(StringComparer.Ordinal)
        {
            "سایرسهامپذیرفتهشدهدربورسوفرابورس",
            "سایرشرکتهایخارجازبورس",
            "سایرشرکتهایپذیرفتهشدهدربورس",
            "مشارکتهایمدنیخارجازبورس",
            "مشارکتهایمدنی)خارجازبورس(",
            "مشارکتهایمدنی)پذیرفتهشدهدربورس(",
            "اوراقمشارکتپذیرفتهشدهدربورس",
            "اوراقمشارکت(پذیرفتهشدهدربورس)",
            "اوراقمشارکت)پذیرفتهشدهدربورس(",
            "(حقتقدم)",
            "دراوراقبهاداربادرآمدثابتکاریزما",
            "سایرسهامدرجشدهدربازارهایپایهفرابورس"
        };
    private async Task<int?> EnsureRightsMasterInfoAsync(
    int parentCompanyId,
    string rightsFSortSymbol,
    CancellationToken cancellationToken)
    {
        int[] existingIds =
            await dbContext.CompanyMaster
                .Where(x =>
                    x.IsListed &&
                    x.FSortSymbol == rightsFSortSymbol)
                .Select(x => x.CompanyId)
                .Distinct()
                .Take(2)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        if (existingIds.Length == 1)
        {
            return existingIds[0];
        }

        if (existingIds.Length > 1)
        {
            return null;
        }

        DbConnection connection =
            dbContext.Database.GetDbConnection();

        bool shouldClose =
            connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection
                .OpenAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            await using DbCommand command =
                connection.CreateCommand();

            command.CommandText =
                """
            INSERT INTO Bors.dbo.MasterInfo
            (
                [نماد],
                [نام],
                Grp,
                GrpId,
                Nav,
                ShareCount,
                Zarar,
                DateAdvise,
                Status,
                FSortName,
                FSortNamad,
                EPS1,
                FiscalDate
            )
            OUTPUT INSERTED.CompanyId
            SELECT
                [نماد] + N'ح',
                [نام] + N' (حق تقدم)',
                Grp,
                GrpId,
                0,
                0,
                0,
                DateAdvise,
                Status,
                FSortName + N'(حقتقدم)',
                FSortNamad + N'ح',
                0,
                FiscalDate
            FROM Bors.dbo.MasterInfo
            WHERE CompanyId = @ParentCompanyId;
            """;

            DbParameter parentParameter =
                command.CreateParameter();

            parentParameter.ParameterName =
                "@ParentCompanyId";

            parentParameter.Value =
                parentCompanyId;

            command.Parameters.Add(parentParameter);

            object? result =
                await command
                    .ExecuteScalarAsync(cancellationToken)
                    .ConfigureAwait(false);

            return result is int companyId
                ? companyId
                : null;
        }
        finally
        {
            if (shouldClose)
            {
                await connection
                    .CloseAsync()
                    .ConfigureAwait(false);
            }
        }
    }
    public async Task<PortfolioChildCompanyResolution> ResolveAsync(
    string rawCompanyName,
    bool reportedIsListed,
    CancellationToken cancellationToken)
    {
        string fSortName = FSort.Normalize(rawCompanyName);

        if (ExcludedPortfolioNames.Contains(fSortName))
        {
            return new PortfolioChildCompanyResolution(
                CompanyId: null,
                HoldingAssetId: null,
                IsListed: reportedIsListed,
                IsExcluded: true,
                FSortName: fSortName);
        }

        var aliases = await dbContext.PortfolioCompanyAliases
            .Where(x =>
                x.IsActive &&
                x.FSortName == fSortName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        int[] companyIds = aliases
            .Select(x => x.CompanyId)
            .Distinct()
            .ToArray();

        if (companyIds.Length == 1)
        {
            var alias = aliases[0];

            if (alias.HoldingAssetId.HasValue)
            {
                var asset = await dbContext.PortfolioHoldingAssets
                    .FirstOrDefaultAsync(
                        x => x.Id == alias.HoldingAssetId.Value,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (asset?.ListedCompanyId is int listedCompanyId)
                {
                    return new PortfolioChildCompanyResolution(
                        CompanyId: listedCompanyId,
                        HoldingAssetId: asset.Id,
                        IsListed: true,
                        IsExcluded: false,
                        FSortName: fSortName);
                }

                if (asset?.UnlistedCompanyId is int unlistedCompanyId)
                {
                    return new PortfolioChildCompanyResolution(
                        CompanyId: unlistedCompanyId,
                        HoldingAssetId: asset.Id,
                        IsListed: false,
                        IsExcluded: false,
                        FSortName: fSortName);
                }
            }

            int aliasCompanyId = companyIds[0];

            var masterById = await dbContext.CompanyMaster
                .FirstOrDefaultAsync(
                    x => x.CompanyId == aliasCompanyId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (masterById is not null)
            {
                PortfolioHoldingAsset asset =
                    await ResolveHoldingAssetAsync(
                        masterById.CompanyId,
                        masterById.IsListed,
                        masterById.CompanyName,
                        masterById.FSortName,
                        masterById.Symbol,
                        cancellationToken)
                    .ConfigureAwait(false);

                alias.AssignHoldingAsset(asset.Id);

                await dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                return new PortfolioChildCompanyResolution(
                    CompanyId: masterById.CompanyId,
                    HoldingAssetId: asset.Id,
                    IsListed: masterById.IsListed,
                    IsExcluded: false,
                    FSortName: fSortName);
            }

            // Alias تاریخی است ولی CompanyId آن دیگر canonical نیست.
            // اینجا return نمی‌کنیم؛ می‌افتد روی CompanyMaster-by-name و بعد Codal.
        }
        var masterMatches = await dbContext.CompanyMaster
                .Where(x => x.FSortName == fSortName)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

        var masterCompanyIds = masterMatches
            .Select(x => x.CompanyId)
            .Distinct()
            .ToArray();

        if (masterCompanyIds.Length == 1)
        {
            var master = masterMatches[0];

            PortfolioHoldingAsset asset =
                await ResolveHoldingAssetAsync(
                    master.CompanyId,
                    master.IsListed,
                    master.CompanyName,
                    master.FSortName,
                    master.Symbol,
                    cancellationToken)
                .ConfigureAwait(false);

            if (aliases.Count == 0)
            {
                var newAlias = new PortfolioCompanyAlias(
                    companyId: master.CompanyId,
                    symbol: master.Symbol,
                    fSortSymbol: master.FSortSymbol,
                    aliasName: rawCompanyName,
                    fSortName: fSortName,
                    isListed: master.IsListed,
                    holdingAssetId: asset.Id);

                dbContext.PortfolioCompanyAliases.Add(newAlias);

                await dbContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return new PortfolioChildCompanyResolution(
                CompanyId: master.CompanyId,
                HoldingAssetId: asset.Id,
                IsListed: master.IsListed,
                IsExcluded: false,
                FSortName: fSortName);
        }
        const string rightsSuffix = "(حقتقدم)";

        if (fSortName.EndsWith(
                rightsSuffix,
                StringComparison.Ordinal))
        {
            string parentFSortName =
                fSortName[..^rightsSuffix.Length];

            var parentMatches =
                await dbContext.CompanyMaster
                    .Where(x =>
                        x.IsListed &&
                        x.FSortName == parentFSortName)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            int[] parentCompanyIds =
                parentMatches
                    .Select(x => x.CompanyId)
                    .Distinct()
                    .ToArray();

            if (parentCompanyIds.Length == 1)
            {
                var parent = parentMatches[0];

                string rightsFSortSymbol =
                    FSort.Normalize(parent.Symbol + "ح");

                var rightsMatches =
                    await dbContext.CompanyMaster
                        .Where(x =>
                            x.IsListed &&
                            x.FSortSymbol == rightsFSortSymbol)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                int[] rightsCompanyIds =
                    rightsMatches
                        .Select(x => x.CompanyId)
                        .Distinct()
                        .ToArray();

                if (rightsCompanyIds.Length == 1)
                {
                    var rights = rightsMatches[0];

                    PortfolioHoldingAsset asset =
                        await ResolveHoldingAssetAsync(
                            rights.CompanyId,
                            true,
                            rights.CompanyName,
                            rights.FSortName,
                            rights.Symbol,
                            cancellationToken)
                        .ConfigureAwait(false);

                    return new PortfolioChildCompanyResolution(
                        CompanyId: rights.CompanyId,
                        HoldingAssetId: asset.Id,
                        IsListed: true,
                        IsExcluded: false,
                        FSortName: fSortName);
                }
                else if (rightsCompanyIds.Length == 0)
                {
                    int? rightsCompanyId =
                        await EnsureRightsMasterInfoAsync(
                            parent.CompanyId,
                            rightsFSortSymbol,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (rightsCompanyId.HasValue)
                    {
                        var rights = await dbContext.CompanyMaster
                            .FirstAsync(
                                x => x.CompanyId == rightsCompanyId.Value,
                                cancellationToken)
                            .ConfigureAwait(false);

                        PortfolioHoldingAsset asset =
                            await ResolveHoldingAssetAsync(
                                rights.CompanyId,
                                true,
                                rights.CompanyName,
                                rights.FSortName,
                                rights.Symbol,
                                cancellationToken)
                            .ConfigureAwait(false);
                        if (aliases.Count == 0)
                        {
                            var newAlias = new PortfolioCompanyAlias(
                                companyId: rights.CompanyId,
                                symbol: rights.Symbol,
                                fSortSymbol: rights.FSortSymbol,
                                aliasName: rawCompanyName,
                                fSortName: fSortName,
                                isListed: true,
                                holdingAssetId: asset.Id);

                            dbContext.PortfolioCompanyAliases.Add(newAlias);

                            await dbContext
                                .SaveChangesAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }

                        return new PortfolioChildCompanyResolution(
                            CompanyId: rights.CompanyId,
                            HoldingAssetId: asset.Id,
                            IsListed: true,
                            IsExcluded: false,
                            FSortName: fSortName);
                    }
                }
            }
        }
        IReadOnlyList<CodalCompanyImport> codalMatches =
                await FindCodalMatchesAsync(
                fSortName,
                cancellationToken)
               .ConfigureAwait(false);

        if (codalMatches.Count == 1)
        {
            CodalCompanyImport codal = codalMatches[0];

            int? companyId =
                await EnsureMasterInfoAsync(
                    codal,
                    fSortName,
                    cancellationToken)
                .ConfigureAwait(false);

            if (companyId.HasValue)
            {
                string companyName =
                    codal.CompanyName ?? codal.Symbol;

                string fSortSymbol =
                    FSort.Normalize(codal.Symbol);

                PortfolioHoldingAsset asset =
                    await ResolveHoldingAssetAsync(
                        companyId.Value,
                        true,
                        companyName,
                        fSortName,
                        codal.Symbol,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (aliases.Count == 1)
                {
                    // Alias تاریخی را نگه می‌داریم؛
                    // فقط به Asset canonical وصلش می‌کنیم.
                    var alias = aliases[0];

                    if (!alias.HoldingAssetId.HasValue)
                    {
                        alias.AssignHoldingAsset(asset.Id);

                        await dbContext
                            .SaveChangesAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                else if (aliases.Count == 0)
                {
                    var newAlias = new PortfolioCompanyAlias(
                        companyId: companyId.Value,
                        symbol: codal.Symbol,
                        fSortSymbol: fSortSymbol,
                        aliasName: rawCompanyName,
                        fSortName: fSortName,
                        isListed: true,
                        holdingAssetId: asset.Id);

                    dbContext.PortfolioCompanyAliases.Add(newAlias);

                    await dbContext
                        .SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                return new PortfolioChildCompanyResolution(
                    CompanyId: companyId.Value,
                    HoldingAssetId: asset.Id,
                    IsListed: true,
                    IsExcluded: false,
                    FSortName: fSortName);
            }
        }

        return new PortfolioChildCompanyResolution(
            CompanyId: null,
            HoldingAssetId: null,
            IsListed: reportedIsListed,
            IsExcluded: false,
            FSortName: fSortName);
    
    }
    private async Task<PortfolioHoldingAsset> ResolveHoldingAssetAsync(
    int companyId,
    bool isListed,
    string companyName,
    string fSortName,
    string? symbol,
    CancellationToken cancellationToken)
    {
        PortfolioHoldingAsset? asset;

        if (isListed)
        {
            asset = await dbContext.PortfolioHoldingAssets
                .FirstOrDefaultAsync(
                    x => x.ListedCompanyId == companyId,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            asset = await dbContext.PortfolioHoldingAssets
                .FirstOrDefaultAsync(
                    x => x.UnlistedCompanyId == companyId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (asset is not null)
        {
            return asset;
        }

        asset = new PortfolioHoldingAsset(
            companyName,
            fSortName,
            companyId,
            isListed,
            symbol);

        dbContext.PortfolioHoldingAssets.Add(asset);

        await dbContext.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return asset;
    }
    private async Task<IReadOnlyList<CodalCompanyImport>> FindCodalMatchesAsync(
    string fSortName,
    CancellationToken cancellationToken)
    {
        if (_codalByName is null)
        {
            List<CodalCompanyImport> companies =
                await dbContext.CodalCompanyImports
                    .AsNoTracking()
                    .Where(x => x.CompanyName != null)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

            _codalByName = companies
                .GroupBy(x => FSort.Normalize(x.CompanyName!))
                .ToDictionary(
                    x => x.Key,
                    x => x.ToList(),
                    StringComparer.Ordinal);
        }

        return _codalByName.TryGetValue(
            fSortName,
            out List<CodalCompanyImport>? matches)
            ? matches
            : [];
    }
    private async Task<int?> EnsureMasterInfoAsync(
    CodalCompanyImport codal,
    string fSortName,
    CancellationToken cancellationToken)
    {
        string fSortSymbol =
            FSort.Normalize(codal.Symbol);

        int[] existingIds =
            await dbContext.CompanyMaster
                .Where(x =>
                    x.IsListed &&
                    x.FSortSymbol == fSortSymbol)
                .Select(x => x.CompanyId)
                .Distinct()
                .Take(2)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

        if (existingIds.Length == 1)
        {
            return existingIds[0];
        }

        if (existingIds.Length > 1)
        {
            return null;
        }

        if (!int.TryParse(
                codal.IndustryId,
                out int grpId))
        {
            return null;
        }

        DbConnection connection =
            dbContext.Database.GetDbConnection();

        bool shouldClose =
            connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection
                .OpenAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            await using DbCommand command =
                connection.CreateCommand();

            command.CommandText =
                """
            INSERT INTO Bors.dbo.MasterInfo
            (
                [نماد],
                [نام],
                Grp,
                GrpId,
                Nav,
                ShareCount,
                Zarar,
                Status,
                FSortName,
                FSortNamad,
                EPS1
            )
            OUTPUT INSERTED.CompanyId
            VALUES
            (
                @Symbol,
                @CompanyName,
                N'',
                @GrpId,
                0,
                0,
                0,
                1,
                @FSortName,
                @FSortSymbol,
                0
            );
            """;

            DbParameter symbolParameter =
                command.CreateParameter();

            symbolParameter.ParameterName = "@Symbol";
            symbolParameter.Value = codal.Symbol;
            command.Parameters.Add(symbolParameter);

            DbParameter nameParameter =
                command.CreateParameter();

            nameParameter.ParameterName = "@CompanyName";
            nameParameter.Value =
                codal.CompanyName ?? codal.Symbol;

            command.Parameters.Add(nameParameter);

            DbParameter grpIdParameter =
                command.CreateParameter();

            grpIdParameter.ParameterName = "@GrpId";
            grpIdParameter.Value = grpId;
            command.Parameters.Add(grpIdParameter);

            DbParameter fSortNameParameter =
                command.CreateParameter();

            fSortNameParameter.ParameterName = "@FSortName";
            fSortNameParameter.Value = fSortName;
            command.Parameters.Add(fSortNameParameter);

            DbParameter fSortSymbolParameter =
                command.CreateParameter();

            fSortSymbolParameter.ParameterName =
                "@FSortSymbol";

            fSortSymbolParameter.Value = fSortSymbol;
            command.Parameters.Add(fSortSymbolParameter);

            object? result =
                await command
                    .ExecuteScalarAsync(cancellationToken)
                    .ConfigureAwait(false);

            return result is int companyId
                 ? companyId
                 : null;
        }
        finally
        {
            if (shouldClose)
            {
                await connection
                    .CloseAsync()
                    .ConfigureAwait(false);
            }
        }
    }
}
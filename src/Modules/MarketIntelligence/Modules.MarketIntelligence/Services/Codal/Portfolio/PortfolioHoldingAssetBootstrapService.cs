using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Data.Views;
using FSH.Modules.MarketIntelligence.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.MarketIntelligence.Services.Portfolio;

public sealed class PortfolioHoldingAssetBootstrapService(
    MarketIntelligenceDbContext dbContext)
{
    public async Task<PortfolioHoldingAssetBootstrapResult> RunAsync(
    CancellationToken cancellationToken = default)
    {
        List<PortfolioCompanyAlias> aliases =
            await dbContext.PortfolioCompanyAliases
                .Where(x =>
                    x.IsActive &&
                    x.HoldingAssetId == null)
                .OrderBy(x => x.CompanyId)
                .ThenByDescending(x => x.IsListed)
                .ToListAsync(cancellationToken);

        if (aliases.Count == 0)
        {
            return new PortfolioHoldingAssetBootstrapResult(
                0,
                0,
                0);
        }

        // CompanyMaster مرجع canonical ماست.
        List<CompanyMasterView> allCompanies =
            await dbContext.CompanyMaster
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        // هویت واقعی Company:
        // CompanyId + IsListed
        Dictionary<(int CompanyId, bool IsListed), List<CompanyMasterView>>
            companyByIdentity =
                allCompanies
                    .GroupBy(x =>
                        (x.CompanyId, x.IsListed))
                    .ToDictionary(
                        x => x.Key,
                        x => x.ToList());

        // فقط برای پیدا کردن counterpart بورسی / غیربورسی.
        // دقت کن FSortName اینجا از CompanyMaster می‌آید، نه Alias.
        Dictionary<string, List<CompanyMasterView>>
            companyByFSortName =
                allCompanies
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.FSortName))
                    .GroupBy(x => x.FSortName)
                    .ToDictionary(
                        x => x.Key,
                        x => x.ToList(),
                        StringComparer.Ordinal);

        List<PortfolioHoldingAsset> existingAssets =
            await dbContext.PortfolioHoldingAssets
                .ToListAsync(cancellationToken);

        var pendingAssignments =
            new List<PendingAliasAssignment>();

        int createdAssets = 0;
        int skippedGroups = 0;

        // مهم:
        // دیگر بر اساس Alias.FSortName گروه‌بندی نمی‌کنیم.
        foreach (var group in aliases.GroupBy(x =>
                     (x.CompanyId, x.IsListed)))
        {
            List<PortfolioCompanyAlias> groupAliases =
                group.ToList();

            //-------------------------------------------------
            // 1. Company واقعی خود Alias را از Master پیدا کن
            //-------------------------------------------------

            if (!companyByIdentity.TryGetValue(
                    group.Key,
                    out List<CompanyMasterView>? masterMatches) ||
                masterMatches.Count != 1)
            {
                skippedGroups++;
                continue;
            }

            CompanyMasterView master =
                masterMatches[0];

            if (string.IsNullOrWhiteSpace(master.FSortName))
            {
                skippedGroups++;
                continue;
            }

            //-------------------------------------------------
            // 2. counterpart را فقط با Master.FSortName پیدا کن
            //-------------------------------------------------

            List<CompanyMasterView> sameCompanyCandidates =
                companyByFSortName.TryGetValue(
                    master.FSortName,
                    out List<CompanyMasterView>? matches)
                    ? matches
                    : [];

            List<CompanyMasterView> listedCompanies =
                sameCompanyCandidates
                    .Where(x => x.IsListed)
                    .GroupBy(x => x.CompanyId)
                    .Select(x => x.First())
                    .ToList();

            List<CompanyMasterView> unlistedCompanies =
                sameCompanyCandidates
                    .Where(x => !x.IsListed)
                    .GroupBy(x => x.CompanyId)
                    .Select(x => x.First())
                    .ToList();

            // بعد از cleanup دیتابیس نباید این اتفاق بیفتد.
            // اگر دوباره رخ داد، Bootstrap حدس نمی‌زند.
            if (listedCompanies.Count > 1 ||
                unlistedCompanies.Count > 1)
            {
                skippedGroups++;
                continue;
            }

            CompanyMasterView? listedCompany =
                listedCompanies.SingleOrDefault();

            CompanyMasterView? unlistedCompany =
                unlistedCompanies.SingleOrDefault();

            int? listedCompanyId =
                listedCompany?.CompanyId;

            int? unlistedCompanyId =
                unlistedCompany?.CompanyId;

            //-------------------------------------------------
            // 3. خود Master همیشه هویت قطعی است
            //-------------------------------------------------

            if (master.IsListed)
            {
                listedCompanyId = master.CompanyId;
            }
            else
            {
                unlistedCompanyId = master.CompanyId;
            }

            //-------------------------------------------------
            // 4. Asset موجود را با CompanyId پیدا کن
            //-------------------------------------------------

            PortfolioHoldingAsset? listedAsset =
                listedCompanyId.HasValue
                    ? existingAssets.FirstOrDefault(x =>
                        x.ListedCompanyId ==
                        listedCompanyId.Value)
                    : null;

            PortfolioHoldingAsset? unlistedAsset =
                unlistedCompanyId.HasValue
                    ? existingAssets.FirstOrDefault(x =>
                        x.UnlistedCompanyId ==
                        unlistedCompanyId.Value)
                    : null;

            // اگر دو Asset مختلف برای دو سمت پیدا شد،
            // داده قبلاً ناسازگار شده و نباید خودکار merge کنیم.
            if (listedAsset is not null &&
                unlistedAsset is not null &&
                !ReferenceEquals(listedAsset, unlistedAsset))
            {
                skippedGroups++;
                continue;
            }

            PortfolioHoldingAsset? asset =
                listedAsset ??
                unlistedAsset;

            //-------------------------------------------------
            // 5. Asset جدید
            //-------------------------------------------------

            if (asset is null)
            {
                CompanyMasterView canonicalCompany =
                    listedCompany ??
                    unlistedCompany ??
                    master;

                int initialCompanyId;
                bool initialIsListed;
                string? symbol;

                // اگر شرکت بورسی وجود دارد، آن را identity اولیه می‌گیریم.
                if (listedCompanyId.HasValue)
                {
                    initialCompanyId =
                        listedCompanyId.Value;

                    initialIsListed = true;

                    symbol =
                        listedCompany?.Symbol ??
                        master.Symbol;
                }
                else
                {
                    initialCompanyId =
                        unlistedCompanyId!.Value;

                    initialIsListed = false;
                    symbol = null;
                }

                asset =
                    new PortfolioHoldingAsset(
                        canonicalCompany.CompanyName,
                        master.FSortName,
                        initialCompanyId,
                        initialIsListed,
                        symbol);

                // اگر Asset با Listed ساخته شد و counterpart
                // غیربورسی هم داریم، وصلش کن.
                if (initialIsListed &&
                    unlistedCompanyId.HasValue)
                {
                    asset.LinkUnlistedCompany(
                        unlistedCompanyId.Value);
                }

                // حالت عکس برای completeness.
                if (!initialIsListed &&
                    listedCompanyId.HasValue)
                {
                    string? listedSymbol =
                        listedCompany?.Symbol;

                    if (string.IsNullOrWhiteSpace(
                            listedSymbol))
                    {
                        skippedGroups++;
                        continue;
                    }

                    asset.LinkListedCompany(
                        listedCompanyId.Value,
                        listedSymbol);
                }

                dbContext.PortfolioHoldingAssets.Add(
                    asset);

                // خیلی مهم:
                // Asset جدید همان لحظه وارد lookup داخلی شود
                // تا گروه counterpart دوباره Asset جدید نسازد.
                existingAssets.Add(asset);

                createdAssets++;
            }

            //-------------------------------------------------
            // 6. Asset موجود را تکمیل کن
            //-------------------------------------------------
            else
            {
                // Conflict را پنهان نمی‌کنیم.
                if (listedCompanyId.HasValue &&
                    asset.ListedCompanyId.HasValue &&
                    asset.ListedCompanyId.Value !=
                    listedCompanyId.Value)
                {
                    skippedGroups++;
                    continue;
                }

                if (unlistedCompanyId.HasValue &&
                    asset.UnlistedCompanyId.HasValue &&
                    asset.UnlistedCompanyId.Value !=
                    unlistedCompanyId.Value)
                {
                    skippedGroups++;
                    continue;
                }

                if (unlistedCompanyId.HasValue &&
                    !asset.UnlistedCompanyId.HasValue)
                {
                    asset.LinkUnlistedCompany(
                        unlistedCompanyId.Value);
                }

                if (listedCompanyId.HasValue &&
                    !asset.ListedCompanyId.HasValue)
                {
                    string? listedSymbol =
                        listedCompany?.Symbol;

                    if (string.IsNullOrWhiteSpace(
                            listedSymbol))
                    {
                        skippedGroups++;
                        continue;
                    }

                    asset.LinkListedCompany(
                        listedCompanyId.Value,
                        listedSymbol);
                }
            }

            //-------------------------------------------------
            // 7. فقط Aliasهای همین Company به Asset وصل شوند
            //-------------------------------------------------

            foreach (PortfolioCompanyAlias alias in
                     groupAliases)
            {
                pendingAssignments.Add(
                    new PendingAliasAssignment(
                        alias,
                        asset));
            }
        }

        //-------------------------------------------------
        // Atomic Bootstrap
        //-------------------------------------------------

        var strategy =
    dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await dbContext.Database.BeginTransactionAsync(
                    cancellationToken);

            // Assetها Id می‌گیرند.
            await dbContext.SaveChangesAsync(
                cancellationToken);

            foreach (PendingAliasAssignment assignment in
                     pendingAssignments)
            {
                assignment.Alias.AssignHoldingAsset(
                    assignment.Asset.Id);
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        });

        return new PortfolioHoldingAssetBootstrapResult(
            createdAssets,
            pendingAssignments.Count,
            skippedGroups);
    }

    private sealed record PendingAliasAssignment(
        PortfolioCompanyAlias Alias,
        PortfolioHoldingAsset Asset);
}

public sealed record PortfolioHoldingAssetBootstrapResult(
    int CreatedAssets,
    int AssignedAliases,
    int SkippedGroups);
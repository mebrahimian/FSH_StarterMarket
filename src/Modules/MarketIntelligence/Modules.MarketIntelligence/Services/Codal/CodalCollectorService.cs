using FSH.Framework.Shared.Dates;
using FSH.Modules.MarketIntelligence.Contracts.Dtos;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using FSH.Modules.MarketIntelligence.Services.Codal.Portfolio;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using FSH.Modules.MarketIntelligence.Services.Codal.Logging;
using FSH.Modules.MarketIntelligence.Services.Companies;
using FSH.Modules.MarketIntelligence.Services.Insights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalCollectorService : ICodalCollectorService
{
    private readonly ICodalClient _codalClient;
    private readonly MarketIntelligenceDbContext _dbContext;
    private readonly IEnumerable<ICodalDisclosureProcessor> _processors;
    private readonly ILogger<CodalCollectorService> _logger;
    private readonly ICompanyRegistry _companyRegistry;
    private readonly DisclosureInsightPipeline _insightPipeline;
    private readonly CodalProcessingLogService _processingLogService;
    public CodalCollectorService(
    ICodalClient codalClient,
    MarketIntelligenceDbContext dbContext,
    HttpClient httpClient,
    ILogger<CodalCollectorService> logger,
    IConfiguration configuration,
    IEnumerable<ICodalDisclosureProcessor> processors,
    ICompanyRegistry companyRegistry,
    CodalProcessingLogService processingLogService,
    DisclosureInsightPipeline insightPipeline)
    {
        _codalClient = codalClient;
        _dbContext = dbContext;
        _processors = processors;
        _logger = logger;
        _companyRegistry = companyRegistry;
        _insightPipeline = insightPipeline;
        _processingLogService = processingLogService;
        
    }

#pragma warning disable S4144 // Temporary copy; will use ascending persistence order
    public async Task CollectIncrementalAsync(
    CancellationToken cancellationToken = default)
    {
#if DEBUG
        const long debugTracingNo = 1599894;

        Disclosure testDisclosure =
            await _dbContext.Disclosures
                .SingleAsync(
                    x => x.TracingNo == debugTracingNo,
                    cancellationToken);

        var testProcessors = _processors
            .Where(x => x.CanProcess(testDisclosure))
            .ToList();

        foreach (var processor in testProcessors)
        {
            _logger.LogInformation(
                "Incremental DEBUG: {Processor}, TracingNo={TracingNo}",
                processor.GetType().Name,
                testDisclosure.TracingNo);

            await processor.ProcessAsync(
                testDisclosure,
                cancellationToken);
        }

        await _insightPipeline.ProcessAsync(
            testDisclosure,
            cancellationToken);

        return;
#endif
        // آخرین تاریخ ذخیره‌شده به همان فرمت فارسی کدال
        string? lastPublishDateStr = await _dbContext.Disclosures
            .OrderByDescending(x => x.PublishDateTimeRaw)
            .Select(x => x.PublishDateTimeRaw)
            .FirstOrDefaultAsync(cancellationToken);
        _ = _processingLogService;
        if (string.IsNullOrWhiteSpace(lastPublishDateStr))
        {
            throw new InvalidOperationException(
                "No disclosure exists. Run the backfill process first.");
        }

        // فقط برای مقایسه، تاریخ فارسی را به DateTime تبدیل می‌کنیم
        var lastPublishDate = PersianDateHelper.ToGregorian(lastPublishDateStr);

        var pageNumber = 1;
        var reachedLastPublishDate = false;

        /*
         * کدال اطلاعات را از جدید به قدیم برمی‌گرداند.
         * فعلاً آن‌ها را در حافظه نگه می‌داریم.
         */
        var collectedDisclosures = new List<(Disclosure Disclosure, DateTime PublishDateTime)>();
        /*
         * جلوگیری از تکرار TracingNo بین صفحات مختلف.
         * ممکن است هنگام صفحه‌بندی، داده‌های کدال جابه‌جا شوند.
         */
        var collectedTracingNos = new HashSet<long>();

        while (!reachedLastPublishDate)
        {
            var result = await _codalClient.SearchAsync(
                new()
                {
                    PageNumber = pageNumber

                },
                cancellationToken);

            Console.WriteLine($"Reading page {pageNumber}/{result.TotalPages}");

            Console.WriteLine($"Letters count: {result.Letters.Count}");

            if (result.Letters.Count == 0)
            {
                break;
            }

            var pageTracingNos = result.Letters
                .Select(x => x.TracingNo)
                .Distinct()
                .ToList();

            var existingTracingNos = await _dbContext.Disclosures
                .Where(x => pageTracingNos.Contains(x.TracingNo))
                .Select(x => x.TracingNo)
                .ToHashSetAsync(cancellationToken);

            foreach (var letter in result.Letters)
            {
                string? pubRaw = letter.PublishDateTimeRaw;

                if (string.IsNullOrWhiteSpace(pubRaw))
                {
                    throw new InvalidOperationException(
                        $"PublishDateTimeRaw is empty for TracingNo {letter.TracingNo}.");
                }

                /*
                 * تاریخ کدال رشته فارسی است.
                 * برای مقایسه آن را به DateTime تبدیل می‌کنیم.
                 */
                var pub = PersianDateHelper.ToGregorian(pubRaw);
                if (!pub.HasValue)
                {
                    throw new InvalidOperationException(
                        $"PublishDateTimeRaw is invalid for TracingNo {letter.TracingNo}. Value: {pubRaw}");
                }

                /*
                 * این شرط باید قبل از بررسی Duplicate باشد؛
                 * چون رکورد مرزی طبیعتاً قبلاً در دیتابیس وجود دارد.
                 */
                if (pub.Value <= lastPublishDate)
                {
                    reachedLastPublishDate = true;
                    break;
                }

                string? symbol = letter.Symbol;

                if (string.IsNullOrWhiteSpace(symbol)
                    || symbol.Length > 64
                    || existingTracingNos.Contains(letter.TracingNo)
                    || !collectedTracingNos.Add(letter.TracingNo))
                {
                    continue;
                }

                string? sentRaw = letter.SentDateTimeRaw;

                var sent = PersianDateHelper.ToGregorian(sentRaw);

                var (let, rt, ct, ft) = ParseUrlParameters(letter.Url, letter.Title);
                int? RepTypCode =
                rt is >= 0 and <= 9
                   ? 1000000 + rt
                   : null;

                var disclosure = new Disclosure(
                    letter.TracingNo,
                    symbol,
                    letter.CompanyName ?? "",
                    letter.Title ?? "",
                    letter.LetterCode ?? "",
                    sentRaw ?? "",
                    pubRaw,
                    sent,
                    pub.Value,
                    letter.Url ?? "",
                    letter.HasHtml,
                    false,
                    letter.HasExcel,
                    letter.HasPdf,
                    letter.HasXbrl,
                    letter.HasAttachment,
                    letter.AttachmentUrl ?? "",
                    letter.PdfUrl ?? "",
                    letter.ExcelUrl ?? "",
                    letter.XbrlUrl ?? "",
                    letter.TedanUrl ?? "",
                    let,
                    rt,
                    ct,
                    ft,
                    RepTypCode);
                CompanyIdentity? company = 
                    await _companyRegistry.FindBySymbolAsync(symbol, cancellationToken);

                if (company is not null)
                {
                    disclosure.AssignCompanyId(company.CompanyId);
                }

                collectedDisclosures.Add((disclosure, pub.Value));
            }

            if (reachedLastPublishDate)
            {
                break;
            }

            if (pageNumber >= result.TotalPages)
            {
                break;
            }

            pageNumber++;

            await Task.Delay(
                TimeSpan.FromSeconds(7),
                cancellationToken);

        }

        /*
         * اگر به آخرین تاریخ موجود نرسیدیم، چیزی ذخیره نمی‌کنیم.
         * در نتیجه قطعی یا ناقص بودن دریافت باعث ایجاد فاصله نمی‌شود.
         */
        if (!reachedLastPublishDate)
        {
            throw new InvalidOperationException(
                $"The previous publish date '{lastPublishDateStr}' was not reached. " +
                "No disclosure was saved.");
        }

        /*
         * کدال نزولی تحویل داده است.
         * حالا از قدیمی‌ترین به جدیدترین مرتب می‌کنیم.
         */
        var orderedDisclosures = collectedDisclosures
                                .OrderBy(x => x.PublishDateTime)
                                .ThenBy(x => x.Disclosure.TracingNo)
                                .Select(x => x.Disclosure)
                                .ToList();

        Console.WriteLine($"Saving {orderedDisclosures.Count} disclosures in ascending order.");

        foreach (Disclosure disclosure in orderedDisclosures)
        {
            _dbContext.Disclosures.Add(disclosure);

            await _dbContext.SaveChangesAsync(cancellationToken);

            IReadOnlyList<ICodalDisclosureProcessor> processors =
                      _processors.Where(x => x.CanProcess(disclosure))
                                 .ToList();

            foreach (ICodalDisclosureProcessor processor in processors)
            {
                await processor.ProcessAsync(
                    disclosure,
                    cancellationToken);
            }
           
            await _insightPipeline.ProcessAsync(disclosure, cancellationToken);
            
        }
        _logger.LogInformation("Disclosure Reading is completed.");
    }
    public async Task CollectHistoricalRebuildAsync(
    string fromDate,
    string toDate,
    bool resumeFromCheckpoint = true, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromDate);
        ArgumentException.ThrowIfNullOrWhiteSpace(toDate);

        string normalizedFromDate = NormalizeDigits(fromDate.Trim());

        string normalizedToDate = NormalizeDigits(toDate.Trim());

        if (!IsValidPersianDate(normalizedFromDate))
        {
            throw new ArgumentException(
                "From date is not a valid Persian date.",
                nameof(fromDate));
        }

        if (!IsValidPersianDate(normalizedToDate))
        {
            throw new ArgumentException(
                "To date is not a valid Persian date.",
                nameof(toDate));
        }

        if (string.CompareOrdinal(
                normalizedFromDate,
                normalizedToDate) > 0)
        {
            throw new ArgumentException(
                "From date cannot be after to date.",
                nameof(fromDate));
        }
        long progressId = await _processingLogService.CreateProgressAsync(
            normalizedFromDate,
            normalizedToDate,
            cancellationToken);
        string windowFromDate = normalizedFromDate;
        while (string.CompareOrdinal(windowFromDate, normalizedToDate) <= 0)
        {
           string windowToDate = GetPersianMonthEnd(windowFromDate);

           if (string.CompareOrdinal(windowToDate, normalizedToDate) > 0)
           {
               windowToDate = normalizedToDate;
           }
           if (resumeFromCheckpoint)
           {
                bool alreadyCompleted = await _dbContext.CodalProcessingLogs
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.Operation == "Historical"
                            && x.Stage == "Search"
                            && x.Status == "Completed"
                            && x.FromDate == windowFromDate
                            && x.ToDate == windowToDate,
                        cancellationToken);

                if (alreadyCompleted)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation(
                            "Historical checkpoint skipped completed month. FromDate={FromDate}, ToDate={ToDate}",
                            windowFromDate,
                            windowToDate);
                    }

                    windowFromDate = GetNextPersianMonthStart(windowFromDate);
                    continue;
                }
           }
           bool logged = await _processingLogService.TryLogAsync(
              operation: "Historical",
              stage: "Search",
              level: 1,
              status: "Started",
              fromDate: windowFromDate,
              toDate: windowToDate,
              cancellationToken: cancellationToken);

           if (!logged)
            {
                _logger.LogWarning("Unable to persist Historical month completion. FromDate={FromDate}, ToDate={ToDate}",
                      windowFromDate,
                      windowToDate);
            }
           CodalSearchResponse firstPage =
              await SearchHistoricalPageAsync(windowFromDate, windowToDate, 1, cancellationToken)
              ?? throw new InvalidOperationException(
                   $"Historical first page failed. FromDate={windowFromDate}, ToDate={windowToDate}");
           // bool hasFailedPages = false;
            for (int pageNumber = firstPage.TotalPages; pageNumber >= 2; pageNumber--)
            {
              await Task.Delay(TimeSpan.FromSeconds(20), cancellationToken);

              CodalSearchResponse? page = await SearchHistoricalPageAsync(
                    windowFromDate,
                    windowToDate,
                    pageNumber,
                     cancellationToken);
              if (page is null)
                {
                    throw new InvalidOperationException(
                        $"Historical page failed. " +
                        $"FromDate={windowFromDate}, " +
                        $"ToDate={windowToDate}, " +
                        $"Page={pageNumber}");
                }
              await ProcessHistoricalPageAsync(page.Letters, cancellationToken);
              await _processingLogService.UpdateProgressAsync(
                 progressId,
                 windowFromDate,
                 windowToDate,
                 pageNumber,
                 cancellationToken);
              if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Historical rebuild page. FromDate={FromDate}, ToDate={ToDate}, Page={PageNumber}, TotalPages={TotalPages}, Letters={LettersCount}",
                        windowFromDate,
                        windowToDate,
                        pageNumber,
                        firstPage.TotalPages,
                        page.Letters.Count);
                }                
            }
           await ProcessHistoricalPageAsync(firstPage.Letters, cancellationToken);
           await _processingLogService.UpdateProgressAsync(
              progressId,
              windowFromDate,
              windowToDate,
              1,
              cancellationToken);
           if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                   "Historical rebuild page. FromDate={FromDate}, ToDate={ToDate}, Page={PageNumber}, TotalPages={TotalPages}, Letters={LettersCount}",
                   windowFromDate,
                   windowToDate,
                   1,
                   firstPage.TotalPages,
                   firstPage.Letters.Count);
            }
           bool completedLogged = await _processingLogService.TryLogAsync(
                 operation: "Historical",
                 stage: "Search",
                 level: 1,
                 status: "Completed",
                 fromDate: windowFromDate,
                 toDate: windowToDate,
                 cancellationToken: cancellationToken);

           if (!completedLogged)
            {
                _logger.LogWarning("Unable to persist Historical month completion. FromDate={FromDate}, ToDate={ToDate}",
                      windowFromDate,
                      windowToDate);
            }
            windowFromDate = GetNextPersianMonthStart(windowFromDate);
        }
        Dictionary<int, string> parentSymbols = await _dbContext.CompanyMaster
             .AsNoTracking()
             .Where(company => company.IsListed && company.Symbol != null)
             .ToDictionaryAsync(company => company.CompanyId,
                                company => company.Symbol!,
                                cancellationToken);
        List<InvestmentPortfolioPosition> monthlyPositions = await _dbContext.InvestmentPortfolioPositions
             .AsNoTracking()
             .Where(position => position.SourceType == PortfolioSourceType.MonthlyActivity)
             .ToListAsync(cancellationToken);

        int deletedHoldingPeriods = await _dbContext
             .InvestmentPortfolioHoldingPeriods
             .ExecuteDeleteAsync(cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Historical rebuild holding periods cleared. Deleted={DeletedCount}",
                deletedHoldingPeriods);
        }
        var monthlyPortfolios = monthlyPositions
             .Where(position => string.CompareOrdinal(position.PeriodEndDate, "1399/01/31") >= 0)
             .GroupBy(position => new
                  {
                    position.ParentCompanyId,
                    position.PeriodEndDate,
                  })
             .OrderBy(group => group.Key.ParentCompanyId)
             .ThenBy(group => group.Key.PeriodEndDate)
             .ToList();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Historical rebuild holding portfolios prepared. Portfolios={PortfolioCount}",
                monthlyPortfolios.Count);
        }
        InvestmentPortfolioProcessor holdingPortfolioProcessor = _processors
            .OfType<InvestmentPortfolioProcessor>()
            .Single();
        int? previousParentCompanyId = null;
        IReadOnlyList<InvestmentPortfolioPosition>
        previousPortfolio = [];
        foreach (var portfolioGroup in monthlyPortfolios)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int parentCompanyId = portfolioGroup.Key.ParentCompanyId;
            if (!parentSymbols.TryGetValue(parentCompanyId,
                      out string? parentSymbol) ||
                      string.IsNullOrWhiteSpace(parentSymbol))
            {
               throw new InvalidOperationException($"Parent symbol not found. CompanyId={parentCompanyId}");
            }

            if (previousParentCompanyId != parentCompanyId)
            {
                previousPortfolio = [];
                previousParentCompanyId =
                parentCompanyId;
            }

            List<InvestmentPortfolioPosition> currentPortfolio = portfolioGroup.ToList();

            await holdingPortfolioProcessor
                .ApplyHoldingEntryExitAsync(
                    parentSymbol,
                    previousPortfolio,
                    currentPortfolio,
                    portfolioGroup.Key.PeriodEndDate,
                    cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            previousPortfolio = currentPortfolio;
        }
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Historical rebuild holding source loaded. Positions={PositionCount}",
                monthlyPositions.Count);
        }
    }                  
    private async Task ProcessHistoricalPageAsync(
    Collection<CodalLetterDto> letters,
    CancellationToken cancellationToken)
    {
        if (letters.Count == 0)
        {
            return;
        }
        List<CodalLetterDto> orderedWindowLetters =
              letters
                .GroupBy(letter => letter.TracingNo).Select(group => group.First())
                .OrderBy(letter => PersianDateHelper.ToGregorian(letter.PublishDateTimeRaw))
                .ThenBy(letter => letter.TracingNo)
                .ToList();

        long[] tracingNos = orderedWindowLetters
             .Select(letter => letter.TracingNo)
             .ToArray();

        Dictionary<long, Disclosure> existingDisclosures =
             await _dbContext.Disclosures
                .Where(disclosure => EF.Constant(tracingNos).Contains(disclosure.TracingNo))
                .ToDictionaryAsync(disclosure => disclosure.TracingNo, cancellationToken);
        foreach (CodalLetterDto letter in orderedWindowLetters)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(letter.Symbol) ||
                   letter.Symbol.Length > 64)
            {
                continue;
            }
            var (let, rt, ct, ft) = ParseUrlParameters(letter.Url, letter.Title);
            if (rt is null)
            {
                continue;
            }
            int? reportingTypeCode = rt is >= 0 and <= 9
                   ? 1000000 + rt
                   : null;
            long tracingNo = letter.TracingNo;

            existingDisclosures.TryGetValue(tracingNo, out Disclosure? disclosure);
            if (disclosure is null)
            {
                string? sentRaw = letter.SentDateTimeRaw;

                string? publishRaw = letter.PublishDateTimeRaw;

                DateTime? sent = PersianDateHelper.ToGregorian(sentRaw);

                DateTime? published = PersianDateHelper.ToGregorian(publishRaw);

                disclosure =
                    new Disclosure(
                        letter.TracingNo,
                        letter.Symbol,
                        letter.CompanyName ?? string.Empty,
                        letter.Title ?? string.Empty,
                        letter.LetterCode ?? string.Empty,
                        sentRaw ?? string.Empty,
                        publishRaw ?? string.Empty,
                        sent,
                        published,
                        letter.Url ?? string.Empty,
                        letter.HasHtml,
                        false,
                        letter.HasExcel,
                        letter.HasPdf,
                        letter.HasXbrl,
                        letter.HasAttachment,
                        letter.AttachmentUrl ?? string.Empty,
                        letter.PdfUrl ?? string.Empty,
                        letter.ExcelUrl ?? string.Empty,
                        letter.XbrlUrl ?? string.Empty,
                        letter.TedanUrl ?? string.Empty,
                        let,
                        rt,
                        ct,
                        ft,
                        reportingTypeCode);
                CompanyIdentity? company =
                   await _companyRegistry.FindBySymbolAsync(letter.Symbol, cancellationToken);

                if (company is not null)
                {
                    disclosure.AssignCompanyId(company.CompanyId);
                }
                _dbContext.Disclosures.Add(disclosure);

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            if (disclosure.Let is null || disclosure.Rt is null ||
                   disclosure.ReportingTypeCode is null)
            {
                disclosure.Let = let;
                disclosure.Rt = rt;
                disclosure.Ct = ct;
                disclosure.Ft = ft;
                disclosure.ReportingTypeCode = reportingTypeCode;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            List<ICodalDisclosureProcessor> processors =
                _processors
                 .Where(processor => processor.CanProcess(disclosure))
                 .ToList();

            if (processors.Count == 0)
            {
                continue;
            }
            bool processorFailed = false;
            foreach (ICodalDisclosureProcessor processor in processors)
            {
                try
                {
                    if (processor is InvestmentPortfolioProcessor portfolioProcessor)
                    {
                        await portfolioProcessor.ProcessBackfillAsync(
                            disclosure,
                            cancellationToken);
                    }
                    else
                    {
                        await processor.ProcessAsync(
                            disclosure,
                            cancellationToken);
                    }
                }
                catch (HttpRequestException ex)
                {
                    processorFailed = true;
                    bool logged = await _processingLogService.TryLogAsync(
                        operation: "Historical",
                        stage: $"Parse:{processor.GetType().Name}",
                        level: 3,
                        exception: ex,
                        status: "Incomplete",
                        tracingNo: disclosure.TracingNo,
                        cancellationToken: cancellationToken);

                    if (!logged)
                    {
                        throw new InvalidOperationException(
                            $"Unable to persist Historical processor HTTP failure. " +
                            $"TracingNo={disclosure.TracingNo}, " +
                            $"Processor={processor.GetType().Name}",
                            ex);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    processorFailed = true;
                    bool logged = await _processingLogService.TryLogAsync(
                        operation: "Historical",
                        stage: $"Parse:{processor.GetType().Name}",
                        level: 2,
                        exception: ex,
                        status: "Incomplete",
                        tracingNo: disclosure.TracingNo,
                        cancellationToken: cancellationToken);

                    if (!logged)
                    {
                        throw new InvalidOperationException(
                            $"Unable to persist failure. TracingNo={disclosure.TracingNo}",
                            ex);
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    processorFailed = true;
                    bool logged = await _processingLogService.TryLogAsync(
                        operation: "Historical",
                        stage: $"Parse:{processor.GetType().Name}",
                        level: 3,
                        exception: ex,
                        status: "Incomplete",
                        tracingNo: disclosure.TracingNo,
                        cancellationToken: cancellationToken);

                    if (!logged)
                    {
                        throw new InvalidOperationException(
                            $"Failed to persist JSON parsing error. TracingNo={disclosure.TracingNo}",
                            ex);
                    }
                }
                catch (FormatException ex)
                {
                    processorFailed = true;
                    bool logged = await _processingLogService.TryLogAsync(
                       operation: "Historical",
                       stage: $"Parse:{processor.GetType().Name}",
                       level: 3,
                       exception: ex,
                       status: "Incomplete",
                       tracingNo: disclosure.TracingNo,
                       cancellationToken: cancellationToken);

                    if (!logged)
                      {
                        throw new InvalidOperationException(
                        $"Failed to persist formatting error. TracingNo={disclosure.TracingNo}",
                        ex);
                      }
                }
            }
            if (processorFailed)
            {
                continue;
            }
            try
            {
                await _insightPipeline.ProcessAsync(
                    disclosure,
                    cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                bool logged = await _processingLogService.TryLogAsync(
                    operation: "Historical",
                    stage: "Insight",
                    level: 2,
                    exception: ex,
                    status: "Incomplete",
                    tracingNo: disclosure.TracingNo,
                    cancellationToken: cancellationToken);

                if (!logged)
                {
                    throw new InvalidOperationException(
                        $"Unable to persist Insight failure. TracingNo={disclosure.TracingNo}",
                        ex);
                }
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Historical rebuild disclosure. TracingNo={TracingNo}, Exists={Exists}",
                    tracingNo,
                    true);
            }
        }

    }

    public Task CollectSymbolBackfillAsync(
        string symbol,
        string fromDate,
        string toDate,
        CancellationToken cancellationToken = default)
    {
        return CollectSymbolBackfillCoreAsync(
            symbol, fromDate, toDate,
            insightOnly: false,
            cancellationToken);
    }

    public Task CollectInsightSymbolBackfillAsync(
        string symbol,
        string fromDate,
        string toDate,
        CancellationToken cancellationToken = default)
    {
        return CollectSymbolBackfillCoreAsync(
            symbol, fromDate, toDate,
            insightOnly: true,
            cancellationToken);
    }

    private async Task CollectSymbolBackfillCoreAsync(
        string symbol,
        string fromDate,
        string toDate,
        bool insightOnly,
        CancellationToken cancellationToken)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        ArgumentException.ThrowIfNullOrWhiteSpace(fromDate);

        ArgumentException.ThrowIfNullOrWhiteSpace(toDate);

        string normalizedSymbol = symbol.Trim();

        string normalizedFromDate = NormalizeDigits(fromDate.Trim());

        string normalizedToDate = NormalizeDigits(toDate.Trim());

        if (!IsValidPersianDate(normalizedFromDate))
        {
            throw new ArgumentException(
                "From date is not a valid Persian date.",
                nameof(fromDate));
        }

        if (!IsValidPersianDate(normalizedToDate))
        {
            throw new ArgumentException(
                "To date is not a valid Persian date.",
                nameof(toDate));
        }

        if (string.CompareOrdinal(
                normalizedFromDate,
                normalizedToDate) > 0)
        {
            throw new ArgumentException(
                "From date cannot be after to date.",
                nameof(fromDate));
        }

        string searchFromDate = normalizedFromDate;
        string searchToDate = normalizedToDate;
       
        TimeSpan requestDelay = TimeSpan.FromSeconds(5);


        List<CodalLetterDto> letters;

        if (insightOnly)
        {
            letters = await SearchInsightDisclosuresAsync(
                normalizedSymbol,
                normalizedFromDate,
                normalizedToDate,
                cancellationToken);
        }
        else
        {
            letters = [];

            // کل حلقه جست‌وجوی فعلی
            // از string windowFromDate = searchFromDate
            // تا انتهای while بدون تغییر اینجا باقی می‌ماند.
        }


        string windowFromDate = searchFromDate;

        bool isFirstRequest = true;

        while (
            !insightOnly &&
            string.CompareOrdinal(
                windowFromDate,
                searchToDate) <= 0)
        {
            string nextYearDate =
                AddYearsToPersianDate(
                    windowFromDate,
                    1);

            string windowToDate = searchToDate;                

            if (!isFirstRequest)
            {
                await Task.Delay(
                    requestDelay,
                    cancellationToken);
            }
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Searching Codal for {Symbol}, publication window {FromDate} to {ToDate}, page {PageNumber}.",
                    normalizedSymbol,
                    windowFromDate,
                    windowToDate,
                    1);
            }

            CodalSearchResponse firstPage =
                await _codalClient.SearchAsync(
                    new CodalSearchRequest
                    {
                        Symbol = normalizedSymbol,
                        FromDate = windowFromDate,
                        ToDate = windowToDate,
                        PageNumber = 1,
                    },
                    cancellationToken);

            isFirstRequest = false;

            letters.AddRange(firstPage.Letters);

            for (
                int pageNumber = 2;
                pageNumber <= firstPage.TotalPages;
                pageNumber++)
            {
                await Task.Delay(
                    requestDelay,
                    cancellationToken);

                CodalSearchResponse page =
                    await _codalClient.SearchAsync(
                        new CodalSearchRequest
                        {
                            Symbol = normalizedSymbol,
                            FromDate = windowFromDate,
                            ToDate = windowToDate,
                            PageNumber = pageNumber,
                        },
                        cancellationToken);

                letters.AddRange(
                    page.Letters);
            }

            if (
                string.Equals(
                    windowToDate,
                    searchToDate,
                    StringComparison.Ordinal))
            {
                break;
            }

            windowFromDate = windowToDate;
        }

        HashSet<long> processedTracingNos = [];

        int createdCount = 0;
        int processedCount = 0;
        int missingPeriodCount = 0;
        int outsidePeriodCount = 0;

        string parseFromDate = normalizedFromDate;
        foreach (
            CodalLetterDto letter in
            letters.OrderBy(letter =>
                PersianDateHelper.ToGregorian(
                    letter.PublishDateTimeRaw)))
        {
            if (
                string.IsNullOrWhiteSpace(
                    letter.Symbol) ||
                letter.Symbol.Length > 64 ||
                !processedTracingNos.Add(
                    letter.TracingNo))
            {
                continue;
            }

            

            var (let, rt, ct, ft) =
                ParseUrlParameters(
                    letter.Url, letter.Title);

            if (rt is null)
            {
                continue;
            }

            int? reportingTypeCode =
                rt is >= 0 and <= 9
                   ? 1000000 + rt
                   : null;

            Disclosure? disclosure =
                await _dbContext.Disclosures
                    .SingleOrDefaultAsync(
                        item =>
                            item.TracingNo ==
                            letter.TracingNo,
                        cancellationToken);

            if (disclosure is null)
            {
                string? sentRaw = letter.SentDateTimeRaw;

                string? publishRaw = letter.PublishDateTimeRaw;

                DateTime? sent = PersianDateHelper.ToGregorian(sentRaw);

                DateTime? published = PersianDateHelper.ToGregorian(publishRaw);

                disclosure =
                    new Disclosure(
                        letter.TracingNo,
                        letter.Symbol,
                        letter.CompanyName ?? string.Empty,
                        letter.Title ?? string.Empty,
                        letter.LetterCode ?? string.Empty,
                        sentRaw ?? string.Empty,
                        publishRaw ?? string.Empty,
                        sent,
                        published,
                        letter.Url ?? string.Empty,
                        letter.HasHtml,
                        false,
                        letter.HasExcel,
                        letter.HasPdf,
                        letter.HasXbrl,
                        letter.HasAttachment,
                        letter.AttachmentUrl ?? string.Empty,
                        letter.PdfUrl ?? string.Empty,
                        letter.ExcelUrl ?? string.Empty,
                        letter.XbrlUrl ?? string.Empty,
                        letter.TedanUrl ?? string.Empty,
                        let,
                        rt,
                        ct,
                        ft,
                        reportingTypeCode);

                CompanyIdentity? company = await _companyRegistry.FindBySymbolAsync(letter.Symbol, cancellationToken);
                if (company is not null)
                {
                    disclosure.AssignCompanyId(company.CompanyId);
                }
                _dbContext.Disclosures.Add(disclosure);

                await _dbContext.SaveChangesAsync(cancellationToken);

                createdCount++;
            }
            else if (disclosure.Let is null ||
                     disclosure.Rt is null ||
                     disclosure.ReportingTypeCode is null)
            {
                disclosure.Let = let;
                disclosure.Rt = rt;
                disclosure.Ct = ct;
                disclosure.Ft = ft;
                disclosure.ReportingTypeCode = reportingTypeCode;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            List<ICodalDisclosureProcessor> processors = _processors.Where(x => x.CanProcess(disclosure)).ToList();

            if (processors.Count == 0)
            {
                continue;
            }

            string? periodDate = ExtractPeriodDateFromTitle(letter.Title);

            if (periodDate is null)
            {
                missingPeriodCount++;
                continue;
            }          

            await Task.Delay(
                requestDelay,
                cancellationToken);

            foreach (ICodalDisclosureProcessor processor in processors)
            {
                if (processor is InvestmentPortfolioProcessor portfolioProcessor)
                {
                    await portfolioProcessor.ProcessBackfillAsync(
                        disclosure,
                        cancellationToken);
                }
                else
                {
                    await processor.ProcessAsync(
                        disclosure,
                        cancellationToken);
                }

                processedCount++;
            }
            await _insightPipeline.ProcessAsync(disclosure, cancellationToken);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Targeted Codal backfill completed for {Symbol}. " +
                    "Requested period: {FromDate} to {ToDate}. " +
                    "Search publication window: {SearchFromDate} to {SearchToDate}. " +
                    "Created: {CreatedCount}, processed: {ProcessedCount}, " +
                    "missing period in title: {MissingPeriodCount}, " +
                    "outside requested period: {OutsidePeriodCount}.",
               normalizedSymbol,
               normalizedFromDate,
               normalizedToDate,
               searchFromDate,
               searchToDate,
               createdCount,
               processedCount,
               missingPeriodCount,
               outsidePeriodCount);
        }

    }

    private async Task<List<CodalLetterDto>> SearchInsightDisclosuresAsync(
        string symbol,
        string fromDate,
        string toDate,
        CancellationToken cancellationToken)
    {
        var results = new Dictionary<long, CodalLetterDto>();

        foreach (int category in new[] { 3, 1 })
        {
            int pageNumber = 1;

            while (true)
            {
                CodalSearchResponse response =
                    await _codalClient.SearchAsync(
                        new CodalSearchRequest
                        {
                            Symbol = symbol,
                            Category = category,
                            FromDate = fromDate,
                            ToDate = toDate,
                            PageNumber = pageNumber
                        },
                        cancellationToken);

                foreach (CodalLetterDto letter in response.Letters)
                {
                    var (let, rt, _, _) =
                        ParseUrlParameters(letter.Url, letter.Title);

                    bool selected =
                            (category == 3 && let == 58) ||
                            (category == 3 && rt == 2 && let == 8) ||
                            (category == 1 && rt == 2 && let == 6);

                    if (selected)
                    {
                        results.TryAdd(letter.TracingNo, letter);
                    }
                }

                if (pageNumber >= response.TotalPages ||
                    response.Letters.Count == 0)
                {
                    break;
                }

                pageNumber++;

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    cancellationToken);
            }
        }

        return results.Values
            .OrderBy(x =>
                PersianDateHelper.ToGregorian(x.PublishDateTimeRaw))
            .ThenBy(x => x.TracingNo)
            .ToList();
    }

    private static (short? let, byte? rt, byte? ct, short? ft) ParseUrlParameters(string? url, string? title)
    {
        short? let =
            title?.Contains(
                "گزارش فعالیت ماهانه",
                StringComparison.Ordinal) == true
                ? (short)58
                : null;

        if (string.IsNullOrWhiteSpace(url))
            return (let, null, null, null);

#pragma warning disable S1075
        var query = System.Web.HttpUtility.ParseQueryString(
            new Uri("https://dummy.local" + url).Query);
#pragma warning restore S1075

        if (let is null)
        {
            let = short.TryParse(
                query["let"],
                out var l)
                ? l
                : null;
        }

        byte? rt =
            byte.TryParse(
                query["rt"],
                out var r)
                ? r
                : null;

        byte? ct =
            byte.TryParse(
                query["ct"],
                out var c)
                ? c
                : null;

        short? ft =
            short.TryParse(
                query["ft"],
                out var f)
                ? f
                : null;

        if (rt is null &&
            int.TryParse(
                query["ReportingType"],
                out var reportingType) &&
            reportingType == 1000002)
        {
            rt = 2;
        }

        return (let, rt, ct, ft);
    }
    public async Task<bool> ParsePendingDisclosuresAsync(
    CancellationToken cancellationToken = default)
    {
        const int batchSize = 20;
        const int maxDisclosuresPerRun = 20;
        TimeSpan delayBetweenRequests = TimeSpan.FromSeconds(5);

        DateTime lastPublishDate = DateTime.MinValue;
        long lastTracingNo = long.MinValue;

        var pageNumber = 1;
        var firstBatch = true;
        var processedCount = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int remaining = maxDisclosuresPerRun - processedCount;
            if (remaining <= 0)
            {
                break;
            }
                        
            IQueryable<Disclosure> query = _dbContext.Disclosures
                 .Where(x => x.SalesParseStatus == DisclosureParseStatus.Pending &&
                             x.Let == 58 &&
                             x.Symbol == "وسپه");
            if (!firstBatch)
            {
                DateTime cursorDate = lastPublishDate;
                long cursorTracingNo = lastTracingNo;

                query = query.Where(x => (x.PublishDateTime ?? DateTime.MinValue) > cursorDate ||
                                         ((x.PublishDateTime ?? DateTime.MinValue) == cursorDate &&
                                           x.TracingNo > cursorTracingNo)
                                   );
            }

            List<Disclosure> disclosures =
                await query
                    .OrderBy(x =>
                        x.PublishDateTime ?? DateTime.MinValue)
                    .ThenBy(x => x.TracingNo)
                    .Take(Math.Min(batchSize, remaining))
                    .ToListAsync(cancellationToken);

            if (disclosures.Count == 0)
            {
                break;
            }
            
            foreach (Disclosure disclosure in disclosures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                List<ICodalDisclosureProcessor> processors =
                    _processors
                        .Where(x => x.CanProcess(disclosure))
                        .ToList();

                bool processorFailed = false;

                if (processors.Count == 0)
                {
                    disclosure.SalesParseStatus = DisclosureParseStatus.Skipped;

                    disclosure.SalesParsedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    continue;
                }


                foreach (ICodalDisclosureProcessor processor in processors)
                {
                    try
                    {
                        if (processor is InvestmentPortfolioProcessor portfolioProcessor)
                        {
                            await portfolioProcessor.ProcessBackfillAsync(
                                disclosure,
                                cancellationToken);
                        }
                        else
                        {
                            await processor.ProcessAsync(
                                disclosure,
                                cancellationToken);
                        }

                        await Task.Delay(
                            delayBetweenRequests,
                            cancellationToken);
                    }
                    catch (OperationCanceledException)
                        when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (HttpRequestException ex)
                        when (ex.StatusCode ==
                              HttpStatusCode.TooManyRequests)
                    {
                        throw;
                    }
                    catch (HttpRequestException)
                    {
                        return false;
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                    catch (InvalidOperationException ex)
                    {
                        processorFailed = true;
                        _logger.LogError(
                            ex,
                            "Processor {Processor} failed for Disclosure {TracingNo}.",
                            processor.GetType().Name,
                            disclosure.TracingNo);
                    }
                    catch (FormatException ex)
                    {
                        processorFailed = true;
                        _logger.LogError(
                            ex,
                            "Processor {Processor} failed for Disclosure {TracingNo}.",
                            processor.GetType().Name,
                            disclosure.TracingNo);
                    }
                    catch (JsonException ex)
                    {
                        processorFailed = true;
                        _logger.LogError(
                            ex,
                            "Processor {Processor} failed for Disclosure {TracingNo}.",
                            processor.GetType().Name,
                            disclosure.TracingNo);
                    }
                }
                if (processorFailed)
                {
                    disclosure.SalesParseStatus = DisclosureParseStatus.Failed;
                    disclosure.SalesParsedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                
                if (!processorFailed)
                {
                    await _insightPipeline.ProcessAsync(disclosure, cancellationToken);
                }
            }
            processedCount += disclosures.Count;
            Disclosure lastDisclosure = disclosures[^1];

            lastPublishDate = lastDisclosure.PublishDateTime ?? DateTime.MinValue;
            lastTracingNo = lastDisclosure.TracingNo;
            firstBatch = false;
            _dbContext.ChangeTracker.Clear();
            Console.WriteLine($"Disclosure scan batch saved: {pageNumber}");
            pageNumber++;
        }
        _logger.LogInformation("End Of Parse Pending Disclosures.");
        return processedCount >= maxDisclosuresPerRun;
    }
    public async Task CollectBackfillChunkAsync(int startPage, int endPage, CancellationToken cancellationToken = default)
    {
        var lastPublishDateStr = await _dbContext.Disclosures
            .OrderByDescending(x => x.PublishDateTimeRaw)
            .Select(x => x.PublishDateTimeRaw)
            .FirstOrDefaultAsync(cancellationToken);

        // فعلاً برای BackFill یک سال قبل
        

        var definitions = CodalDefinitionsProvider.Load();

        int pageNumber = startPage;
        int totalPagesInChunk = startPage - endPage + 1;
        var stop = false;

        while (!stop)
        {
            var result = await _codalClient.SearchAsync(
                new()
                {                            // 1000000:تولیدی 
                                             // 1000001:ساختمانی     
                                             // 1000002:سرمایه گذاری  
                    PageNumber = pageNumber, // 1000003:بانک            
                                             // 1000004:لیزینگ   
                                             // 1000005:خدماتی 
                                                              // 1000006:بیمه               
                                                              // 1000007:حمل ونقل دریایی
                },                           // 1000008:کشاورزی          
                cancellationToken);          // 1000009:تامین سرمایه         

            if (_logger.IsEnabled(LogLevel.Information))
            {
                int processedPages =
                    startPage - pageNumber + 1;

                double progressPercent = processedPages * 100d / totalPagesInChunk;

                _logger.LogInformation(
                    "Backfill progress: {Progress:F1}% | Page {CurrentPage}/{TotalPages} | Letters: {LettersCount} | Chunk: {StartPage}->{EndPage}",
                    progressPercent,
                    pageNumber,
                    result.TotalPages,
                    result.Letters.Count,
                    startPage,
                    endPage);
            }
            
            // TracingNo های این صفحه
            var tracingNos = result.Letters
                .Select(x => x.TracingNo)
                .ToList();


            // رکوردهایی که قبلاً ذخیره شده‌اند
            var existingTracingNos = await _dbContext.Disclosures
                .Where(x => tracingNos.Contains(x.TracingNo))
                .Select(x => x.TracingNo)
                .ToHashSetAsync(cancellationToken);


            // جلوگیری از Duplicate داخل همین صفحه
            var pageTracingNos = new HashSet<long>();


            var disclosures = new List<Disclosure>();

            DateTime? currentPubDate = null;


            foreach (var letter in result.Letters)
            {
                if (string.IsNullOrWhiteSpace(letter.Symbol)
                    || letter.Symbol.Length > 64
                    || existingTracingNos.Contains(letter.TracingNo)
                    || !pageTracingNos.Add(letter.TracingNo))
                {
                    continue;
                }

                string? sentRaw = letter.SentDateTimeRaw;
                string? pubRaw = letter.PublishDateTimeRaw;


                var sent = PersianDateHelper.ToGregorian(sentRaw);
                var pub = PersianDateHelper.ToGregorian(pubRaw);


                currentPubDate = pub;
                                            
                var (let, rt, ct, ft) = ParseUrlParameters(letter.Url, letter.Title);
                if (rt is null)
                {
                    continue;
                }
                int? RepTypCode =
                rt is >= 0 and <= 9
                   ? 1000000 + rt
                   : null;

                var disclosure = new Disclosure(
                    letter.TracingNo,
                    letter.Symbol ?? "",
                    letter.CompanyName ?? "",
                    letter.Title ?? "",
                    letter.LetterCode ?? "",
                    sentRaw ?? "",
                    pubRaw ?? "",
                    sent,
                    pub,
                    letter.Url ?? "",
                    letter.HasHtml,
                    false,
                    letter.HasExcel,
                    letter.HasPdf,
                    letter.HasXbrl,
                    letter.HasAttachment,
                    letter.AttachmentUrl ?? "",  // AttachmentUrl
                    letter.PdfUrl ?? "",  // PdfUrl
                    letter.ExcelUrl ?? "",  // ExcelUrl
                    letter.XbrlUrl ?? "",  // XbrlUrl
                    letter.TedanUrl ?? "",  // TedanUrl
                    let,  // Let
                    rt,  // Rt
                    ct,  // Ct
                    ft, RepTypCode); // Ft
                disclosures.Add(disclosure);
                _dbContext.Disclosures.Add(disclosure);
                await _dbContext.SaveChangesAsync(cancellationToken);
                IReadOnlyList<ICodalDisclosureProcessor> processors =
                       _processors
                          .Where(x => x.CanProcess(disclosure))
                          .ToList();

                foreach (ICodalDisclosureProcessor processor in processors)
                {
                    try
                    {
                        if (processor is InvestmentPortfolioProcessor portfolioProcessor)
                        {
                            await portfolioProcessor.ProcessBackfillAsync(
                                disclosure,
                                cancellationToken);
                        }
                        else
                        {
                            await processor.ProcessAsync(
                                disclosure,
                                cancellationToken);
                        }
                    }
                    
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(
                            ex,
                            "Processor HTTP error. TracingNo: {TracingNo}, Processor: {Processor}",
                            disclosure.TracingNo,
                            processor.GetType().Name);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError(
                            ex,
                            "Processor error. TracingNo: {TracingNo}, Processor: {Processor}",
                            disclosure.TracingNo,
                            processor.GetType().Name);
                    }
                }

                try
                {
                    await _insightPipeline.ProcessAsync(
                        disclosure,
                        cancellationToken);
                }
                
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(
                        ex,
                        "Insight pipeline failed. TracingNo: {TracingNo}",
                        disclosure.TracingNo);
                }
            }
            // ذخیره یکجای صفحه

            // اگر به رکوردهای قدیمی رسیدیم، توقف
            if (stop)
                break;

            if (pageNumber <= endPage)
            {
                break;
            }

            pageNumber--;


            var delay = result.TotalPages > 10
                ? TimeSpan.FromSeconds(5)
                : TimeSpan.FromSeconds(0.2);


            await Task.Delay(delay, cancellationToken);
        }
        _logger.LogInformation("Backfill process completed.");

    }
    private static string? ExtractPeriodDateFromTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        string normalizedTitle =
            NormalizeDigits(title);

        const int dateLength = 10;

        string? lastValidDate = null;

        for (
            int index = 0;
            index <=
                normalizedTitle.Length -
                dateLength;
            index++)
        {
            ReadOnlySpan<char> candidate =
                normalizedTitle.AsSpan(
                    index,
                    dateLength);

            if (
                candidate[4] != '/' ||
                candidate[7] != '/')
            {
                continue;
            }

            if (
                !int.TryParse(
                    candidate[..4],
                    out int year) ||
                !int.TryParse(
                    candidate.Slice(5, 2),
                    out int month) ||
                !int.TryParse(
                    candidate.Slice(8, 2),
                    out int day))
            {
                continue;
            }

            if (
                year is < 1300 or > 1600 ||
                month is < 1 or > 12 ||
                day is < 1 or > 31)
            {
                continue;
            }

            string possibleDate =
                candidate.ToString();

            if (IsValidPersianDate(possibleDate))
            {
                lastValidDate =
                    possibleDate;
            }
        }

        return lastValidDate;
    }

    private static string NormalizeDigits(
        string value)
    {
        char[] characters =
            value.ToCharArray();

        for (
            int index = 0;
            index < characters.Length;
            index++)
        {
            char character =
                characters[index];

            if (
                character is >= '۰' and <= '۹')
            {
                characters[index] =
                    (char)(
                        '0' +
                        character -
                        '۰');
            }
            else if (
                character is >= '٠' and <= '٩')
            {
                characters[index] =
                    (char)(
                        '0' +
                        character -
                        '٠');
            }
        }

        return new string(characters);
    }
    private static bool IsValidPersianDate(
    string value)
    {
        if (
            value.Length != 10 ||
            value[4] != '/' ||
            value[7] != '/')
        {
            return false;
        }

        if (
            !int.TryParse(
                value.AsSpan(0, 4),
                out int year) ||
            !int.TryParse(
                value.AsSpan(5, 2),
                out int month) ||
            !int.TryParse(
                value.AsSpan(8, 2),
                out int day))
        {
            return false;
        }

        int maximumDay =
            month is >= 1 and <= 6
                ? 31
                : 30;

        return
            year is >= 1200 and <= 1600 &&
            month is >= 1 and <= 12 &&
            day >= 1 &&
            day <= maximumDay;
    }
    private static string GetPersianMonthEnd(
    string value)
    {
        int year =
            int.Parse(
                value.AsSpan(0, 4),
                CultureInfo.InvariantCulture);

        int month =
            int.Parse(
                value.AsSpan(5, 2),
                CultureInfo.InvariantCulture);

        var calendar =
            new PersianCalendar();

        int day =
            calendar.GetDaysInMonth(
                year,
                month);

        return string.Create(CultureInfo.InvariantCulture,
                             $"{year:0000}/{month:00}/{day:00}");
    }
    private static string GetNextPersianMonthStart(
    string value)
    {
        int year =
            int.Parse(
                value.AsSpan(0, 4),
                CultureInfo.InvariantCulture);

        int month =
            int.Parse(
                value.AsSpan(5, 2),
                CultureInfo.InvariantCulture);

        if (month == 12)
        {
            year++;
            month = 1;
        }
        else
        {
            month++;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{year:0000}/{month:00}/01");
    }
    private static string AddYearsToPersianDate(
        string value,
        int years)
    {
        _ = int.TryParse(
            value.AsSpan(0, 4),
            out int year);

        string shiftedYear =
            (year + years).ToString(
                "0000",
                CultureInfo.InvariantCulture);

        return shiftedYear + value[4..];
    }
    private async Task<CodalSearchResponse?> SearchHistoricalPageAsync(
    string fromDate,
    string toDate,
    int pageNumber,
    CancellationToken cancellationToken)
    {
        try
        {
            return await _codalClient.SearchAsync(
                new CodalSearchRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    PageNumber = pageNumber
                },
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            bool logged = await _processingLogService.TryLogAsync(
                operation: "Historical",
                stage: "Search",
                level: 3,
                exception: ex,
                status: "Incomplete",
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: pageNumber,
                attemptCount: 4,
                cancellationToken: cancellationToken);

            if (!logged)
            {
                throw new InvalidOperationException(
                    $"Failed to persist Historical search failure. Page={pageNumber}, FromDate={fromDate}, ToDate={toDate}",
                    ex);
            }

            _logger.LogError(
                ex,
                "Historical search failed after retries. FromDate={FromDate}, ToDate={ToDate}, Page={PageNumber}",
                fromDate,
                toDate,
                pageNumber);

            return null;
        }
    }
}

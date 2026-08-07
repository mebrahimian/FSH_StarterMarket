
using FSH.Framework.Shared.Dates;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using static FSH.Modules.MarketIntelligence.Contracts.Authorization.MarketIntelligencePermissions;
namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalCollectorService : ICodalCollectorService
{
    private readonly ICodalClient _codalClient;
    private readonly MarketIntelligenceDbContext _dbContext;
    private readonly IEnumerable<ICodalDisclosureProcessor> _processors;
    private readonly ILogger<CodalCollectorService> _logger;

    public CodalCollectorService(
    ICodalClient codalClient,
    MarketIntelligenceDbContext dbContext,
    HttpClient httpClient,
    ILogger<CodalCollectorService> logger,
    IEnumerable<ICodalDisclosureProcessor> processors)
    {
        _codalClient = codalClient;
        _dbContext = dbContext;
        _processors = processors;
        _logger = logger;
    }

#pragma warning disable S4144 // Temporary copy; will use ascending persistence order
    public async Task CollectIncrementalAsync(
    CancellationToken cancellationToken = default)
    {
        // آخرین تاریخ ذخیره‌شده به همان فرمت فارسی کدال
        string? lastPublishDateStr = await _dbContext.Disclosures
            .OrderByDescending(x => x.PublishDateTimeRaw)
            .Select(x => x.PublishDateTimeRaw)
            .FirstOrDefaultAsync(cancellationToken);

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

                var (let, rt, ct, ft) = ParseUrlParameters(letter.Url);
                int? RepTypCode = rt switch
                {
                    0 => 1000000,
                    1 => 1000001,
                    2 => 1000002,
                    3 => 1000003,
                    4 => 1000004,
                    5 => 1000005,
                    6 => 1000006,
                    7 => 1000007,
                    8 => 1000008,
                    9 => 1000009,
                    _ => null
                };

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

        Console.WriteLine(
            $"Saving {orderedDisclosures.Count} disclosures in ascending order.");




        foreach (Disclosure disclosure in orderedDisclosures)
        {
            _dbContext.Disclosures.Add(disclosure);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var processor = _processors
                .SingleOrDefault(x => x.CanProcess(disclosure));

            if (processor is not null)
            {
                await processor.ProcessAsync(
                    disclosure,
                    cancellationToken);
            }
        }
        _logger.LogInformation("Disclosure Reading is completed.");
    }

    public async Task CollectBackfillAsync(CancellationToken cancellationToken = default)
    {
        var lastPublishDateStr = await _dbContext.Disclosures
            .OrderByDescending(x => x.PublishDateTimeRaw)
            .Select(x => x.PublishDateTimeRaw)
            .FirstOrDefaultAsync(cancellationToken);

        // فعلاً برای BackFill یک سال قبل
        lastPublishDateStr = PersianDateHelper.ToPersian(DateTime.Now.AddYears(-1));

        var lastPublishDate = PersianDateHelper.ToGregorian(lastPublishDateStr);

        var definitions = CodalDefinitionsProvider.Load();

        var pageNumber = 1;
        var stop = false;

        while (!stop)
        {
            var result = await _codalClient.SearchAsync(
                new()
                {                            // 1000000:تولیدی 
                                             // 1000001:ساختمانی     
                                             // 1000002:سرمایه گذاری  
                    PageNumber = pageNumber ,// 1000003:بانک            
                                             // 1000004:لیزینگ   
                                             // 1000005:خدماتی 
                                             // 1000006:بیمه               
                                             // 1000007:حمل ونقل دریایی
                },                           // 1000008:کشاورزی          
                cancellationToken);          // 1000009:تامین سرمایه         


            Console.WriteLine(
                $"Reading page {pageNumber}/{result.TotalPages}");

            Console.WriteLine(
                $"Letters count: {result.Letters.Count}");


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


                // هنوز به اطلاعات قدیمی نرسیدیم
                if (lastPublishDate >= pub)
                {
                    stop = true;
                    break;
                }
                var (let, rt, ct, ft) = ParseUrlParameters(letter.Url);
                if (rt is null) 
                {
                    continue;
                }
                int? RepTypCode = rt switch
                {
                    0 => 1000000,
                    1 => 1000001,
                    2 => 1000002,
                    3 => 1000003,
                    4 => 1000004,
                    5 => 1000005,
                    6 => 1000006,
                    7 => 1000007,
                    8 => 1000008,
                    9 => 1000009,
                    _ => null
                };
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
                var processor = _processors.SingleOrDefault(x => x.CanProcess(disclosure));
                if (processor is not null)
                {
                    await processor.ProcessAsync(disclosure, cancellationToken);
                }

            }
            // ذخیره یکجای صفحه

            // اگر به رکوردهای قدیمی رسیدیم، توقف
            if (stop)
                break;


            pageNumber++;


            var delay = result.TotalPages > 10
                ? TimeSpan.FromSeconds(5)
                : TimeSpan.FromSeconds(0.2);


            await Task.Delay(delay, cancellationToken);
        }
        _logger.LogInformation("Backfill process completed.");

    }
    private static (short? let, byte? rt, byte? ct, short? ft) ParseUrlParameters(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (null, null, null, null);
#pragma warning disable S1075
        var query = System.Web.HttpUtility.ParseQueryString(new Uri("https://dummy.local" + url).Query);
#pragma warning restore S1075
        short? let = short.TryParse(query["let"], out var l) ? l : null;
        byte? rt = byte.TryParse(query["rt"], out var r) ? r : null;
        byte? ct = byte.TryParse(query["ct"], out var c) ? c : null;
        short? ft = short.TryParse(query["ft"], out var f) ? f : null;
        if (rt is null && 
            int.TryParse(query["ReportingType"], out var reportingType) && 
            reportingType == 1000002)  rt = 2;
        return (let, rt, ct, ft);
    }
    public async Task ParsePendingDisclosuresAsync(
    CancellationToken cancellationToken = default)
    {
        const int batchSize = 20;

        TimeSpan delayBetweenRequests = TimeSpan.FromSeconds(5);

        var definitions = CodalDefinitionsProvider.Load();
        byte[] supportedReportTypes = definitions.MonthlyActivities.Keys.ToArray();
        var pageNumber = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Disclosure> disclosures =
                await _dbContext.Disclosures
                      .Where(x => (x.Let == 58 || (x.Rt == 2 && x.Let == 8)) &&
                                  x.Rt.HasValue &&
                                  supportedReportTypes.Contains(x.Rt.Value) &&
                                  x.SalesParseStatus == DisclosureParseStatus.Pending
                            )
                      .OrderBy(x => x.PublishDateTime ?? DateTime.MinValue)
                      .ThenBy(x => x.Id)
                      .Take(batchSize)
                      .ToListAsync(cancellationToken);

            if (disclosures.Count == 0)
            {
                break;
            }

            foreach (Disclosure disclosure in disclosures)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ICodalDisclosureProcessor? processor =
                    _processors.SingleOrDefault(
                        x => x.CanProcess(disclosure));

                if (processor is null)
                {
                    throw new InvalidOperationException(
                        $"No processor was found for disclosure " +
                        $"{disclosure.TracingNo}.");
                }

                try
                {
                    await processor.ProcessAsync(
                        disclosure,
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
                    // کدال کد امنیتی درخواست کرده است.
                    // پردازش متوقف می‌شود و رکورد Pending باقی می‌ماند.
                    throw;
                }
                catch (HttpRequestException)
                {
                    // خطای موقت شبکه؛ رکورد Pending باقی بماند.
                    // برای جلوگیری از انتخاب دوباره همین رکورد
                    // در حلقه جاری، کل عملیات متوقف می‌شود.
                    return;
                }
                catch (IOException)
                {
                    // قطع ارتباط هنگام خواندن پاسخ کدال.
                    // رکورد Pending باقی می‌ماند.
                    return;
                }
                catch (InvalidOperationException)
                {
                    disclosure.SalesParseStatus = DisclosureParseStatus.Failed;

                    disclosure.SalesParsedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }
                catch (FormatException)
                {
                    disclosure.SalesParseStatus = DisclosureParseStatus.Failed;

                    disclosure.SalesParsedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }
                catch (JsonException)
                {
                    disclosure.SalesParseStatus = DisclosureParseStatus.Failed;

                    disclosure.SalesParsedAt = DateTime.UtcNow;

                    await _dbContext.SaveChangesAsync(
                        cancellationToken);
                }

                await Task.Delay(
                    delayBetweenRequests,
                    cancellationToken);
            }

            _dbContext.ChangeTracker.Clear();
            Console.WriteLine($"Page Saved: {pageNumber}");
            pageNumber++;
        }
        _logger.LogInformation("End Of Parse Pending Disclosures.");
    }
}

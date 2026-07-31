
using FSH.Framework.Shared.Dates;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Domain.Enums;
using FSH.Modules.MarketIntelligence.Services.Codal.Configuration;
using FSH.Modules.MarketIntelligence.Services.Codal.Interfaces;
using FSH.Modules.MarketIntelligence.Services.Codal.Processors;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using static FSH.Modules.MarketIntelligence.Contracts.Authorization.MarketIntelligencePermissions;
using System.Text.Json;
namespace FSH.Modules.MarketIntelligence.Services.Codal;

public sealed class CodalCollectorService : ICodalCollectorService
{
    private readonly ICodalClient _codalClient;
    private readonly MarketIntelligenceDbContext _dbContext;
    private readonly IEnumerable<ICodalDisclosureProcessor> _processors;

    public CodalCollectorService(
    ICodalClient codalClient,
    MarketIntelligenceDbContext dbContext,
    HttpClient httpClient,
    IEnumerable<ICodalDisclosureProcessor> processors)
    {
        _codalClient = codalClient;
        _dbContext = dbContext;
        _processors = processors;
    }


    public async Task CollectAsync2(CancellationToken cancellationToken = default)
    {
        var lastPublishDateStr = await _dbContext.Disclosures
            .OrderByDescending(x => x.PublishDateTimeRaw)
            .Select(x => x.PublishDateTimeRaw)
            .FirstOrDefaultAsync(cancellationToken);

        // فعلاً برای BackFill یک سال قبل
        lastPublishDateStr = PersianDateHelper.ToPersian(DateTime.Now.AddYears(-5));

        var lastPublishDate = PersianDateHelper.ToGregorian(lastPublishDateStr);

        var definitions = CodalDefinitionsProvider.Load();

        var pageNumber = 1500;
        var stop = false;

        while (!stop)
        {
            var result = await _codalClient.SearchAsync(
                new()
                {
                    // شرطهای خواندن کدال مثلا category=3 ; let58 ;,,,,,
                    // در اینجا فقط شماره صفحه ملاک است
                    PageNumber = pageNumber,
                    Category = 3

                },
                cancellationToken);


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
                    ft); // Ft
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


            pageNumber--;


            var delay = result.TotalPages > 10
                ? TimeSpan.FromSeconds(2)
                : TimeSpan.FromSeconds(0.2);


            await Task.Delay(delay, cancellationToken);
        }
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

        return (let, rt, ct, ft);
    }
    public async Task ParsePendingDisclosuresAsync(
    CancellationToken cancellationToken = default)
    {
        const int batchSize = 20;

        TimeSpan delayBetweenRequests = TimeSpan.FromSeconds(1);

        var definitions = CodalDefinitionsProvider.Load();
        byte[] supportedReportTypes = definitions.MonthlyActivities.Keys.ToArray();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<Disclosure> disclosures =
                await _dbContext.Disclosures
                      .Where(x => x.Let == 58 &&
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
        }
    }
}

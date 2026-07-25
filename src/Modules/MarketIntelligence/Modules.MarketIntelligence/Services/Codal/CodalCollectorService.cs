
using FSH.Framework.Shared.Dates;
using FSH.Modules.MarketIntelligence.Data;
using FSH.Modules.MarketIntelligence.Domain;
using FSH.Modules.MarketIntelligence.Services.Codal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Globalization;
using static FSH.Modules.MarketIntelligence.Contracts.Authorization.MarketIntelligencePermissions;

namespace FSH.Modules.MarketIntelligence.Services.Codal;


public sealed class CodalCollectorService : ICodalCollectorService
{
    private readonly ICodalClient _codalClient;
    private readonly MarketIntelligenceDbContext _dbContext;
    
    public CodalCollectorService(
    ICodalClient codalClient,
    
    MarketIntelligenceDbContext dbContext)
    {
        _codalClient = codalClient;
        _dbContext = dbContext;
        
    }
    
    public async Task CollectAsync(
    CancellationToken cancellationToken = default)
    {
        var lastPublishDateStr = await _dbContext.Disclosures
            .OrderByDescending(x => x.PublishDateTimeRaw)
            .Select(x => x.PublishDateTimeRaw)
            .FirstOrDefaultAsync(cancellationToken);

        
            lastPublishDateStr = PersianDateHelper.ToPersian(DateTime.Now.AddYears(-1));
            
        
        var lastPublishDate = PersianDateHelper.ToGregorian(lastPublishDateStr);
        
       
        var pageNumber = 1;
        var stop = false;
        int NumberRead = 0;
        DateTime? CurrentPubDate = DateTime.Now;

        while (!stop)
        {
            
            var result = await _codalClient.SearchAsync(
                new()
                {
                    PageNumber = pageNumber
                },
                cancellationToken);

                        
            foreach (var letter in result.Letters)
            {
                if (string.IsNullOrWhiteSpace(letter.Symbol) || letter.Symbol.Length > 64)
                {
                    continue;
                }
                string? sentRaw = letter.SentDateTimeRaw;
                string? pubRaw = letter.PublishDateTimeRaw;
                var sent = PersianDateHelper.ToGregorian(sentRaw);
                var pub = PersianDateHelper.ToGregorian(pubRaw);
                CurrentPubDate = pub;
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
                    letter.HasHtml,
                    false,
                    letter.Url ?? "",
                    letter.HasExcel,
                    false,
                    false,
                    false,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null);

                
                if (lastPublishDate < pub) // به آخرین اعلامیه خوانده شده نرسیدیم 
                {
                    NumberRead++;
                    _dbContext.Disclosures.Add(disclosure);
                }
            }
            
            if (NumberRead > 0)
            {                
                await _dbContext.SaveChangesAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
            }
            if (lastPublishDate >= CurrentPubDate)
                break;

            pageNumber++;
            // تاخیر 2 ثانیه
            
            var delay = result.TotalPages > 10
                    ? TimeSpan.FromSeconds(10)
                    : TimeSpan.FromSeconds(1);
            await Task.Delay(delay, cancellationToken);

        }
        NumberRead++;

    }
    public async Task CollectAsync2(
    CancellationToken cancellationToken = default)
    {
        var lastPublishDateStr = await _dbContext.Disclosures
            .OrderByDescending(x => x.PublishDateTimeRaw)
            .Select(x => x.PublishDateTimeRaw)
            .FirstOrDefaultAsync(cancellationToken);

#pragma warning disable S125
        // فعلاً برای BackFill یک سال قبل
        //  lastPublishDateStr = PersianDateHelper.ToPersian(DateTime.Now.AddYears(-1));
#pragma warning restore S1075
        var lastPublishDate = PersianDateHelper.ToGregorian(lastPublishDateStr);


        var pageNumber = 1;
        var stop = false;


        while (!stop)
        {
            var result = await _codalClient.SearchAsync(
                new()
                {
                    // شرطهای خواندن کدال مثلا category=3 ; let58 ;,,,,,
                    // در اینجا فقط شماره صفحه ملاک است
                    PageNumber = pageNumber
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
                    letter.HasHtml,
                    false,
                    letter.Url ?? "",
                    letter.HasExcel,
                    false,
                    false,
                    false,
                    null,  // AttachmentUrl
                    null,  // PdfUrl
                    null,  // ExcelUrl
                    null,  // XbrlUrl
                    null,  // TedanUrl
                    let,  // Let
                    rt,  // Rt
                    ct,  // Ct
                    ft); // Ft


                disclosures.Add(disclosure);
            }


            // ذخیره یکجای صفحه
            if (disclosures.Count > 0)
            {
                await _dbContext.Disclosures.AddRangeAsync(
                    disclosures,
                    cancellationToken);


                await _dbContext.SaveChangesAsync(cancellationToken);

                _dbContext.ChangeTracker.Clear();
            }


            // اگر به رکوردهای قدیمی رسیدیم، توقف
            if (stop)
                break;


            pageNumber++;


            var delay = result.TotalPages > 10
                ? TimeSpan.FromSeconds(10)
                : TimeSpan.FromSeconds(1);


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
}

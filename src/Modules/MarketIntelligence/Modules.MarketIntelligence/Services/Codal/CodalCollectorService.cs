
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

            Console.WriteLine(
                $"Reading page {pageNumber}/{result.TotalPages}");
            Console.WriteLine($"Letters count: {result.Letters.Count}");
            
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
}

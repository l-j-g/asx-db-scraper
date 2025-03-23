using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using AsxDbScraper.Services;
using AsxDbScraper.Data;

namespace AsxDbScraper.Functions;

public class ScrapeFinancialStatements
{
    private readonly IAsxScraperService _scraperService;
    private readonly AsxDbContext _dbContext;
    private readonly ILogger<ScrapeFinancialStatements> _logger;

    public ScrapeFinancialStatements(
        IAsxScraperService scraperService,
        AsxDbContext dbContext,
        ILogger<ScrapeFinancialStatements> logger)
    {
        _scraperService = scraperService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [FunctionName("ScrapeFinancialStatements")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        try
        {
            string companyCode = req.Query["companyCode"];
            if (string.IsNullOrEmpty(companyCode))
            {
                return new BadRequestObjectResult("Please provide a company code");
            }

            if (!DateTime.TryParse(req.Query["statementDate"], out DateTime statementDate))
            {
                statementDate = DateTime.UtcNow;
            }

            _logger.LogInformation("Starting financial statement scraping for {CompanyCode}", companyCode);

            // Scrape and save balance sheet
            var balanceSheet = await _scraperService.ScrapeBalanceSheetAsync(companyCode, statementDate);
            await _dbContext.BalanceSheets.AddAsync(balanceSheet);

            // Scrape and save income statement
            var incomeStatement = await _scraperService.ScrapeIncomeStatementAsync(companyCode, statementDate);
            await _dbContext.IncomeStatements.AddAsync(incomeStatement);

            // Scrape and save cash flow statement
            var cashFlowStatement = await _scraperService.ScrapeCashFlowStatementAsync(companyCode, statementDate);
            await _dbContext.CashFlowStatements.AddAsync(cashFlowStatement);

            await _dbContext.SaveChangesAsync();

            return new OkObjectResult(new
            {
                message = "Financial statements scraped successfully",
                companyCode,
                statementDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping financial statements");
            return new StatusCodeResult(500);
        }
    }
} 
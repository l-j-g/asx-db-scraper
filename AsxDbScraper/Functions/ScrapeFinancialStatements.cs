using System.Net;
using AsxDbScraper.Services;
using AsxDbScraper.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

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

    [Function("ScrapeFinancialStatements")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string companyCode = query["companyCode"];
            if (string.IsNullOrEmpty(companyCode))
            {
                var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new { message = "Please provide a company code" });
                return badRequestResponse;
            }

            if (!DateTime.TryParse(query["statementDate"], out DateTime statementDate))
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Financial statements scraped successfully",
                companyCode,
                statementDate
            });
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping financial statements");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new { message = "Internal server error" });
            return response;
        }
    }
}
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AsxDbScraper.Models;

namespace AsxDbScraper.Services;

public interface IAsxScraperService
{
    Task<BalanceSheet> ScrapeBalanceSheetAsync(string companyCode, DateTime statementDate);
    Task<IncomeStatement> ScrapeIncomeStatementAsync(string companyCode, DateTime statementDate);
    Task<CashFlowStatement> ScrapeCashFlowStatementAsync(string companyCode, DateTime statementDate);
}

public class AsxScraperService : IAsxScraperService
{
    private readonly IAlphaVantageService _alphaVantageService;
    private readonly ILogger<AsxScraperService> _logger;

    public AsxScraperService(
        IAlphaVantageService alphaVantageService,
        ILogger<AsxScraperService> logger)
    {
        _alphaVantageService = alphaVantageService;
        _logger = logger;
    }

    public async Task<BalanceSheet> ScrapeBalanceSheetAsync(string companyCode, DateTime statementDate)
    {
        _logger.LogInformation("Scraping balance sheet for {CompanyCode} as of {StatementDate}", companyCode, statementDate);
        return await _alphaVantageService.GetBalanceSheetAsync(companyCode);
    }

    public async Task<IncomeStatement> ScrapeIncomeStatementAsync(string companyCode, DateTime statementDate)
    {
        _logger.LogInformation("Scraping income statement for {CompanyCode} as of {StatementDate}", companyCode, statementDate);
        return await _alphaVantageService.GetIncomeStatementAsync(companyCode);
    }

    public async Task<CashFlowStatement> ScrapeCashFlowStatementAsync(string companyCode, DateTime statementDate)
    {
        _logger.LogInformation("Scraping cash flow statement for {CompanyCode} as of {StatementDate}", companyCode, statementDate);
        return await _alphaVantageService.GetCashFlowStatementAsync(companyCode);
    }
} 
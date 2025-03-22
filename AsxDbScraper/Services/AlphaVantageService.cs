using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AsxDbScraper.Models;

namespace AsxDbScraper.Services;

public interface IAlphaVantageService
{
    Task<BalanceSheet> GetBalanceSheetAsync(string companyCode);
    Task<IncomeStatement> GetIncomeStatementAsync(string companyCode);
    Task<CashFlowStatement> GetCashFlowStatementAsync(string companyCode);
}

public class AlphaVantageService : IAlphaVantageService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AlphaVantageService> _logger;
    private readonly string _apiKey;
    private const string BaseUrl = "https://www.alphavantage.co/query";

    public AlphaVantageService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AlphaVantageService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["AlphaVantage:ApiKey"] ?? throw new ArgumentNullException("AlphaVantage:ApiKey configuration is missing");
    }

    public async Task<BalanceSheet> GetBalanceSheetAsync(string companyCode)
    {
        try
        {
            var url = $"{BaseUrl}?function=BALANCE_SHEET&symbol={companyCode}.AX&apikey={_apiKey}";
            _logger.LogInformation("Fetching balance sheet from Alpha Vantage for {CompanyCode}", companyCode);
            
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<AlphaVantageResponse<BalanceSheetData>>(response);

            if (data?.AnnualReports == null || !data.AnnualReports.Any())
            {
                _logger.LogWarning("No balance sheet data found for {CompanyCode}", companyCode);
                throw new Exception($"No balance sheet data found for {companyCode}");
            }

            var latestReport = data.AnnualReports.First();
            _logger.LogInformation("Successfully retrieved balance sheet for {CompanyCode} as of {StatementDate}", 
                companyCode, latestReport.FiscalDateEnding);

            return new BalanceSheet
            {
                CompanyCode = companyCode,
                StatementDate = DateTime.Parse(latestReport.FiscalDateEnding),
                TotalAssets = decimal.Parse(latestReport.TotalAssets),
                CurrentAssets = decimal.Parse(latestReport.TotalCurrentAssets),
                NonCurrentAssets = decimal.Parse(latestReport.TotalNonCurrentAssets),
                TotalLiabilities = decimal.Parse(latestReport.TotalLiabilities),
                CurrentLiabilities = decimal.Parse(latestReport.TotalCurrentLiabilities),
                NonCurrentLiabilities = decimal.Parse(latestReport.TotalNonCurrentLiabilities),
                TotalEquity = decimal.Parse(latestReport.TotalShareholderEquity),
                SourceUrl = url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching balance sheet for {CompanyCode}", companyCode);
            throw;
        }
    }

    public async Task<IncomeStatement> GetIncomeStatementAsync(string companyCode)
    {
        try
        {
            var url = $"{BaseUrl}?function=INCOME_STATEMENT&symbol={companyCode}.AX&apikey={_apiKey}";
            _logger.LogInformation("Fetching income statement from Alpha Vantage for {CompanyCode}", companyCode);
            
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<AlphaVantageResponse<IncomeStatementData>>(response);

            if (data?.AnnualReports == null || !data.AnnualReports.Any())
            {
                _logger.LogWarning("No income statement data found for {CompanyCode}", companyCode);
                throw new Exception($"No income statement data found for {companyCode}");
            }

            var latestReport = data.AnnualReports.First();
            _logger.LogInformation("Successfully retrieved income statement for {CompanyCode} as of {StatementDate}", 
                companyCode, latestReport.FiscalDateEnding);

            return new IncomeStatement
            {
                CompanyCode = companyCode,
                StatementDate = DateTime.Parse(latestReport.FiscalDateEnding),
                Revenue = decimal.Parse(latestReport.TotalRevenue),
                CostOfGoodsSold = decimal.Parse(latestReport.CostOfRevenue),
                GrossProfit = decimal.Parse(latestReport.GrossProfit),
                OperatingExpenses = decimal.Parse(latestReport.OperatingExpenses),
                OperatingIncome = decimal.Parse(latestReport.OperatingIncome),
                NetIncome = decimal.Parse(latestReport.NetIncome),
                EarningsPerShare = decimal.Parse(latestReport.EarningsPerShare),
                SourceUrl = url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching income statement for {CompanyCode}", companyCode);
            throw;
        }
    }

    public async Task<CashFlowStatement> GetCashFlowStatementAsync(string companyCode)
    {
        try
        {
            var url = $"{BaseUrl}?function=CASH_FLOW&symbol={companyCode}.AX&apikey={_apiKey}";
            _logger.LogInformation("Fetching cash flow statement from Alpha Vantage for {CompanyCode}", companyCode);
            
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonSerializer.Deserialize<AlphaVantageResponse<CashFlowData>>(response);

            if (data?.AnnualReports == null || !data.AnnualReports.Any())
            {
                _logger.LogWarning("No cash flow data found for {CompanyCode}", companyCode);
                throw new Exception($"No cash flow data found for {companyCode}");
            }

            var latestReport = data.AnnualReports.First();
            _logger.LogInformation("Successfully retrieved cash flow statement for {CompanyCode} as of {StatementDate}", 
                companyCode, latestReport.FiscalDateEnding);

            return new CashFlowStatement
            {
                CompanyCode = companyCode,
                StatementDate = DateTime.Parse(latestReport.FiscalDateEnding),
                OperatingCashFlow = decimal.Parse(latestReport.OperatingCashFlow),
                InvestingCashFlow = decimal.Parse(latestReport.InvestingCashFlow),
                FinancingCashFlow = decimal.Parse(latestReport.FinancingCashFlow),
                NetCashFlow = decimal.Parse(latestReport.NetCashFlow),
                BeginningCashBalance = decimal.Parse(latestReport.BeginningCashPosition),
                EndingCashBalance = decimal.Parse(latestReport.EndingCashPosition),
                SourceUrl = url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching cash flow statement for {CompanyCode}", companyCode);
            throw;
        }
    }
}

// Alpha Vantage API Response Models
public class AlphaVantageResponse<T>
{
    public List<T> AnnualReports { get; set; } = new();
}

public class BalanceSheetData
{
    public string FiscalDateEnding { get; set; } = string.Empty;
    public string TotalAssets { get; set; } = string.Empty;
    public string TotalCurrentAssets { get; set; } = string.Empty;
    public string TotalNonCurrentAssets { get; set; } = string.Empty;
    public string TotalLiabilities { get; set; } = string.Empty;
    public string TotalCurrentLiabilities { get; set; } = string.Empty;
    public string TotalNonCurrentLiabilities { get; set; } = string.Empty;
    public string TotalShareholderEquity { get; set; } = string.Empty;
}

public class IncomeStatementData
{
    public string FiscalDateEnding { get; set; } = string.Empty;
    public string TotalRevenue { get; set; } = string.Empty;
    public string CostOfRevenue { get; set; } = string.Empty;
    public string GrossProfit { get; set; } = string.Empty;
    public string OperatingExpenses { get; set; } = string.Empty;
    public string OperatingIncome { get; set; } = string.Empty;
    public string NetIncome { get; set; } = string.Empty;
    public string EarningsPerShare { get; set; } = string.Empty;
}

public class CashFlowData
{
    public string FiscalDateEnding { get; set; } = string.Empty;
    public string OperatingCashFlow { get; set; } = string.Empty;
    public string InvestingCashFlow { get; set; } = string.Empty;
    public string FinancingCashFlow { get; set; } = string.Empty;
    public string NetCashFlow { get; set; } = string.Empty;
    public string BeginningCashPosition { get; set; } = string.Empty;
    public string EndingCashPosition { get; set; } = string.Empty;
} 
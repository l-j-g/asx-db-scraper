using System;

namespace AsxDbScraper.Models;

public abstract class FinancialStatement
{
    public int Id { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public DateTime StatementDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string SourceUrl { get; set; } = string.Empty;
}

public class BalanceSheet : FinancialStatement
{
    public decimal TotalAssets { get; set; }
    public decimal CurrentAssets { get; set; }
    public decimal NonCurrentAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal CurrentLiabilities { get; set; }
    public decimal NonCurrentLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
}

public class IncomeStatement : FinancialStatement
{
    public decimal Revenue { get; set; }
    public decimal CostOfGoodsSold { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal OperatingExpenses { get; set; }
    public decimal OperatingIncome { get; set; }
    public decimal NetIncome { get; set; }
    public decimal EarningsPerShare { get; set; }
}

public class CashFlowStatement : FinancialStatement
{
    public decimal OperatingCashFlow { get; set; }
    public decimal InvestingCashFlow { get; set; }
    public decimal FinancingCashFlow { get; set; }
    public decimal NetCashFlow { get; set; }
    public decimal BeginningCashBalance { get; set; }
    public decimal EndingCashBalance { get; set; }
} 
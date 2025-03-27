using AsxDbScraper.Models;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Threading;

namespace AsxDbScraper.Services
{
    public interface IAsxScraperService
    {
        Task<bool> ScrapeNextCompanyAsync();
        Task<int> GetQueueLengthAsync();
    }

    public class AsxScraperService : IAsxScraperService
    {
        private readonly IAlphaVantageService _alphaVantageService;
        private readonly IAsxCompanyService _companyService;
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<AsxScraperService> _logger;
        private readonly string _databaseName = "AsxDbScraper";
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public AsxScraperService(
            IAlphaVantageService alphaVantageService,
            IAsxCompanyService companyService,
            CosmosClient cosmosClient,
            ILogger<AsxScraperService> logger)
        {
            _alphaVantageService = alphaVantageService;
            _companyService = companyService;
            _cosmosClient = cosmosClient;
            _logger = logger;
        }

        public async Task<int> GetQueueLengthAsync()
        {
            try
            {
                var companies = await _companyService.GetAllCompaniesAsync();
                return companies.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue length");
                return -1;
            }
        }

        public async Task<bool> ScrapeNextCompanyAsync()
        {
            // Use a semaphore to prevent concurrent executions
            // This prevents multiple Azure Function instances from processing the same company
            if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(1)))
            {
                _logger.LogWarning("Another scrape operation is in progress. Skipping this execution.");
                return false;
            }

            try
            {
                // Get all companies ordered by LastUpdated (oldest first)
                var companies = await _companyService.GetAllCompaniesAsync();
                var nextCompany = companies
                    .OrderBy(c => c.LastUpdated)
                    .FirstOrDefault();

                if (nextCompany == null)
                {
                    _logger.LogInformation("No companies found to scrape");
                    return false;
                }

                // Calculate age of data in days
                var dataAge = (DateTime.UtcNow - nextCompany.LastUpdated).TotalDays;
                _logger.LogInformation("Scraping financial data for {CompanyCode} (last updated: {LastUpdated}, age: {DataAge:N1} days)",
                    nextCompany.Code, nextCompany.LastUpdated, dataAge);

                // Get containers
                var balanceSheetsContainer = _cosmosClient.GetContainer(_databaseName, "BalanceSheets");
                var incomeStatementsContainer = _cosmosClient.GetContainer(_databaseName, "IncomeStatements");
                var cashFlowsContainer = _cosmosClient.GetContainer(_databaseName, "CashFlowStatements");
                var companiesContainer = _cosmosClient.GetContainer(_databaseName, "Companies");

                // Track metrics for reporting
                var successCount = 0;
                var startTime = DateTime.UtcNow;

                try
                {
                    // Scrape balance sheet
                    var balanceSheet = await _alphaVantageService.GetBalanceSheetAsync(nextCompany.Code);
                    if (balanceSheet != null)
                    {
                        await balanceSheetsContainer.UpsertItemAsync(balanceSheet, new PartitionKey(balanceSheet.CompanyCode));
                        successCount++;
                        _logger.LogInformation("Balance sheet for {CompanyCode} updated", nextCompany.Code);
                    }

                    // Scrape income statement
                    var incomeStatement = await _alphaVantageService.GetIncomeStatementAsync(nextCompany.Code);
                    if (incomeStatement != null)
                    {
                        await incomeStatementsContainer.UpsertItemAsync(incomeStatement, new PartitionKey(incomeStatement.CompanyCode));
                        successCount++;
                        _logger.LogInformation("Income statement for {CompanyCode} updated", nextCompany.Code);
                    }

                    // Scrape cash flow
                    var cashFlow = await _alphaVantageService.GetCashFlowStatementAsync(nextCompany.Code);
                    if (cashFlow != null)
                    {
                        await cashFlowsContainer.UpsertItemAsync(cashFlow, new PartitionKey(cashFlow.CompanyCode));
                        successCount++;
                        _logger.LogInformation("Cash flow statement for {CompanyCode} updated", nextCompany.Code);
                    }

                    // Always update the LastUpdated timestamp, even if some operations failed
                    // This prevents us from getting stuck on companies that consistently fail
                    nextCompany.LastUpdated = DateTime.UtcNow;
                    await companiesContainer.UpsertItemAsync(nextCompany, new PartitionKey(nextCompany.Code));

                    var elapsed = DateTime.UtcNow - startTime;
                    _logger.LogInformation("Company {CompanyCode} processed with {SuccessCount}/3 statement updates in {ElapsedMs}ms",
                        nextCompany.Code, successCount, elapsed.TotalMilliseconds);

                    return successCount > 0;
                }
                catch (Exception ex)
                {
                    // Handle different types of exceptions differently
                    if (ex is CosmosException cosmosEx)
                    {
                        if (cosmosEx.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("Rate limit exceeded for Cosmos DB. Consider reducing frequency.");
                        }
                        else if (cosmosEx.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                        {
                            _logger.LogWarning("Data too large for Cosmos DB document for company {CompanyCode}", nextCompany.Code);
                        }
                    }

                    _logger.LogError(ex, "Error processing financial data for {CompanyCode}", nextCompany.Code);

                    // Still update LastUpdated to prevent getting stuck on problematic companies
                    try
                    {
                        nextCompany.LastUpdated = DateTime.UtcNow;
                        await companiesContainer.UpsertItemAsync(nextCompany, new PartitionKey(nextCompany.Code));
                        _logger.LogInformation("Updated LastUpdated for company {CompanyCode} despite errors", nextCompany.Code);
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(updateEx, "Failed to update LastUpdated for company {CompanyCode}", nextCompany.Code);
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ScrapeNextCompanyAsync");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
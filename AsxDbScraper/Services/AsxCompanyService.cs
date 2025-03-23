using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AsxDbScraper.Models;
using AsxDbScraper.Data;

namespace AsxDbScraper.Services;

public interface IAsxCompanyService
{
    Task<IEnumerable<AsxCompany>> GetAllCompaniesAsync();
    Task<AsxCompany?> GetCompanyByCodeAsync(string code);
    Task UpdateCompaniesListAsync();
}

public class AsxCompanyService : IAsxCompanyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AsxCompanyService> _logger;
    private readonly AsxDbContext _dbContext;
    private const string AsxListedCompaniesUrl = "https://www.asx.com.au/asx/research/ASXListedCompanies.csv";

    public AsxCompanyService(
        HttpClient httpClient,
        ILogger<AsxCompanyService> logger,
        AsxDbContext dbContext)
    {
        _httpClient = httpClient;
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<AsxCompany>> GetAllCompaniesAsync()
    {
        _logger.LogInformation("Fetching all ASX-listed companies from database");
        return await Task.FromResult(_dbContext.AsxCompanies.Where(c => c.IsActive).OrderBy(c => c.Code));
    }

    public async Task<AsxCompany?> GetCompanyByCodeAsync(string code)
    {
        _logger.LogInformation("Fetching ASX company with code {Code}", code);
        return await Task.FromResult(_dbContext.AsxCompanies.FirstOrDefault(c => c.Code == code && c.IsActive));
    }

    public async Task UpdateCompaniesListAsync()
    {
        try
        {
            _logger.LogInformation("Starting ASX companies list update");
            
            // Fetch the CSV file
            var response = await _httpClient.GetStringAsync(AsxListedCompaniesUrl);
            var lines = response.Split('\n').Skip(1); // Skip header row

            // Parse companies
            var companies = new List<AsxCompany>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = line.Split(',');
                if (fields.Length >= 3)
                {
                    companies.Add(new AsxCompany
                    {
                        Code = fields[0].Trim('"'),
                        Name = fields[1].Trim('"'),
                        Industry = fields[2].Trim('"'),
                        LastUpdated = DateTime.UtcNow,
                        IsActive = true
                    });
                }
            }

            _logger.LogInformation("Found {Count} companies in ASX list", companies.Count);

            // Update database
            var existingCompanies = _dbContext.AsxCompanies.ToList();
            
            // Mark companies not in the new list as inactive
            foreach (var existing in existingCompanies)
            {
                if (!companies.Any(c => c.Code == existing.Code))
                {
                    existing.IsActive = false;
                    existing.LastUpdated = DateTime.UtcNow;
                }
                _logger.LogInformation("Updated ASX company {Code} to {IsActive}", existing.Code, existing.IsActive);
            }

            // Add or update companies
            foreach (var company in companies)
            {
                var existing = existingCompanies.FirstOrDefault(c => c.Code == company.Code);
                if (existing == null)
                {
                    _dbContext.AsxCompanies.Add(company);
                    _logger.LogInformation("Added ASX company {Code}", company.Code);
                }
                else
                {
                    existing.Name = company.Name;
                    existing.Industry = company.Industry;
                    existing.LastUpdated = DateTime.UtcNow;
                    existing.IsActive = true;
                    _logger.LogInformation("Updated ASX company {Code}", existing.Code);
                }
            }

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Successfully updated ASX companies list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ASX companies list");
            throw;
        }
    }
} 
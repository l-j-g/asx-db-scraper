using Microsoft.Azure.Cosmos;
using AsxDbScraper.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsxDbScraper.Services
{
    public interface IAsxCompanyService
    {
        Task<IEnumerable<AsxCompany>> GetAllCompaniesAsync();
        Task<AsxCompany> GetCompanyByCodeAsync(string code);
        Task UpdateCompaniesListAsync();
    }

    public class AsxCompanyService : IAsxCompanyService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly IAlphaVantageService _alphaVantageService;
        private readonly string _databaseName = "AsxDbScraper";
        private readonly string _containerName = "Companies";

        public AsxCompanyService(CosmosClient cosmosClient, IAlphaVantageService alphaVantageService)
        {
            _cosmosClient = cosmosClient;
            _alphaVantageService = alphaVantageService;
        }

        public async Task<IEnumerable<AsxCompany>> GetAllCompaniesAsync()
        {
            var container = _cosmosClient.GetContainer(_databaseName, _containerName);
            var query = new QueryDefinition("SELECT * FROM c");
            var iterator = container.GetItemQueryIterator<AsxCompany>(query);
            var companies = new List<AsxCompany>();

            while (iterator.HasMoreResults)
            {
                var results = await iterator.ReadNextAsync();
                companies.AddRange(results);
            }

            return companies;
        }

        public async Task<AsxCompany> GetCompanyByCodeAsync(string code)
        {
            var container = _cosmosClient.GetContainer(_databaseName, _containerName);
            var query = new QueryDefinition("SELECT * FROM c WHERE c.code = @code")
                .WithParameter("@code", code.ToUpper());
            
            var iterator = container.GetItemQueryIterator<AsxCompany>(query);
            var results = await iterator.ReadNextAsync();

            return results.FirstOrDefault();
        }

        public async Task UpdateCompaniesListAsync()
        {
            var container = _cosmosClient.GetContainer(_databaseName, _containerName);
            var companies = await _alphaVantageService.GetAsxCompaniesAsync();

            foreach (var company in companies)
            {
                try
                {
                    await container.UpsertItemAsync(company, new PartitionKey(company.Code));
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Error processing company {company.Code}: {ex.Message}");
                }
            }
        }
    }
} 
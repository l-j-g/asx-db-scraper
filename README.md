# ASX DB Scraper

A serverless Azure Function application that scrapes and stores financial data for Australian Securities Exchange (ASX) listed companies. This project efficiently collects balance sheets, income statements, and cash flow statements using a rate-limited approach that stays within free tier API limits.

![Azure](https://img.shields.io/badge/azure-%230072C6.svg?style=for-the-badge&logo=microsoftazure&logoColor=white)
![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.Net](https://img.shields.io/badge/.NET-%235C2D91.svg?style=for-the-badge&logo=.net&logoColor=white)
![Cosmos DB](https://img.shields.io/badge/Cosmos%20DB-0078D4.svg?style=for-the-badge&logo=microsoftazure&logoColor=white)

## Architecture

![Architecture Diagram](docs/architecture.png)

This project uses a modern cloud-native architecture:

- **Azure Functions** - Serverless compute running on consumption plan (free tier)
- **Azure Cosmos DB** - NoSQL database with free tier capabilities
- **Alpha Vantage API** - Financial data source with free tier option
- **Azure Key Vault** - Secure secret storage
- **Azure Monitor** - Logging and monitoring

### Key Technical Features

- **Intelligent Rate Limiting** - Automatically rotates through companies based on data age
- **Resilient Error Handling** - Comprehensive exception management prevents stuck processing
- **Serverless Architecture** - Only pay for what you use (with substantial free tier limits)
- **Dependency Injection** - Clean separation of concerns and improved testability
- **Concurrency Control** - Semaphore pattern prevents duplicate processing
- **JSON Document Storage** - Flexible schema for financial data
- **Performance Metrics** - Runtime monitoring of processing times
- **RESTful API Endpoint** - HTTP trigger for status monitoring

## Cost Optimization

This project is specifically designed to operate within free tiers:

| Resource | Free Tier Limits | Project Usage |
|----------|------------------|--------------|
| Azure Functions | 1M executions/month | ~4,320 executions/month (every 10 min) |
| Cosmos DB | 1000 RU/s, 25GB storage | < 100 RU/s typical, < 1GB storage |
| Storage Account | 5GB LRS | < 100MB |
| Alpha Vantage API | 5 calls/min, 500/day | 1 call per 10 min (144/day) |

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools
- Azure account (free tier sufficient)
- Alpha Vantage API key (free tier available)

### Local Development Setup

1. Clone the repository
   ```bash
   git clone https://github.com/yourusername/asx-db-scraper.git
   cd asx-db-scraper
   ```

2. Get an Alpha Vantage API key from [alphavantage.co](https://www.alphavantage.co/support/#api-key)

3. Create `local.settings.json` file:
   ```json
   {
     "IsEncrypted": false,
     "Values": {
       "AzureWebJobsStorage": "UseDevelopmentStorage=true",
       "FUNCTIONS_WORKER_RUNTIME": "dotnet",
       "CosmosDb:ConnectionString": "<your-cosmos-db-connection-string>",
       "CosmosDb:DatabaseName": "AsxDbScraper",
       "AlphaVantage:ApiKey": "<your-api-key>"
     }
   }
   ```

4. Run the function app locally:
   ```bash
   func start
   ```

### Cloud Deployment

Deploy to Azure using the Infrastructure as Code (IaC) template:

```bash
az login
az account set --subscription "<your-subscription-id>"
az group create --name asx-db-scraper-rg --location australiaeast
az deployment group create --resource-group asx-db-scraper-rg --template-file infrastructure/main.bicep
```

## Project Structure

- `Functions/` - Azure Function triggers
  - `FinancialDataFunction.cs` - Timer trigger for scraping and HTTP endpoint for status
  - `AsxCompaniesFunction.cs` - HTTP endpoints for company data
- `Services/` - Business logic services
  - `AsxScraperService.cs` - Core scraping orchestration service
  - `AsxCompanyService.cs` - Company data management
  - `AlphaVantageService.cs` - Financial data provider integration
- `Models/` - Data models
- `infrastructure/` - IaC templates for Azure deployment

## Monitoring and Management

Monitor the scraper progress using the built-in status API:

```bash
curl https://your-function-app.azurewebsites.net/api/GetScraperStatus?code=<function-key>
```

Response:
```json
{
  "status": "running",
  "queueLength": 42,
  "interval": "10 minutes",
  "timestamp": "2024-03-26T12:34:56.789Z"
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Future Enhancements

- Web front-end for data visualization
- Machine learning predictions for stock performance
- Historical data analysis
- Real-time data streaming with SignalR
- Multi-region deployment for global redundancy 
# ASX DB Scraper

An Azure Function application that scrapes financial market data for Australian stocks from the ASX (Australian Securities Exchange). 

## Features

- Automated process that runs as an Azure Function with HTTP trigger
- Scrapes balance sheets, income statements, and cash flow statements
- Stores data in SQL Server using Entity Framework Core
- Logs operations using Serilog
- Cross-platform support (Windows, macOS, Linux)

## Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools
- SQL Server (local or Azure)
- Visual Studio 2022, VS Code with C# extension, or Rider
- Alpha Vantage API key (free tier available)

## Setup

1. Clone the repository

2. Get an Alpha Vantage API key:
   - Go to https://www.alphavantage.co/support/#api-key
   - Sign up for a free API key
   - Copy your API key

3. Set up SQL Server:
   - For Windows: Install SQL Server Express LocalDB
   - For macOS/Linux: Use Docker or Azure SQL Database
   - Update the connection string in `local.settings.json` with your SQL Server details

4. Update the configuration:
   - Open `local.settings.json`
   - Replace `YOUR_API_KEY_HERE` with your Alpha Vantage API key
   - Update the connection string if needed

5. Run the following commands to restore packages and build:
   ```bash
   dotnet restore
   dotnet build
   ```

6. Run the following command to create the database:
   ```bash
   dotnet ef database update
   ```

7. Start the Azure Functions host:
   ```bash
   func start
   ```

## Development Environment Setup

### Windows
- Install Visual Studio 2022 or VS Code with C# extension
- Install SQL Server Express LocalDB
- Install Azure Functions Core Tools

### macOS
- Install VS Code with C# extension or JetBrains Rider
- Install Docker Desktop for running SQL Server
- Install Azure Functions Core Tools:
  ```bash
   brew tap azure/functions
   brew install azure-functions-core-tools@4
   ```

### Linux
- Install VS Code with C# extension or JetBrains Rider
- Install Docker for running SQL Server
- Install Azure Functions Core Tools:
  ```bash
   curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg
   sudo mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg
   sudo sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-$(lsb_release -cs)-prod $(lsb_release -cs) main" > /etc/apt/sources.list.d/dotnetdev.list'
   sudo apt-get update
   sudo apt-get install azure-functions-core-tools-4
   ```

## Usage

The function can be triggered via HTTP POST request with the following query parameters:
- `companyCode`: The ASX company code (e.g., "BHP")
- `statementDate`: (Optional) The date of the financial statements. Defaults to current date.

Example request:
```
POST http://localhost:7071/api/ScrapeFinancialStatements?companyCode=BHP&statementDate=2024-03-20
```

Note: Alpha Vantage's free tier has a limit of 5 API calls per minute. The function will automatically handle rate limiting.

## Project Structure

- `Models/`: Contains the data models for financial statements
- `Data/`: Contains the Entity Framework DbContext
- `Services/`: Contains the ASX scraping service and Alpha Vantage integration
- `Functions/`: Contains the Azure Function implementation
- `logs/`: Contains application logs (created automatically)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details. 
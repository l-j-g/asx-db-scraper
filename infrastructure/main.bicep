@description('The location for all resources')
param location string = resourceGroup().location

@description('The environment (dev, staging, prod)')
param environment string

@description('The project name')
param projectName string = 'asxdb'

@description('The function app name')
param functionAppName string = '${projectName}-${environment}'

@description('The Cosmos DB account name')
param cosmosDbAccountName string = '${projectName}-${environment}-cosmos-db-account'
@description('The Cosmos DB database name')
param cosmosDbName string = 'AsxDbScraper'

var storageAccountName = '${projectName}${environment}storage'

@description('The Cosmos DB container names')
param containers array = [
  {
    name: 'Companies'
  }
  {
    name: 'BalanceSheets'
  }
  {
    name: 'IncomeStatements'
  }
  {
    name: 'CashFlowStatements'
  }
]
@description('The Key Vault name')
param keyVaultName string = '${projectName}-${environment}-kv'

// Create storage account for function app
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    networkAcls: {
      defaultAction: 'Deny'
      bypass: 'AzureServices'
    }
  }
}

// Create Cosmos DB account
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: cosmosDbAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    enableAutomaticFailover: true
    networkAclBypass: 'AzureServices'
    networkAclBypassResourceIds: []
    publicNetworkAccess: 'Disabled'
    virtualNetworkRules: []
  }
}

// Create Cosmos DB database
resource cosmosDb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-11-15' = {
  parent: cosmosDbAccount
  name: cosmosDbName
  properties: {
    resource: {
      id: cosmosDbName
    }
  }
}

// Create Cosmos DB containers
resource cosmosDbContainers 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = [
  for container in containers: {
    parent: cosmosDb
    name: container.name
    properties: {
      resource: {
        id: container.name
        partitionKey: {
          paths: ['/id']
        }
        indexingPolicy: {
          indexingMode: 'consistent'
          automatic: true
        }
      }
    }
  }
]

// Create App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1' // Consumption plan
  }
  kind: 'functionapp'
  properties: {
    reserved: true
    targetWorkerCount: 1
    maximumElasticWorkerCount: 1
    perSiteScaling: false
  }
}

// Create Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: true
    tenantId: subscription().tenantId
    accessPolicies: []
    sku: {
      name: 'standard'
      family: 'A'
    }
  }
}

// Create Function App without Key Vault reference initially
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'CosmosDb__ConnectionString'
          value: cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'CosmosDb__DatabaseName'
          value: cosmosDbName
        }
        {
          name: 'CosmosDb__ContainerName'
          value: containers[0].name
        }
      ]
      cors: {
        allowedOrigins: ['http://localhost:3000', 'https://localhost:3000']
        supportCredentials: true
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Update Key Vault access policy after Function App is created
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// Add Key Vault reference to Function App after access policy is set
resource functionAppSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: functionApp
  name: 'appsettings'
  properties: {
    AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
    FUNCTIONS_WORKER_RUNTIME: 'dotnet'
    FUNCTIONS_EXTENSION_VERSION: '~4'
    'CosmosDb__ConnectionString': cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
    'CosmosDb__DatabaseName': cosmosDbName
    'CosmosDb__ContainerName': containers[0].name
    'AlphaVantage__ApiKey': '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AlphaVantageApiKey/)'
  }
}

// Output values (no secrets)
output functionAppName string = functionApp.name
output cosmosDbName string = cosmosDbName
output keyVaultName string = keyVault.name

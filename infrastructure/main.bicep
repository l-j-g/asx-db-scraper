@description('The name of the resource group')
param resourceGroupName string

@description('The location for all resources')
param location string = resourceGroup().location

@description('The environment (dev, staging, prod)')
param environment string

@description('The project name')
param projectName string = 'asx-db-scraper'

@description('The function app name')
param functionAppName string = '${projectName}-${environment}'

@description('The Cosmos DB account name')
param cosmosDbAccountName string = '${projectName}-${environment}'

@description('The Cosmos DB database name')
param cosmosDbName string = 'AsxDbScraper'

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

// Generate a unique name for the storage account
var storageAccountName = '${projectName}${environment}${uniqueString(resourceGroup().id)}'

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
resource cosmosDbContainers 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-11-15' = [for container in containers: {
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
}]

// Create App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${functionAppName}-plan'
  location: location
  sku: {
    name: 'Y1' // Consumption plan
  }
  kind: 'functionapp'
  properties: {
    name: '${functionAppName}-plan'
    reserved: true
    targetWorkerCount: 1
    maximumElasticWorkerCount: 1
    perSiteScaling: false
  }
}

// Create Function App
resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  properties: {
    name: functionAppName
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
          name: 'CosmosDb:ConnectionString'
          value: cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
        }
        {
          name: 'CosmosDb:DatabaseName'
          value: cosmosDbName
        }
        {
          name: 'CosmosDb:ContainerName'
          value: containers[0].name
        }
        {
          name: 'AlphaVantage:ApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AlphaVantageApiKey/)'
        }
      ]
      cors: {
        allowedOrigins: ['*']
        allowedMethods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS']
        allowedHeaders: ['*']
        exposedHeaders: []
        maxAgeInSeconds: 86400
        allowCredentials: true
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Create Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: '${projectName}-${environment}-kv'
  location: location
  properties: {
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    enabledForDiskEncryption: true
    tenantId: subscription().tenantId
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
    sku: {
      name: 'standard'
      family: 'A'
    }
  }
}

// Output values
output functionAppName string = functionApp.name
output cosmosDbConnectionString string = cosmosDbAccount.listConnectionStrings().connectionStrings[0].connectionString
output keyVaultName string = keyVault.name 

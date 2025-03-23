@description('The environment (dev, prod)')
param environment string

@description('The location for all resources')
param location string

@description('The project name')
param projectName string

@description('The storage account name')
param storageAccountName string = replace(toLower('${projectName}${environment}'), '-', '')

@description('The Cosmos DB account name')
param cosmosDbAccountName string = '${projectName}-${environment}'

@description('The Cosmos DB database name')
param cosmosDbName string = 'AsxDbScraper'

@description('The Key Vault name')
param keyVaultName string = '${projectName}-${environment}-kv'

@description('The GitHub Service Principal ID')
param githubPrincipalId string

// Single Function App name without environment suffix
var functionAppName = projectName

@description('The Cosmos DB container names')
param containers array = [
  { name: 'Companies' }
  { name: 'BalanceSheets' }
  { name: 'IncomeStatements' }
  { name: 'CashFlowStatements' }
]

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
      defaultAction: 'Allow' // Changed from 'Deny' to 'Allow'
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
    publicNetworkAccess: 'Enabled' // Changed from 'Disabled' to 'Enabled'
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

// Create Key Vault first to avoid circular dependency
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

// Create single Function App (production slot)
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
        {
          name: 'AlphaVantage__ApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AlphaVantageApiKey/)'
        }
      ]
      cors: {
        allowedOrigins: ['http://localhost:3000']
        supportCredentials: true
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Create dev deployment slot
resource devSlot 'Microsoft.Web/sites/slots@2022-09-01' = {
  parent: functionApp
  name: 'dev'
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
        {
          name: 'AlphaVantage__ApiKey'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/AlphaVantageApiKey/)'
        }
        {
          name: 'SLOT_NAME'
          value: 'dev'
        }
      ]
      cors: {
        allowedOrigins: ['http://localhost:3000']
        supportCredentials: true
      }
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

// Update Key Vault access policy for Function App
resource functionAppKeyVaultPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
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

// Update Key Vault access policy for Dev Slot
resource devSlotKeyVaultPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: devSlot.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// Add Storage Blob Data Contributor role to GitHub service principal
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, storageAccount.id, 'StorageBlobDataContributor')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    ) // Storage Blob Data Contributor role
    principalId: githubPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Output values (no secrets)
output functionAppName string = functionApp.name
output keyVaultName string = keyVault.name
output cosmosDbName string = cosmosDbName
output storageAccountName string = storageAccount.name

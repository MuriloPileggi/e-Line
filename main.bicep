// Bicep template for e-line Azure resources 

@description('The base name for all resources')
param projectName string = 'eline'

@description('The Azure region where the resources will be deployde')
param location string = 'brazilsouth'

// Variables to create consistent naming for all our resources 
var resourceGroupName = 'rg-${projectName}-prod-br'
var appServicePlanName = 'plan-${projectName}-prod-br'
var appServiceName = 'app-${projectName}-prod-br'
var cosmosDBAccountName = 'db-${projectName}-prod-br'
var keyVaultName = 'kv-${projectName}-prod-br-${uniqueString(resourceGroup().id)}'
var appInsightsName = 'ai-${projectName}-prod-br'

// --- Resource Definitions ---

// 1. Application insights: For monitoring and logging our application
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// 2. App service plan: Defines compute resources (CPU/RAM) for our web app
// Using a basic linux sku to keep costs down for MVP
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1' // Basic tier
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for linux plans
  }
}

// 3. App service: Hosts our ASP.NET Core Minimal API
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true // Enforce HTTPS for security
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      appSettings: [
        // Link app insights to our app service for automatic monitoring
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
      ]
    }
  }
}

// 4. Cosmos DB account: Serverless NoSQL database
resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosDBAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
      }
    ]
    // Using serverless capacity mode to keep costs minimal
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

// 5. Key vault: To securely store applications secrets
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: [] // Will need to add access policy to add se
  }
}

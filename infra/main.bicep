// main.bicep - Main entry point for Expense Management App infrastructure

@description('Azure region for all resources')
param location string = 'uksouth'

@description('Object ID of the Entra ID admin for SQL Server')
param adminObjectId string

@description('Login (email) of the Entra ID admin for SQL Server')
param adminLogin string

@description('Whether to deploy GenAI resources (Azure OpenAI + AI Search)')
param deployGenAI bool = false

// Unique suffix based on resource group ID (stable, no timestamps)
var uniqueSuffix = uniqueString(resourceGroup().id)
var appName        = 'app-expensemgmt-${uniqueSuffix}'
var sqlServerName  = 'sql-expensemgmt-${uniqueSuffix}'
var openAIName     = 'oai-expensemgmt-${uniqueSuffix}'
var searchName     = 'srch-expensemgmt-${uniqueSuffix}'
var managedIdentityName = 'mid-AppModAssist-02-03-38'

// Deploy App Service + Managed Identity
module appServiceModule 'app-service.bicep' = {
  name: 'appServiceDeploy'
  params: {
    location: location
    appName: appName
    managedIdentityName: managedIdentityName
  }
}

// Deploy Azure SQL
module sqlModule 'azure-sql.bicep' = {
  name: 'sqlDeploy'
  params: {
    location: location
    sqlServerName: sqlServerName
    databaseName: 'Northwind'
    administratorLogin: adminLogin
    administratorObjectId: adminObjectId
    managedIdentityPrincipalId: appServiceModule.outputs.managedIdentityPrincipalId
    managedIdentityName: managedIdentityName
  }
}

// Conditionally deploy GenAI resources
module genAIModule 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeploy'
  params: {
    location: location
    openAILocation: 'swedencentral'
    managedIdentityPrincipalId: appServiceModule.outputs.managedIdentityPrincipalId
    managedIdentityClientId: appServiceModule.outputs.managedIdentityClientId
    openAIName: openAIName
    searchName: searchName
  }
}

// Outputs
output appServiceName string = appServiceModule.outputs.appServiceName
output appServiceUrl string = appServiceModule.outputs.appServiceUrl
output sqlServerFqdn string = sqlModule.outputs.sqlServerFqdn
output sqlDatabaseName string = sqlModule.outputs.databaseName
output managedIdentityClientId string = appServiceModule.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appServiceModule.outputs.managedIdentityPrincipalId
output openAIEndpoint string = deployGenAI ? genAIModule.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAIModule.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAIModule.outputs.searchEndpoint : ''

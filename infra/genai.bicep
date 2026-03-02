// genai.bicep - Azure OpenAI + AI Search with RBAC for managed identity

@description('Azure region for AI Search (uksouth)')
param location string

@description('Azure region for Azure OpenAI (swedencentral)')
param openAILocation string = 'swedencentral'

@description('Principal ID of the managed identity')
param managedIdentityPrincipalId string

@description('Client ID of the managed identity')
param managedIdentityClientId string

@description('Name for the Azure OpenAI resource')
param openAIName string

@description('Name for the AI Search resource')
param searchName string

// GPT-4o model deployment name
var modelDeploymentName = 'gpt-4o'

// Role definition IDs
var cognitiveServicesOpenAIUserRoleId = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
var searchIndexDataReaderRoleId = '1407120a-92aa-4202-b7e9-c0e197c71c8f'

// Azure OpenAI resource in swedencentral
resource openAI 'Microsoft.CognitiveServices/accounts@2023-10-01-preview' = {
  name: toLower(openAIName)
  location: openAILocation
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: toLower(openAIName)
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o model deployment
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-10-01-preview' = {
  parent: openAI
  name: modelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 8
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-11-20'
    }
  }
}

// AI Search in uksouth
resource aiSearch 'Microsoft.Search/searchServices@2023-11-01' = {
  name: toLower(searchName)
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    publicNetworkAccess: 'enabled'
  }
}

// RBAC: Cognitive Services OpenAI User on OpenAI resource
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, cognitiveServicesOpenAIUserRoleId)
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesOpenAIUserRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// RBAC: Search Index Data Reader on AI Search resource
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiSearch.id, managedIdentityPrincipalId, searchIndexDataReaderRoleId)
  scope: aiSearch
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', searchIndexDataReaderRoleId)
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = modelDeploymentName
output openAIName string = openAI.name
output searchEndpoint string = 'https://${aiSearch.name}.search.windows.net'
output searchName string = aiSearch.name

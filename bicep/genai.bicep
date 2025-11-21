// Azure OpenAI and Cognitive Search deployment
@description('Location for all resources')
param location string = 'uksouth'

@description('Environment suffix for resource naming')
param environmentSuffix string

@description('Managed Identity Principal ID for role assignments')
param managedIdentityPrincipalId string

// Variables
var openAIName = 'oai-expensemgmt-${environmentSuffix}'
var searchName = 'srch-expensemgmt-${environmentSuffix}'
var openAILocation = 'swedencentral' // GPT-4o is available in Sweden
var openAIModelName = 'gpt-4o'
var openAIDeploymentName = 'gpt-4o'

// Azure OpenAI Service
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIName
  location: openAILocation
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIName
    publicNetworkAccess: 'Enabled'
  }
}

// Deploy GPT-4o model
resource openAIDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: openAIDeploymentName
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: openAIModelName
      version: '2024-08-06'
    }
  }
}

// Azure Cognitive Search (for RAG)
resource search 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Role assignment: Cognitive Services OpenAI User for Managed Identity
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, 'CognitiveServicesOpenAIUser')
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services OpenAI User
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role assignment: Search Index Data Contributor for Managed Identity
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(search.id, managedIdentityPrincipalId, 'SearchIndexDataContributor')
  scope: search
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7') // Search Index Data Contributor
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Outputs
output openAIName string = openAI.name
output openAIEndpoint string = openAI.properties.endpoint
output openAIModelName string = openAIDeploymentName
output searchName string = search.name
output searchEndpoint string = 'https://${search.name}.search.windows.net'

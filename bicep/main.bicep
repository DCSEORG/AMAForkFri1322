// Main Bicep file for Expense Management System deployment
targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = 'uksouth'

@description('Environment suffix for resource naming')
param environmentSuffix string = utcNow('ddHHmm')

@description('Whether to deploy GenAI resources')
param deployGenAI bool = false

@description('Azure AD Administrator Object ID (optional - if not provided, SQL will not have AD admin configured)')
param sqlAdminObjectId string = ''

@description('Azure AD Administrator Login Name (optional)')
param sqlAdminLogin string = ''

// App Service and Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    environmentSuffix: environmentSuffix
  }
}

// Azure SQL Database
module sqlDatabase 'sql.bicep' = {
  name: 'sqlDatabaseDeployment'
  params: {
    location: location
    environmentSuffix: environmentSuffix
    managedIdentityName: appService.outputs.managedIdentityName
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
    adminObjectId: sqlAdminObjectId
    adminLogin: sqlAdminLogin
  }
}

// Azure OpenAI and Cognitive Services (conditional)
module genAI 'genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: location
    environmentSuffix: environmentSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityName string = appService.outputs.managedIdentityName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output sqlServerName string = sqlDatabase.outputs.sqlServerName
output sqlDatabaseName string = sqlDatabase.outputs.sqlDatabaseName
output openAIEndpoint string = deployGenAI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genAI.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genAI.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genAI.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genAI.outputs.searchName : ''

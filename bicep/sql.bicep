// Azure SQL Database deployment with Azure AD-only authentication
@description('Location for all resources')
param location string = 'uksouth'

@description('Environment suffix for resource naming')
param environmentSuffix string

@description('Managed Identity Name')
param managedIdentityName string

@description('Managed Identity Principal ID')
param managedIdentityPrincipalId string

@description('Azure AD Administrator Object ID')
param adminObjectId string = ''

@description('Azure AD Administrator Login Name')
param adminLogin string = ''

// Variables
var sqlServerName = 'sql-expensemgmt-${environmentSuffix}'
var sqlDatabaseName = 'ExpenseManagementDB'

// Azure SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administrators: empty(adminObjectId) ? null : {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: adminLogin
      sid: adminObjectId
      tenantId: subscription().tenantId
      principalType: 'User'
    }
    publicNetworkAccess: 'Enabled'
  }
}

// Firewall rule for Azure services
resource firewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2GB
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
    readScale: 'Disabled'
  }
}

// Outputs
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

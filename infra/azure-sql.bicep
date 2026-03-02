// azure-sql.bicep - Azure SQL Server and Database

@description('Azure region for resources')
param location string

@description('Name for the SQL Server')
param sqlServerName string

@description('Name for the SQL Database')
param databaseName string = 'Northwind'

@description('Entra ID admin login (email)')
param administratorLogin string

@description('Entra ID admin object ID')
param administratorObjectId string

@description('Principal ID of the managed identity for DB access')
param managedIdentityPrincipalId string

@description('Name of the managed identity (for reference)')
param managedIdentityName string

// SQL Server with Entra ID-only authentication
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      login: administratorLogin
      sid: administratorObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
    minimalTlsVersion: '1.2'
  }
}

// Firewall rule - allow Azure services
resource firewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// SQL Database - Basic tier
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB - Basic tier max
  }
}

// Outputs
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name

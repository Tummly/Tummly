// Azure SQL for QA. Deployed as a module into its own resource group (rg-tummly-qa-uksouth),
// where Resource Mover placed the server on 22 Sep 2026 when it moved from Central US.

@description('Azure SQL region.')
param location string

@description('SQL logical server name (globally unique).')
param serverName string

@description('SQL database name.')
param databaseName string

@description('SQL admin login.')
param sqlAdminLogin string

@description('SQL admin password (meets Azure SQL complexity rules).')
@secure()
param sqlAdminPassword string

@description('SQL SKU mode: always-on DTU S0 or Free offer (GP serverless + useFreeLimit).')
@allowed(['Free', 'S0'])
param sqlSkuMode string

param tags object

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Single resource (not two conditionals) so Free↔S0 is an in-place SKU update, not delete+recreate.
resource sqlDb 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: databaseName
  location: location
  tags: tags
  sku: sqlSkuMode == 'S0'
    ? {
        name: 'S0'
        tier: 'Standard'
      }
    : {
        name: 'GP_S_Gen5'
        tier: 'GeneralPurpose'
        family: 'Gen5'
        capacity: 1
      }
  properties: sqlSkuMode == 'S0'
    ? {
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        // Standard DTU does not allow the free-offer 32 GiB size; 30 GiB is valid for S0.
        maxSizeBytes: 32212254720
        // Matches the live database after the Resource Mover move.
        requestedBackupStorageRedundancy: 'Local'
      }
    : {
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        autoPauseDelay: 60
        // Serverless min vCores (type defs often wrongly declare int)
        #disable-next-line BCP036
        minCapacity: json('0.5')
        useFreeLimit: true
        freeLimitExhaustionBehavior: 'AutoPause'
        requestedBackupStorageRedundancy: 'Local'
      }
}

output serverName string = sqlServer.name
output serverFqdn string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDb.name

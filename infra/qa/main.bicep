@description('Azure region for QA resources (UK South).')
param location string = 'uksouth'

@description('Static Web Apps region — Free SKU is not available in UK South (use eastus2 / westus2 / etc.).')
param swaLocation string = 'eastus2'

@description('Azure SQL region — this subscription cannot provision SQL in UK South; Central US works.')
param sqlLocation string = 'centralus'

@description('Environment label used in names (qa | prod).')
param environment string = 'qa'

@description('SQL admin login.')
param sqlAdminLogin string = 'tummlysqladmin'

@description('SQL admin password (meets Azure SQL complexity rules).')
@secure()
param sqlAdminPassword string

@description('SQL SKU mode: Free offer (GP serverless + useFreeLimit) or always-on DTU S0.')
@allowed(['Free', 'S0'])
param sqlSkuMode string = 'Free'

@description('Optional suffix for globally unique names (ACR, storage). Leave empty to use a short uniqueString.')
param uniqueSuffix string = ''

@description('Placeholder image until GitHub Actions pushes the real API image to ACR.')
param placeholderApiImage string = 'mcr.microsoft.com/k8se/quickstart:latest'

@description('Target port for the placeholder / API container.')
param apiTargetPort int = 80

var suffix = empty(uniqueSuffix) ? take(uniqueString(subscription().subscriptionId, resourceGroup().id, environment), 6) : uniqueSuffix

var names = {
  log: 'log-tummly-${environment}'
  swa: 'swa-tummly-${environment}'
  cae: 'cae-tummly-${environment}'
  ca: 'ca-tummly-${environment}-api'
  acr: 'acrtummly${environment}${suffix}'
  // Globally unique; include region slug when SQL cannot live in the primary RG location
  sql: 'sql-tummly-${environment}-${sqlLocation}'
  sqldb: 'sqldb-tummly-${environment}'
  st: 'sttummly${environment}${suffix}'
  id: 'id-tummly-${environment}-api'
  blobContainer: 'help-centre-attachments'
}

var tags = {
  app: 'tummly'
  env: environment
  managedBy: 'bicep'
}

// --- Observability (required by Container Apps environment) ---

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: names.log
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// --- Identity ---

resource apiIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: names.id
  location: location
  tags: tags
}

// --- Container Registry ---

resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: names.acr
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
    publicNetworkAccess: 'Enabled'
  }
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, apiIdentity.id, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '7f951dda-4ed3-4680-a7ca-43fe172d538d' // AcrPull
    )
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// --- Blob (Help Centre attachments) ---

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: names.st
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storage
  name: 'default'
}

resource attachmentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: names.blobContainer
  properties: {
    publicAccess: 'None'
  }
}

resource storageBlobDataContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storage.id, apiIdentity.id, 'StorageBlobDataContributor')
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
    )
    principalId: apiIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// --- Azure SQL ---

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: names.sql
  location: sqlLocation
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

resource sqlDbFree 'Microsoft.Sql/servers/databases@2023-08-01-preview' = if (sqlSkuMode == 'Free') {
  parent: sqlServer
  name: names.sqldb
  location: sqlLocation
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    autoPauseDelay: 60
    // Serverless min vCores (type defs often wrongly declare int)
    #disable-next-line BCP036
    minCapacity: json('0.5')
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
  }
}

resource sqlDbS0 'Microsoft.Sql/servers/databases@2023-08-01-preview' = if (sqlSkuMode == 'S0') {
  parent: sqlServer
  name: names.sqldb
  location: sqlLocation
  tags: tags
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

// --- Static Web Apps (frontend) ---

resource swa 'Microsoft.Web/staticSites@2022-03-01' = {
  name: names.swa
  location: swaLocation
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

// --- Container Apps ---

resource cae 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: names.cae
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: names.ca
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${apiIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: cae.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: acr.properties.loginServer
          identity: apiIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: apiTargetPort
        transport: 'auto'
        allowInsecure: false
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: placeholderApiImage
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          // ADR-0015: HTTP readiness budget ≥ migrate retries (~150s); failureThreshold max 10.
          probes: [
            {
              type: 'Startup'
              httpGet: {
                path: '/health/ready'
                port: apiTargetPort
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 20
              timeoutSeconds: 5
              failureThreshold: 10
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/health/ready'
                port: apiTargetPort
                scheme: 'HTTP'
              }
              initialDelaySeconds: 10
              periodSeconds: 20
              timeoutSeconds: 5
              failureThreshold: 10
            }
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: apiTargetPort
                scheme: 'HTTP'
              }
              initialDelaySeconds: 30
              periodSeconds: 30
              timeoutSeconds: 5
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  dependsOn: [
    acrPull
  ]
}

// --- Outputs (for DNS handoff + app settings) ---

output resourceGroupName string = resourceGroup().name
output location string = location
output staticWebAppName string = swa.name
output staticWebAppDefaultHostname string = swa.properties.defaultHostname
output containerAppsEnvironmentName string = cae.name
output containerAppName string = api.name
output containerAppFqdn string = api.properties.configuration.ingress.fqdn
output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer
output sqlServerName string = sqlServer.name
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = names.sqldb
output storageAccountName string = storage.name
output blobContainerName string = attachmentsContainer.name
output apiIdentityName string = apiIdentity.name
output apiIdentityClientId string = apiIdentity.properties.clientId
output apiIdentityPrincipalId string = apiIdentity.properties.principalId
output logAnalyticsWorkspaceName string = logAnalytics.name

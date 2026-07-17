using 'main.bicep'

param location = 'uksouth'
param swaLocation = 'westeurope'
param environment = 'qa'
param sqlAdminLogin = 'tummlysqladmin'
// sqlAdminPassword: pass at deploy time via deploy.ps1 or --parameters sqlAdminPassword=...
param sqlSkuMode = 'Free'
param uniqueSuffix = ''
param placeholderApiImage = 'mcr.microsoft.com/k8se/quickstart:latest'
param apiTargetPort = 80

#Requires -Version 7
<#
.SYNOPSIS
  Create rg-tummly-qa and deploy infra/qa/main.bicep (UK South).

.EXAMPLE
  .\deploy.ps1 -SqlAdminPassword 'YourStrong!Passw0rd'
#>
param(
  [Parameter(Mandatory = $true)]
  [SecureString] $SqlAdminPassword,

  [string] $ResourceGroup = 'rg-tummly-qa',
  [string] $Location = 'uksouth',
  [ValidateSet('Free', 'S0')]
  [string] $SqlSkuMode = 'S0',
  [string] $SubscriptionId = '',
  [string] $DeploymentName = "tummly-qa-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
)

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

function Assert-AzCli {
  $az = Get-Command az -ErrorAction SilentlyContinue
  if (-not $az) {
    $fallback = Join-Path ${env:ProgramFiles} 'Microsoft SDKs\Azure\CLI2\wbin\az.cmd'
    if (Test-Path $fallback) {
      Set-Alias -Name az -Value $fallback -Scope Script
      return
    }
    throw 'Azure CLI (az) not found. Install: https://learn.microsoft.com/cli/azure/install-azure-cli-windows then re-run az login.'
  }
}

Assert-AzCli

$account = az account show -o json 2>$null | ConvertFrom-Json
if (-not $account) {
  Write-Host 'Not logged in — starting az login...'
  az login | Out-Null
  $account = az account show -o json | ConvertFrom-Json
}

if ($SubscriptionId) {
  az account set --subscription $SubscriptionId | Out-Null
  $account = az account show -o json | ConvertFrom-Json
}

Write-Host "Subscription: $($account.name) ($($account.id))"
Write-Host "Resource group: $ResourceGroup ($Location)"

az group create --name $ResourceGroup --location $Location --tags app=tummly env=qa managedBy=bicep | Out-Null

$plain = [System.Net.NetworkCredential]::new('', $SqlAdminPassword).Password

az deployment group create `
  --name $DeploymentName `
  --resource-group $ResourceGroup `
  --template-file (Join-Path $here 'main.bicep') `
  --parameters `
    location=$Location `
    swaLocation=eastus2 `
    sqlLocation=centralus `
    environment=qa `
    sqlAdminLogin=tummlysqladmin `
    sqlAdminPassword=$plain `
    sqlSkuMode=$SqlSkuMode `
    placeholderApiImage='mcr.microsoft.com/k8se/quickstart:latest' `
    apiTargetPort=80 `
  --query properties.outputs `
  --output json

Write-Host ''
Write-Host 'Done. Save outputs for DNS handoff (ticket 11) and app settings (ticket 12).'
Write-Host 'SWA deployment token: az staticwebapp secrets list -n swa-tummly-qa -g rg-tummly-qa --query properties.apiKey -o tsv'
Write-Host 'Note: API container is a placeholder image until GitHub Actions (ticket 10) pushes the real Dockerfile build; then set target port 8080.'

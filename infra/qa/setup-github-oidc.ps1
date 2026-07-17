#Requires -Version 7
<#
.SYNOPSIS
  Create Entra app + federated credential for GitHub Actions on branch qa,
  grant RBAC on rg-tummly-qa / ACR, and print GitHub secrets to set.

.EXAMPLE
  .\setup-github-oidc.ps1
#>
param(
  [string] $ResourceGroup = 'rg-tummly-qa',
  [string] $AcrName = 'acrtummlyqavfavue',
  [string] $AppDisplayName = 'sp-tummly-qa-github',
  [string] $GitHubOrg = 'Tummly',
  [string] $GitHubRepo = 'Tummly',
  [string] $Branch = 'qa'
)

$ErrorActionPreference = 'Stop'
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
  throw 'Azure CLI (az) required. Run az login first.'
}

$account = az account show -o json | ConvertFrom-Json
$subscriptionId = $account.id
$tenantId = $account.tenantId

Write-Host "Subscription: $($account.name) ($subscriptionId)"

$existing = az ad app list --display-name $AppDisplayName -o json | ConvertFrom-Json
if ($existing -and $existing.Count -ge 1) {
  $appId = $existing[0].appId
  $objectId = $existing[0].id
  Write-Host "Reusing Entra app $AppDisplayName ($appId)"
}
else {
  $app = az ad app create --display-name $AppDisplayName -o json | ConvertFrom-Json
  $appId = $app.appId
  $objectId = $app.id
  Write-Host "Created Entra app $AppDisplayName ($appId)"
  az ad sp create --id $appId | Out-Null
}

$sp = az ad sp show --id $appId -o json | ConvertFrom-Json
$spObjectId = $sp.id

# Branch subject — matches workflows that do not use a GitHub Environment
# (Environment secrets require repo admin; org may block Environment creation.)
$subject = "repo:${GitHubOrg}/${GitHubRepo}:ref:refs/heads/${Branch}"
$credName = 'github-qa-branch'
$creds = az ad app federated-credential list --id $objectId -o json | ConvertFrom-Json
if (-not ($creds | Where-Object { $_.name -eq $credName })) {
  $credJson = @{
    name        = $credName
    issuer      = 'https://token.actions.githubusercontent.com'
    subject     = $subject
    audiences   = @('api://AzureADTokenExchange')
    description = "GitHub Actions branch $Branch"
  } | ConvertTo-Json -Compress
  $tmp = [System.IO.Path]::GetTempFileName()
  Set-Content -Path $tmp -Value $credJson -Encoding utf8
  az ad app federated-credential create --id $objectId --parameters "@$tmp" | Out-Null
  Remove-Item $tmp -Force
  Write-Host "Federated credential: $subject"
}
else {
  Write-Host "Federated credential already present: $credName"
}

$rgId = az group show -n $ResourceGroup --query id -o tsv
az role assignment create --assignee-object-id $spObjectId --assignee-principal-type ServicePrincipal --role Contributor --scope $rgId 2>$null | Out-Null
$acrId = az acr show -n $AcrName -g $ResourceGroup --query id -o tsv
az role assignment create --assignee-object-id $spObjectId --assignee-principal-type ServicePrincipal --role AcrPush --scope $acrId 2>$null | Out-Null

Write-Host ''
Write-Host '=== GitHub repository secrets / variables ==='
Write-Host "AZURE_CLIENT_ID       = $appId"
Write-Host "AZURE_TENANT_ID       = $tenantId"
Write-Host "AZURE_SUBSCRIPTION_ID = $subscriptionId"
Write-Host "AZURE_STATIC_WEB_APPS_API_TOKEN = (az staticwebapp secrets list -n swa-tummly-qa -g $ResourceGroup --query properties.apiKey -o tsv)"
Write-Host "Variable VITE_API_BASE_URL = https://ca-tummly-qa-api....azurecontainerapps.io/api"
Write-Host ''
Write-Host "Federated subject must be exactly: $subject"

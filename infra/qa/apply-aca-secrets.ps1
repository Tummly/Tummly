#Requires -Version 7
<#
.SYNOPSIS
  Apply infra/qa/secrets.qa.env to ca-tummly-qa-api (same keys as Railway).

.EXAMPLE
  Copy-Item .\secrets.qa.env.example .\secrets.qa.env
  # Edit secrets.qa.env — COPY from Railway, REPLACE Azure-only lines
  .\apply-aca-secrets.ps1
#>
param(
  [string] $EnvFile = (Join-Path $PSScriptRoot 'secrets.qa.env'),
  [string] $ResourceGroup = 'rg-tummly-qa',
  [string] $ContainerApp = 'ca-tummly-qa-api',
  [string] $ApiManagedIdentityClientId = '55d29a4d-e60d-44ab-9aab-9bb69ee0ab08'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $EnvFile)) {
  throw "Missing $EnvFile — copy secrets.qa.env.example and fill from Railway (see comments)."
}

$pairs = [System.Collections.Generic.List[string]]::new()
$pairs.Add("ASPNETCORE_URLS=http://+:8080")
$pairs.Add("AZURE_CLIENT_ID=$ApiManagedIdentityClientId")

Get-Content $EnvFile | ForEach-Object {
  $line = $_.Trim()
  if (-not $line -or $line.StartsWith('#')) { return }
  $eq = $line.IndexOf('=')
  if ($eq -lt 1) { return }
  $key = $line.Substring(0, $eq).Trim()
  $val = $line.Substring($eq + 1)
  if ($key -in @('ASPNETCORE_URLS', 'AZURE_CLIENT_ID')) { return }
  if ([string]::IsNullOrWhiteSpace($val) -or $val -match '^REPLACE') {
    Write-Warning "Skipping unset/placeholder: $key"
    return
  }
  # az --set-env-vars treats space as separator; quote values that need it
  $pairs.Add("$key=$val")
}

Write-Host "Applying $($pairs.Count) env vars to $ContainerApp ..."
az containerapp update `
  --name $ContainerApp `
  --resource-group $ResourceGroup `
  --set-env-vars @pairs `
  --query "{image:properties.template.containers[0].image, latestReady:properties.latestReadyRevisionName, runningStatus:properties.runningStatus}" `
  --output json

Write-Host ''
Write-Host 'Done. Probe: https://ca-tummly-qa-api.agreeablewater-62e50314.uksouth.azurecontainerapps.io/health/ready'

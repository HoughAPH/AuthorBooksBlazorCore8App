<#
.SYNOPSIS
  One-time local setup: writes the API's secrets into .NET user secrets (outside the repo).

.DESCRIPTION
  Generates a random Jwt:Key and an admin account for local development. Existing secrets are never
  overwritten, so the script is safe to re-run. Nothing secret is written to a tracked file.

  Usage:  pwsh ./scripts/setup.ps1  [-AdminEmail admin@example.com]
          (Windows PowerShell 5.1 also works: powershell -File scripts/setup.ps1)
#>
param(
    [string]$AdminEmail = 'admin@example.com'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\IdentityManagerServerApi'

function New-RandomSecret([int]$Bytes) {
    $buffer = New-Object byte[] $Bytes
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($buffer)
    [Convert]::ToBase64String($buffer)
}

function New-RandomPassword {
    # Satisfies the Identity policy (upper, lower, digit, 8+ chars); avoids look-alike characters.
    $upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'; $lower = 'abcdefghijkmnopqrstuvwxyz'; $digit = '23456789'
    $all = $upper + $lower + $digit
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    function Pick([string]$set) {
        $b = New-Object byte[] 4; $rng.GetBytes($b)
        $set[[int]([BitConverter]::ToUInt32($b, 0) % $set.Length)]
    }
    $chars = @((Pick $upper), (Pick $lower), (Pick $digit))
    1..13 | ForEach-Object { $chars += (Pick $all) }
    -join ($chars | Sort-Object { Get-Random })
}

$existing = @{}
foreach ($line in (dotnet user-secrets list --project $project)) {
    if ($line -match '^(.+?) = ') { $existing[$Matches[1]] = $true }
}

$newAdminPassword = $null
$wanted = [ordered]@{
    'Jwt:Key'            = { New-RandomSecret 64 }
    'Seed:AdminEmail'    = { $AdminEmail }
    'Seed:AdminPassword' = { $script:newAdminPassword = New-RandomPassword; $script:newAdminPassword }
}

foreach ($key in $wanted.Keys) {
    if ($existing.ContainsKey($key)) {
        Write-Host "  kept     $key (already set)"
        continue
    }
    $value = & $wanted[$key]
    dotnet user-secrets set $key $value --project $project | Out-Null
    Write-Host "  created  $key"
}

Write-Host ''
if ($newAdminPassword) {
    Write-Host 'Admin login for the Blazor app (shown once; stored in user secrets):'
    Write-Host "  Email:    $(if ($existing.ContainsKey('Seed:AdminEmail')) { '(see Seed:AdminEmail in user secrets)' } else { $AdminEmail })"
    Write-Host "  Password: $newAdminPassword"
} else {
    Write-Host 'Admin credentials already configured. View them with:'
    Write-Host "  dotnet user-secrets list --project IdentityManagerServerApi"
}
Write-Host ''
Write-Host 'Next: dotnet run --project IdentityManagerServerApi --launch-profile https'
Write-Host '      dotnet run --project BlazorWebAssemblyApp'

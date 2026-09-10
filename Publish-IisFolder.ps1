[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Destination,

    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'TicketVoucherSystemApp\TicketVoucherSystemApp.csproj'

New-Item -ItemType Directory -Path $Destination -Force | Out-Null
dotnet publish $project --configuration $Configuration --output $Destination

if ($LASTEXITCODE -ne 0) {
    throw 'The IIS publish failed.'
}

Write-Host "IIS publish folder is ready: $Destination"

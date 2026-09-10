[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PhysicalPath,

    [Parameter(Mandatory)]
    [string] $ConnectionString,

    [string] $SiteName = 'Ticket Voucher System',
    [string] $AppPoolName = 'TicketVoucherSystem',
    [int] $Port = 8080,
    [string] $AdminEmail = ''
)

$ErrorActionPreference = 'Stop'
Import-Module WebAdministration

$resolvedPath = (Resolve-Path $PhysicalPath).Path

if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) {
    New-WebAppPool -Name $AppPoolName | Out-Null
}

Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ''
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value ApplicationPoolIdentity
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name startMode -Value AlwaysRunning

if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName -PhysicalPath $resolvedPath -Port $Port -ApplicationPool $AppPoolName | Out-Null
} else {
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $resolvedPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName
}

$appCmd = Join-Path $env:windir 'System32\inetsrv\appcmd.exe'

function Set-AppPoolEnvironmentVariable([string] $Name, [string] $Value) {
    & $appCmd set config -section:system.applicationHost/applicationPools "/-[name='$AppPoolName'].environmentVariables.[name='$Name']" /commit:apphost 2>$null | Out-Null
    & $appCmd set config -section:system.applicationHost/applicationPools "/+[name='$AppPoolName'].environmentVariables.[name='$Name',value='$Value']" /commit:apphost | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not configure IIS environment variable '$Name'."
    }
}

Set-AppPoolEnvironmentVariable 'ASPNETCORE_ENVIRONMENT' 'Production'
Set-AppPoolEnvironmentVariable 'ConnectionStrings__DefaultConnection' $ConnectionString
if (-not [string]::IsNullOrWhiteSpace($AdminEmail)) {
    Set-AppPoolEnvironmentVariable 'Admin__Email' $AdminEmail
}

& icacls $resolvedPath /grant "IIS AppPool\${AppPoolName}:(OI)(CI)RX" /T | Out-Null
Restart-WebAppPool -Name $AppPoolName
Start-Website -Name $SiteName

Write-Host "IIS site '$SiteName' is running on port $Port."

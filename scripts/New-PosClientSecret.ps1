# Run locally in PowerShell 7. Keep the displayed secret private.
$posSecret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
$posHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($posSecret)))
Write-Output "Secret (give securely to this POS client): $posSecret"
Write-Output "SecretHash (configure in the API): $posHash"

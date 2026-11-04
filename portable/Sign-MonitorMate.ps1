param(
    [Parameter(Mandatory = $true)]
    [string]$Thumbprint,
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot 'MonitorMate.exe'
if (-not (Test-Path $exe)) {
    throw "MonitorMate.exe was not found next to this script."
}

$cert = Get-ChildItem "Cert:\CurrentUser\My\$Thumbprint" -ErrorAction SilentlyContinue
if (-not $cert -or -not $cert.HasPrivateKey) {
    throw "No code-signing certificate with private key was found for thumbprint $Thumbprint. Import the PFX first."
}

$signature = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert `
    -HashAlgorithm SHA256 -TimestampServer $TimestampUrl
$check = Get-AuthenticodeSignature $exe

$check | Format-List Status, StatusMessage, @{ n = 'Signer'; e = { $_.SignerCertificate.Subject } }
if ($check.Status -ne 'Valid') {
    throw "The signature is not valid. The certificate chain must be trusted on the target PC."
}

Write-Host "Signed: $exe"
Write-Host "SmartScreen reputation can still take time for a new publisher. An EV certificate gives immediate reputation."

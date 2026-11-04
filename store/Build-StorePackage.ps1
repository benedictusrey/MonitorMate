param(
    [string]$CertificateThumbprint = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$buildRoot = Join-Path $PSScriptRoot ('package-build-' + [Guid]::NewGuid().ToString('N'))
$payload = Join-Path $buildRoot 'payload'
$msix = Join-Path $buildRoot 'MonitorMate.msix'
$releaseMsix = Join-Path $PSScriptRoot 'MonitorMate.msix'
[IO.Directory]::CreateDirectory($payload) | Out-Null

try {

dotnet publish (Join-Path $root 'src\MonitorMate\MonitorMate.csproj') `
    -c Release -r win-x64 --self-contained true `
    -p:UseAppHost=true -p:PublishSingleFile=false `
    -p:DebugType=None -p:DebugSymbols=false `
    -o $payload
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }

# The Store package launches MonitorMate.exe directly, not the PowerShell launcher.
Remove-Item (Join-Path $payload 'Launch-MonitorMate.ps1') -Force -ErrorAction SilentlyContinue
Get-ChildItem $payload -Filter '*.pdb' -ErrorAction SilentlyContinue | Remove-Item -Force

Copy-Item (Join-Path $PSScriptRoot 'AppxManifest.xml') $payload
Copy-Item (Join-Path $PSScriptRoot 'Assets') $payload -Recurse

$makeappx = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
    -Recurse -Filter MakeAppx.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -like '*\x64\*' } |
    Sort-Object FullName -Descending |
    Select-Object -First 1
if (-not $makeappx) { throw 'MakeAppx.exe was not found in the Windows SDK.' }

& $makeappx.FullName pack /d $payload /p $msix /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx failed with exit code $LASTEXITCODE." }

if ($CertificateThumbprint) {
    $signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" `
        -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -like '*\x64\*' } |
        Sort-Object FullName -Descending |
        Select-Object -First 1
    if (-not $signtool) { throw 'signtool.exe was not found in the Windows SDK.' }

    & $signtool.FullName sign /sha1 $CertificateThumbprint /fd SHA256 `
        /tr http://timestamp.digicert.com /td SHA256 $msix
    if ($LASTEXITCODE -ne 0) { throw "signtool failed with exit code $LASTEXITCODE." }
}

Move-Item -LiteralPath $msix -Destination $releaseMsix -Force
Write-Host "Package: $releaseMsix"
Write-Host 'Replace Identity Name/Publisher with Partner Center values before uploading.'
}
finally {
    $resolvedBuild = (Resolve-Path -LiteralPath $buildRoot).Path
    $resolvedStore = (Resolve-Path -LiteralPath $PSScriptRoot).Path
    if (-not $resolvedBuild.StartsWith($resolvedStore + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to remove a staging directory outside the Store folder.'
    }
    Remove-Item -LiteralPath $resolvedBuild -Recurse -Force
}


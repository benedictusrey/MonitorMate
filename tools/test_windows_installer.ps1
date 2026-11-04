# Run only on a disposable GitHub Actions Windows runner.
$ErrorActionPreference = 'Stop'
if ($env:CI -ne 'true') { throw 'This installation check is restricted to CI.' }
$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\MonitorMate'
$dataDirectory = Join-Path $env:APPDATA 'MonitorMate'
$marker = Join-Path $dataDirectory 'installer-preserve-test.txt'
if ((Test-Path -LiteralPath $installDirectory) -or (Test-Path -LiteralPath $marker)) { throw 'Installer fixture is not fresh.' }
New-Item -ItemType Directory -Path $dataDirectory -Force | Out-Null
[System.IO.File]::WriteAllText($marker, 'preserve this data')
$installer = Join-Path $PSScriptRoot '..\artifacts\MonitorMate-1.0.0-windows-x64.exe'
$mutex = New-Object System.Threading.Mutex($true, 'Local\MonitorMate.SingleInstance')
try {
    $blocked = Start-Process -FilePath $installer -ArgumentList '/S' -WindowStyle Hidden -Wait -PassThru
    if ($blocked.ExitCode -eq 0 -or (Test-Path -LiteralPath $installDirectory)) { throw 'Running-app install guard failed.' }
} finally { $mutex.ReleaseMutex(); $mutex.Dispose() }
$install = Start-Process -FilePath $installer -ArgumentList '/S' -WindowStyle Hidden -Wait -PassThru
if ($install.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $installDirectory 'MonitorMate.exe'))) { throw 'Silent install failed.' }
if (!(Test-Path -LiteralPath 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\MonitorMate')) { throw 'Uninstall registration missing.' }
$uninstall = Start-Process -FilePath (Join-Path $installDirectory 'Uninstall.exe') -ArgumentList '/S' -WindowStyle Hidden -Wait -PassThru
if ($uninstall.ExitCode -ne 0) { throw 'Silent uninstall failed.' }
Start-Sleep -Seconds 2
if (Test-Path -LiteralPath (Join-Path $installDirectory 'MonitorMate.exe')) { throw 'Uninstall left the app payload.' }
if ([System.IO.File]::ReadAllText($marker) -ne 'preserve this data') { throw 'Uninstall removed local data.' }
Remove-Item -LiteralPath $marker
Write-Output 'PASS running-app guard, per-user installation, uninstall registration, and data preservation.'

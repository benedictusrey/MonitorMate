$ErrorActionPreference = 'Stop'

$source = Split-Path -Parent $MyInvocation.MyCommand.Path
if (Test-Path (Join-Path $source 'MonitorMate')) {
    $source = Join-Path $source 'MonitorMate'
}
$target = Join-Path $env:LOCALAPPDATA 'Programs\MonitorMate'
$dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$runtimes = & $dotnet --list-runtimes
if (-not ($runtimes -match 'Microsoft\.WindowsDesktop\.App 10\.')) {
    throw "MonitorMate needs the .NET 10 Desktop Runtime. Install it from https://dotnet.microsoft.com/download/dotnet/10.0 and run this installer again."
}

# Close only an installed copy, never unrelated dotnet processes.
$runningCopies = @(Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction Stop |
    Where-Object { $_.CommandLine -and $_.CommandLine.IndexOf("$target\MonitorMate.dll", [StringComparison]::OrdinalIgnoreCase) -ge 0 })
if ($runningCopies.Count -gt 0) {
    throw 'Exit MonitorMate from the tray before running the installer so it can restore your display normally.'
}

New-Item -ItemType Directory -Force -Path $target | Out-Null
Get-ChildItem -Path $source -File |
    Where-Object { $_.Name -notmatch '^(Install|README|Uninstall)' } |
    Copy-Item -Destination $target -Force
Get-ChildItem -Path $target -File | Unblock-File

$powershell = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$launcher = Join-Path $target 'Launch-MonitorMate.ps1'
$icon = Join-Path $target 'MonitorMate.ico'
$arguments = "-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$launcher`""

$shell = New-Object -ComObject WScript.Shell
$shortcuts = @(
    (Join-Path ([Environment]::GetFolderPath('Desktop')) 'MonitorMate.lnk'),
    (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\MonitorMate.lnk')
)
foreach ($path in $shortcuts) {
    $shortcut = $shell.CreateShortcut($path)
    $shortcut.TargetPath = $powershell
    $shortcut.Arguments = $arguments
    $shortcut.WorkingDirectory = $target
    $shortcut.IconLocation = "$icon,0"
    $shortcut.Description = 'MonitorMate'
    $shortcut.WindowStyle = 1
    $shortcut.Save()
}

Write-Host "MonitorMate installed to $target"
Write-Host 'Starting MonitorMate...'
Start-Process -FilePath $shortcuts[0]

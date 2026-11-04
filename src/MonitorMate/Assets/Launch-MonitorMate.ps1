param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Extra)

$dotnet = (Get-Command dotnet.exe -ErrorAction Stop).Source
$dll = Join-Path $PSScriptRoot 'MonitorMate.dll'
$arguments = '"' + $dll + '"'
if ($Extra) { $arguments += ' ' + ($Extra -join ' ') }

# No console window, but a normal WPF window for the child process.
$start = New-Object System.Diagnostics.ProcessStartInfo
$start.FileName = $dotnet
$start.Arguments = $arguments
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.WindowStyle = 'Normal'
$start.WorkingDirectory = $PSScriptRoot
[System.Diagnostics.Process]::Start($start) | Out-Null

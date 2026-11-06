using System.Diagnostics;
using MonitorMate.Core.Services;

internal static class Program
{
    public static int Main()
    {
        // An inert sibling launcher reproduces the self-contained package layout.
        var launcher = Path.Combine(AppContext.BaseDirectory, "Launch-MonitorMate.ps1");
        File.WriteAllText(launcher, "exit 0\n");
        var startup = AppLaunch.StartupCommand();
        var restart = AppLaunch.CreateRestartStartInfo();
        var failures = 0;
        if (!AppLaunch.IsDotnetHost)
        {
            if (startup != $"\"{Environment.ProcessPath}\" --tray")
            { failures++; Console.WriteLine("FAIL Native apphost startup depends on the PowerShell/.NET launcher."); }
            if (restart.FileName != Environment.ProcessPath)
            { failures++; Console.WriteLine("FAIL Native apphost restart depends on the PowerShell/.NET launcher."); }
        }
        else if (!startup.Contains("-File") || !startup.Contains(launcher) || restart.FileName.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
        { failures++; Console.WriteLine("FAIL DLL-hosted launch lost its hidden launcher."); }
        if (failures == 0) Console.WriteLine("PASS Startup and restart use the correct native or DLL host without launching another process.");
        return failures == 0 ? 0 : 1;
    }
}

MonitorMate 1.0.0 - local desktop payload

GitHub preview preparation. No expiration or timed trial was added.
Public runnable release gates remain pending. The source uses MIT.

Build a payload first; the clean public source bundle contains delivery scripts,
not precompiled application files. See docs/INSTALLATION.md for the publish command.
The desktop build needs the .NET 10 Desktop Runtime x64.

Exit a running copy normally before Install MonitorMate.cmd. The script installer
can force-stop an installed dotnet instance, bypassing gamma cleanup. It creates
shortcuts but does not register a Windows Apps uninstaller.

The managed launcher is not a signature or a warning-free guarantee.
See README.md, SUPPORT.md, docs/AUDIT.md and docs/SIGNING.md at the project root.

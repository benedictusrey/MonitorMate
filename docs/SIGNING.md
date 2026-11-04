# Signing and SmartScreen

Reviewed 4 November 2026. Code signing identifies a publisher and protects file
integrity. A valid signature alone does not guarantee SmartScreen reputation.
Microsoft states that EV certificates no longer grant an automatic bypass.
See [Microsoft's SmartScreen guidance](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation).

## Delivery variants

| Variant | Entry point | Runtime | Current evidence |
|---|---|---|---|
| Desktop `dist` | PowerShell launcher starts `dotnet MonitorMate.dll` | .NET 10 Desktop Runtime | Local files and launcher source inspected. |
| Portable | `MonitorMate.exe` and supporting files | .NET 10 Desktop Runtime | Local executable exists; not a single-file release. |
| Store | MSIX launches `MonitorMate.exe` | Script requests self-contained publish | Local package exists; placeholder identity and no packaged launch test. |

The desktop project sets `UseAppHost=false`. This avoids generating a default
apphost; it does not sign the DLL or scripts. A signed Microsoft host does not
transfer publisher identity or trust to MonitorMate. Historical warning-free
launch on one machine does not predict downloaded-file behavior elsewhere.

## Signing a portable executable

Use a certificate or signing service appropriate to the publisher. With an
already imported certificate and Windows SDK signing tools available:

```powershell
.\portable\Sign-MonitorMate.ps1 -Thumbprint <thumbprint>
Get-AuthenticodeSignature .\portable\MonitorMate.exe
```

Inspect the signer, chain, timestamp and signature status. Test the actual
downloaded artifact on a clean target. Neither OV nor EV should be described as
an immediate warning-removal guarantee. Never include private signing keys in a release.

## MSIX and Store

Use [the Store checklist](../store/RELEASE_CHECKLIST.md). Replace placeholder
identity values before packaging. A local unsigned MSIX is not proof of Store
acceptance. Store-distributed packages follow Microsoft's signing/distribution
process; sideloaded packages have separate trust requirements.

Do not disable Windows protection or silently install trust roots to make a
release appear ready. Self-signed certificates belong only in an explicitly
managed test environment. No trust-store changes were made by this audit.


## Preview and commercial plans

The proposed GitHub preview has no timed trial. A future paid Store package does
not change current signature evidence or remove the need to verify downloaded
artifacts. No key, certificate, trusted root or Partner Center setting was changed.
See [release model](RELEASE_MODEL.md) and [GitHub checklist](GITHUB_PREPARATION.md).

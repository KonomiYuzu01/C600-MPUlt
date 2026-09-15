# Magic 600 Cell — Windows runtime prerequisites

This is a companion dependency package for the MPUlt-based Windows application. **It is not Magic 600 Cell 0.4 or a replacement application download.** Install the application separately from its release. The existing published app may still be named C600 Studio.

## Installation order

1. Extract the entire ZIP to a local folder.
2. On Windows 10 / Windows 11 up to 25H2, right-click **Install-DotNet35.cmd → Run as administrator**. It enables the Windows `NetFx3` feature, including .NET 2.0 and 3.0, using Windows Update when needed. Restart if requested.
3. On Windows 11 26H1 (build 28000) or later, the same entry opens Microsoft's version-specific standalone installation instructions. Download and run the installer provided there; it is not included in this ZIP.
4. Run **Install-DirectX.cmd**. The original Microsoft package first asks for an extraction folder. Choose an empty folder; then open that folder and run **DXSETUP.exe**, reviewing Microsoft's terms. Extraction alone does not install DirectX.
5. Run **Check-Runtime.cmd**, then start Magic 600 Cell and verify the actual display and input.

The DirectX payload is complete and usable offline. **.NET 3.5 is not a universal offline payload**: older Windows releases use an OS feature, and offline installation requires matching Windows installation media. Do not copy a random `sources/sxs` folder from another Windows version. The helper does not bypass Windows Update policy or restart the computer automatically.

## Exact compatibility boundary

- The current application launcher/engine targets **x64 Windows**. Its NativeHost runs **x86 .NET Framework 4.x**, with `useLegacyV2RuntimeActivationPolicy="true"`. Windows 10/11 normally include a newer compatible .NET 4.x runtime; .NET 3.5 does not replace it.
- Preserved MPUlt code targets legacy CLR `v2.0.50727`. This package includes the .NET 3.5 installation path for that legacy dependency chain. A clean-machine test proving that NetFx3 is unnecessary has not been performed.
- Required assemblies: `Microsoft.DirectX`, `Microsoft.DirectX.Direct3D`, and `Microsoft.DirectX.Direct3DX`, each **1.0.2902.0**, neutral culture, token **31bf3856ad364e35**. The checker also uses the application's tested SHA-256 allowlist. `Direct3DX 1.0.2911.0` is not an automatic substitute.
- The June 2010 runtime supplies legacy optional components, including Managed DirectX 1.1. It does not replace the DirectX 11/12 core shipped with Windows. A GPU's advertised DirectX version does not prove these assemblies are installed.
- The checker only reads known system installation folders and registry entries. It does not copy DLLs, alter the GAC, open a graphics device, or inspect personal sessions. Application-local caches are outside this checker’s scope.
- ARM64 Windows, Windows Server, clean-machine installation, and GPU rendering are **not validated by this package**. A passing prerequisite check does not approve G1/G2 or establish performance on Legion hardware.

## Installation failures

For NetFx3 errors, consult the Microsoft installation/repair instructions below. A managed computer may require its administrator to configure Windows Update or supply matching installation media. DISM code 3010 means a restart is required. Other nonzero codes are not reported as success.

If Managed DirectX still fails the checker, confirm DXSETUP actually ran after .NET installation/restart. If installed files have an unrecognized hash, retain the report for compatibility investigation; do not substitute downloaded DLLs or disable the application's verification.

## Provenance and verification

`redist/directx_Jun2010_redist.exe` is Microsoft's unmodified installer, version 9.29.1974.1, published July 15, 2024. Build verification checks SHA-256 **053f76dcbb28802e23341b6a787e3b0791c0fa5c8d4d011b1044172dbf89c73b** and a valid Microsoft Authenticode signature. `manifest.json` and `SHA256SUMS.txt` describe the packaged files. These checks establish package integrity, not successful installation or native rendering on a clean system.

Runtime requirements are taken from the application's `native/NativeHost.exe.config`, `native/directx_runtime.py`, and `DIRECTX.md` at source commit `5e1d35de792ccc8db896b8abf74ff2b1b00e0749`. No application, saved puzzle, credential, or development experiment is included.

Official sources:

- [DirectX June 2010 download](https://www.microsoft.com/en-us/download/details.aspx?id=8109)
- [DirectX deployment guidance](https://learn.microsoft.com/en-us/windows/win32/dxtecharts/directx-setup-for-game-developers)
- [.NET 3.5 on Windows 11, including 26H1](https://learn.microsoft.com/en-us/dotnet/framework/install/dotnet-35-windows-11)
- [.NET 3.5 installation errors and matching offline media](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/dotnet-framework-35-installation-error)

# Managed DirectX prerequisite

Install Microsoft's [DirectX End-User Runtimes (June 2010)](https://www.microsoft.com/en-us/download/details.aspx?id=8109) if Managed DirectX is missing. Use the official package and its installer, which presents Microsoft's terms. C600 Studio does not redistribute Microsoft's DirectX DLLs or install them during puzzle startup.

The native host uses these strong-named assemblies:

| Assembly | Version | Public key token |
|---|---|---|
| Microsoft.DirectX | 1.0.2902.0 | 31bf3856ad364e35 |
| Microsoft.DirectX.Direct3D | 1.0.2902.0 | 31bf3856ad364e35 |
| Microsoft.DirectX.Direct3DX | 1.0.2902.0 | 31bf3856ad364e35 |

All use neutral culture. A newer Direct3DX assembly such as 1.0.2911.0 is a different identity; it should not be substituted automatically. Different serviced files can share the correct assembly identity, so a runtime compatibility check is still required.

Microsoft documents Managed DirectX as an optional legacy component, separately from the DirectX core shipped with Windows. See [Microsoft's deployment guidance](https://learn.microsoft.com/en-us/windows/win32/dxtecharts/directx-setup-for-game-developers). The C600 MIT license does not apply to Microsoft components.

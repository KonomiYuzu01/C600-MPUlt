# Dependencies

The Windows portable build consists of a launcher, a frozen 64-bit Python state engine and a precompiled 32-bit .NET Framework native host. Python and a compiler are needed only when building from source.

The native viewport also needs compatible MPUlt and **Managed DirectX 1.1**. Microsoft's official [DirectX End-User Runtimes (June 2010)](https://www.microsoft.com/en-us/download/details.aspx?id=8109) contains these legacy optional components. A modern Windows DirectX version alone does not establish that Managed DirectX is installed. Microsoft's [deployment documentation](https://learn.microsoft.com/en-us/windows/win32/dxtecharts/directx-setup-for-game-developers) describes using DirectSetup and the component cabinet files; Microsoft retains the applicable license.

The public release excludes DirectX DLLs and the installer. Install the official runtime if it is missing; the launcher locates the required installed assemblies. See [DIRECTX.md](DIRECTX.md) for exact identities. Ordinary puzzle startup does not install dependencies or request administrator privileges.

Source requirements are listed in `requirements.txt`; the frozen release records exact build versions. Runtime computation requires NumPy. SciPy is needed only for the optional offline asset builder, which additionally consumes the retained reference and geometry input packages. Checked-in generated assets are sufficient to run the application; do not imply that `build_assets.py` can regenerate them from this repository alone.

The full original asset manifest is kept unchanged. Its historical geometry-scope description predates later native bridge tests; changing that manifest would change the model identity. Current bridge validation happens against all 259,800 slots and 1,200 generators when the native host connects.

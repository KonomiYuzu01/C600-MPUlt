# Third-party notices

The top-level MIT license applies to the C600 additions. It does not relicense third-party code, binaries, trademarks or documents.

| Component | Notice and distribution scope |
|---|---|
| MPUlt | Original work by Andrey Astrelin. Keep this primary attribution and `licenses/MPUlt-MIT.txt`, including the unchanged Melinda Green copyright line, for source covered by that upstream MIT notice. Record the provenance of the exact runtime build separately. |
| CPython | The portable engine includes CPython runtime components. Preserve the complete license supplied with that exact Python distribution, including incorporated-software notices. |
| NumPy | Preserve the installed wheel's complete main license and recorded component notices, whether the wheel uses a `licenses` directory or a root `.dist-info/LICENSE.txt`. Its main license includes OpenBLAS, LAPACK and compiler-runtime terms; retaining only a short NumPy BSD notice is insufficient. The 0.3 build uses NumPy 2.3.5 and OpenBLAS 0.3.30 from the bundled build runtime; the OpenBLAS binary carries its distributor's Authenticode signature and is not claimed to be byte-identical to the unsigned upstream wheel. |
| PyInstaller | Build tool and bundled bootloader. Its exception allows distributing generated applications under a chosen license compatible with their dependencies. The upstream license says credit and inclusion of its license are not required for generated applications; a complete copy is retained here for transparency. |
| Inno Setup | Optional installer builder by Jordan Russell, with portions by Martijn Laan. Retain its embedded copyright and website notices. Its license does not require a product-documentation acknowledgement; a complete notice is retained voluntarily. |
| Microsoft Managed DirectX | External Microsoft proprietary runtime dependency, separate from MPUlt and C600 licensing. This release does not include its DLLs or installer. Obtain it through Microsoft's official redistributable or use an existing licensed installation; see `DIRECTX.md`. |
| .NET Framework | Windows runtime dependency obtained from Microsoft; not redistributed as source by this repository. |

No private strategy PDF, conversation, screenshot, personal puzzle log, session database, authentication token or raw machine diagnostic bundle is part of the public source distribution.

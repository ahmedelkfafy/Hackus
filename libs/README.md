# libs/

This folder contains third-party DLL references required to build the project.

## Required Files

Place the following DLL files in this directory before building:

- `HandyControl.dll`
- `Ionic.Zip.dll`
- `MailBee.NET.dll`

These DLLs are referenced by the project via relative `<HintPath>` entries in the `.csproj` file.

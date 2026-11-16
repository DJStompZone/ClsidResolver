# CLSID Resolver

A small Windows utility for resolving a CLSID to information stored in the system State Repository database (`StateRepository-Machine.srd`). It creates a Volume Shadow Copy (VSS), extracts the locked SRD file, and scans the SQLite contents for matching CLSID values.

This project is useful for Windows internals research, troubleshooting, and identifying whether a CLSID corresponds to a stored task entry or a hardcoded internal system value.

## What it does

- Accepts a CLSID as a command-line argument
- Creates a VSS snapshot of the system drive
- Copies the protected SRD database out of the shadow volume
- Queries SQLite tables for the provided GUID
- Prints any matches or reports that the value appears to be a hardcoded internal system task

## Why this project exists

On Windows, some CLSIDs are stored in the `StateRepository-Machine.srd` database under the AppRepository state store. This utility is intended to help inspect those records without directly opening the locked live database file.

It is especially useful for:

- Reverse engineering Windows task registration data
- Investigating CLSIDs discovered in app or system logs
- Checking whether a CLSID is stored in the system repository or is instead a built-in internal value

## Requirements

- Windows 10 or Windows 11
- .NET 10 SDK or higher
- Access to Windows Volume Shadow Copy Service (VSS)
- Local administrator privileges

## Build and run

From the project root:

```powershell
dotnet restore
dotnet build
```

Run the utility:

```powershell
dotnet run -- "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}"
```

Or run the built executable directly:

```powershell
.\bin\Debug\net10.0\ClsidResolver.exe "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}"
```

## Usage

```text
CLSID Resolver Utility
----------------------
Usage: ClsidResolver.exe <CLSID> [--DumpSchema]
Example: ClsidResolver.exe "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}"
Example: ClsidResolver.exe "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}" --DumpSchema
```

## Debug / troubleshooting output

The utility supports a runtime switch, `--DumpSchema`, to print the SRD database schema while scanning for the requested CLSID. This is useful when troubleshooting missing matches, unexpected table layouts, or schema drift across Windows builds.

The schema output is opt-in; it is only included when the flag is present. This keeps normal lookups concise while still making the schema available when needed for diagnosis.

Example:

```powershell
ClsidResolver.exe "{ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}" --DumpSchema
```

This prints the schema and then continues with the normal CLSID lookup flow.

## Example output

```text
Attempting to crack SRD for CLSID: {ABCD1234-6969-0420-BEEF-1C0FFEEC0DED}

Result:
CLSID found in State Repository DB!
[Match found in table: ...]
  -> MATCH in [Name]: ...
```

If no record is found, the program returns:

```text
CLSID not found anywhere in State Repository. It is a hardcoded internal UBPM system task.
```

## How it works

The application does the following internally:

1. Parses the CLSID from the command-line arguments.
2. Uses PowerShell and `Win32_ShadowCopy` to create a VSS snapshot of `C:\`.
3. Opens the shadow-copy path to the SRD database at:
   `\ProgramData\Microsoft\Windows\AppRepository\StateRepository-Machine.srd`
4. Copies the database to a temporary file.
5. Opens the SQLite database and scans all tables for columns containing the CLSID.
6. Prints any matches and cleans up the temporary file afterward.

## Project structure

- `ClsidResolver/Program.cs` — CLI entry point and usage text
- `ClsidResolver/ClsidResolver.cs` — SRD extraction and SQLite search logic
- `ClsidResolver/ClsidResolver.csproj` — .NET project definition and dependencies

## Notes

- This tool relies on system internals and Windows-specific APIs.
- It is intended for troubleshooting and research scenarios.
- Some systems may block or restrict shadow-copy creation depending on policy, security settings, or permissions.
- Output may vary depending on Windows version and the state of the local repository database.

## License

MIT License. See the [LICENSE](LICENSE) file for details.

## Disclaimer

This project interacts with protected Windows system state and should be used only in authorized environments for legitimate troubleshooting, forensic analysis, or research purposes.

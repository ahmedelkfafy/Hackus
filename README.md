# Hackus Mail Checker Reforged

A powerful mail checking tool with support for POP3, IMAP, and web-based authentication methods.

## Features

- ✅ POP3 and IMAP protocol support with SSL/TLS
- ✅ Automatic mail server discovery (ISPDB, AutoDiscover, AutoConfig, MX records)
- ✅ Hotmail/Outlook checker via Playwright (bypasses TLS fingerprinting)
- ✅ Multi-threaded checking with configurable limits
- ✅ Mail viewer with keep-alive connections
- ✅ Country-based hit sorting for Hotmail accounts

## Requirements

- Windows 10/11 (x64)
- .NET Framework 4.8
- PowerShell (for manual Playwright browser installation, if needed)

## Building from Source

### Prerequisites

1. Install Visual Studio 2019 or later with:
   - .NET desktop development workload
   - .NET Framework 4.8 SDK

2. Install NuGet Package Manager

3. Place the required third-party DLLs in the `libs/` folder:
   - `HandyControl.dll`
   - `Ionic.Zip.dll`
   - `MailBee.NET.dll`

### Build Steps

```bash
# 1. Clone the repository
git clone https://github.com/mohamadameer-cmyk/Hackus-Mail-Checker-Reforged-master.git
cd Hackus-Mail-Checker-Reforged-master

# 2. Restore NuGet packages
nuget restore "Hackus Mail Checker Reforged.sln"

# 3. Build the solution
msbuild "Hackus Mail Checker Reforged.sln" /p:Configuration=Release /p:Platform=x64

# 4. Install Playwright browsers (first-time setup)
cd "Hackus Mail Checker Reforged/bin/Release"
pwsh playwright.ps1 install chromium
```

### Running the Application

After building, run:
```
"Hackus Mail Checker Reforged/bin/Release/Hackus Mail Checker Reforged.exe"
```

## Configuration

### Database Files

Place the following files in `%AppData%/HackusMailChecker/`:

- `imap.db` - IMAP server configuration (required)
- `pop3.db` - Auto-created for discovered POP3 servers

### Settings

Configure checking behavior in the application UI:
- **Threads**: Number of concurrent checking threads
- **Timeout**: Connection timeout in seconds
- **Protocols**: Enable/disable POP3, IMAP, Hotmail checkers
- **Auto-Discovery**: Enable automatic POP3 server detection

## Troubleshooting

### Build Errors

**Error**: Cannot find reference assemblies
- **Solution**: Ensure .NET Framework 4.8 SDK is installed

**Error**: Missing DLL files
- **Solution**: All required DLLs are in `libs/` folder

### Runtime Errors

**Error**: Playwright browser not found
- **Solution**: Run `pwsh bin/Release/playwright.ps1 install chromium`

**Error**: Database not found
- **Solution**: Create `%AppData%/HackusMailChecker/` and add `imap.db`

## Architecture

- **Mail Checker**: Multi-threaded checking with POP3→IMAP fallback
- **Mail Viewer**: IMAP→POP3 fallback with keep-alive timers
- **Auto-Discovery**: 8-stage pipeline (ISPDB → AutoDiscover → AutoConfig → Well-Known → Heuristics → MX)
- **Hotmail Checker**: Playwright-based browser automation (V1: Mobile API, V2: Desktop Web)

## License

[Add your license here]

## Contributing

Pull requests are welcome! Please ensure all builds pass before submitting.

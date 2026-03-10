# GoMuot for Windows

Windows preview for a lightweight Vietnamese input method focused on `Simple Telex`.

> GoMuot is currently the only Vietnamese input method on Windows positioned to fix Claude Code's dropped-character typing issue.

## Requirements

- Windows 10/11 (64-bit)
- .NET 8.0 Runtime
- Visual Studio 2022 (for building)
- Rust toolchain with MSVC target

## Development Setup

### 1. Install Prerequisites

```powershell
# Install Rust (if not already installed)
# Download from https://rustup.rs

# Add Windows targets
rustup target add x86_64-pc-windows-msvc

# Install .NET 8.0 SDK
# Download from https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. Run Setup Script

```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup/windows.ps1
```

### 3. Build Rust Core

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1
```

### 4. Build WPF Application

The build script publishes the WPF app and packages a zip:

```powershell
platforms\windows\publish\
platforms\windows\GoMuot-<version>-win-x64.zip
```

### 5. Optional Code Signing

The Windows build script can sign published artifacts with Authenticode before zipping them.

Use a PFX file:

```powershell
$env:GOMUOT_SIGN_PFX_PATH = "C:\certs\gomuot.pfx"
$env:GOMUOT_SIGN_PFX_PASSWORD = "your-password"
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
```

Or use a certificate already installed in the Windows certificate store:

```powershell
$env:GOMUOT_SIGN_CERT_THUMBPRINT = "YOUR_CERT_THUMBPRINT"
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
```

Optional environment variables:
- `GOMUOT_SIGNTOOL_PATH`: explicit path to `signtool.exe`
- `GOMUOT_SIGN_TIMESTAMP_URL`: RFC 3161 timestamp URL (default: `http://timestamp.digicert.com`)

Signing helps reduce SmartScreen warnings, but Windows reputation still depends on the certificate and download history.

If you only want the IDE build, open `platforms/windows/GoMuot/GoMuot.csproj` in Visual Studio.

Runtime test checklist:
- `docs/windows-runtime-checklist.md`

## Project Structure

```
platforms/windows/
├── GoMuot/
│   ├── GoMuot.csproj      # WPF project file
│   ├── App.xaml            # Application entry
│   ├── Core/
│   │   ├── RustBridge.cs   # P/Invoke FFI to Rust
│   │   ├── KeyboardHook.cs # Low-level keyboard hook
│   │   ├── KeyCodes.cs     # Virtual key constants
│   │   └── TextSender.cs   # SendInput API wrapper
│   ├── Views/
│   │   ├── TrayIcon.cs     # System tray icon
│   │   ├── OnboardingWindow# First-run wizard
│   │   └── AboutWindow     # About dialog
│   ├── Services/
│   │   └── SettingsService # Registry-based settings
│   ├── Native/
│   │   └── gomuot_core.dll# Rust core library
│   └── Resources/
│       └── Icons/          # App icons
└── README.md
```

## Architecture

### Keyboard Hook

Uses Win32 Low-Level Keyboard Hook (`SetWindowsHookEx` with `WH_KEYBOARD_LL`) for system-wide key interception. This is similar to `CGEventTap` on macOS.

### Text Insertion

Uses `SendInput` API with `KEYEVENTF_UNICODE` flag for direct Unicode character insertion. Supports:
- Backspace deletion for replacing text
- Unicode character insertion (Vietnamese diacritics)
- Injected key marking to prevent recursive processing

### Settings Storage

Settings are stored in Windows Registry at:
```
HKEY_CURRENT_USER\SOFTWARE\GoMuot
```

Auto-start is managed via:
```
HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
```

## Current UX

- Single mode: `Simple Telex`
- Hotkey toggle: `Ctrl+Space`
- Tray toggle: left click the tray icon to switch `V/E`, right click to open the menu

## License

GPL-3.0-or-later

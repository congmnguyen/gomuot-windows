# GoMuot Windows

`GoMuot` is a clean Windows-focused repo for a Vietnamese input method built with a Rust core and a WPF tray app.

Current behavior:
- single typing mode: `Simple Telex`
- `Win + Space` toggles the IME on and off
- build output: `platforms/windows/publish/GoMuot.exe`

Quick start on Windows:

```powershell
git clone https://github.com/congmnguyen/gomuot-windows.git
cd gomuot-windows
powershell -ExecutionPolicy Bypass -File scripts/setup/windows.ps1
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
.\platforms\windows\publish\GoMuot.exe
```

Useful docs:
- `docs/install-windows.md`
- `docs/windows-runtime-checklist.md`

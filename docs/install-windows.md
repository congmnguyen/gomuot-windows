# Gõ Mượt trên Windows

> Bản preview hiện tập trung vào một chế độ duy nhất: `Simple Telex`.

## Cách gõ

- `dd` -> `đ`
- `aw` -> `ă`
- `aa` -> `â`
- `ow` -> `ơ`
- `uw` -> `ư`
- `w` đứng riêng sẽ giữ nguyên là `w`

## Hotkey

- `Ctrl + Shift + Space`: bật/tắt bộ gõ
- Nhấp đúp tray icon: bật/tắt bộ gõ

## Build từ source

Yêu cầu:
- Windows 10/11 64-bit
- [Rust](https://rustup.rs/)
- .NET 8 SDK
- Visual Studio 2022 hoặc Build Tools (MSVC)

```powershell
git clone https://github.com/congmnguyen/gomuot-windows.git
cd gomuot-windows
powershell -ExecutionPolicy Bypass -File scripts/setup/windows.ps1
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
```

Kết quả:
- App publish: `platforms\windows\publish\`
- Gói zip: `platforms\windows\GoMuot-<version>-win-x64.zip`

## Theo dõi

- [Releases](https://github.com/congmnguyen/gomuot-windows/releases)
- [GitHub Issues](https://github.com/congmnguyen/gomuot-windows/issues)
- [Windows Runtime Checklist](./windows-runtime-checklist.md)

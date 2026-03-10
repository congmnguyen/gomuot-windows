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

- `Ctrl + Space`: bật/tắt bộ gõ
- Nhấp trái tray icon: đổi nhanh giữa `V` và `E`

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

Ký mã khi build để giảm cảnh báo SmartScreen:

```powershell
$env:GOMUOT_SIGN_PFX_PATH = "C:\certs\gomuot.pfx"
$env:GOMUOT_SIGN_PFX_PASSWORD = "your-password"
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
```

Bạn cũng có thể dùng certificate đã cài sẵn trong Windows store bằng `GOMUOT_SIGN_CERT_THUMBPRINT`.
Code signing giúp giảm cảnh báo, nhưng SmartScreen vẫn còn phụ thuộc vào reputation của certificate/phần mềm.

Kết quả:
- App publish: `platforms\windows\publish\`
- Gói zip: `platforms\windows\GoMuot-<version>-win-x64.zip`
- Bản publish/release là self-contained, người dùng cuối không cần cài `.NET Runtime` riêng
- Footprint idle của bản WinForms mới hiện đo thực tế khoảng `~8 MB`

## Theo dõi

- [Releases](https://github.com/congmnguyen/gomuot-windows/releases)
- [GitHub Issues](https://github.com/congmnguyen/gomuot-windows/issues)
- [Windows Runtime Checklist](./windows-runtime-checklist.md)

# GoMuot cho Windows

Bản Windows preview cho bộ gõ tiếng Việt gọn nhẹ, hiện tập trung vào `Simple Telex`.

> GoMuot hiện là bộ gõ tiếng Việt duy nhất trên Windows được định hướng để xử lý lỗi gõ bị mất chữ trong Claude Code.
>
> Bản WinForms hiện tại đo thực tế khoảng `~8 MB` RAM khi idle.

## Yêu cầu

Để chạy bản release:
- Windows 10/11 (64-bit)
- Không cần cài .NET Runtime riêng

Để build từ source:
- Visual Studio 2022 (để build)
- Rust toolchain với target MSVC
- .NET 8.0 SDK

## Thiết lập môi trường

### 1. Cài công cụ cần thiết

```powershell
# Cài Rust nếu máy chưa có
# Tải từ https://rustup.rs

# Thêm target Windows
rustup target add x86_64-pc-windows-msvc

# Cài .NET 8.0 SDK (chỉ cần khi build từ source)
# Tải từ https://dotnet.microsoft.com/download/dotnet/8.0
```

### 2. Chạy script setup

```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup/windows.ps1
```

### 3. Build Rust core

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1
```

### 4. Build app WinForms

Script build sẽ publish app WinForms self-contained và đóng gói file zip:

```powershell
platforms\windows\publish\
platforms\windows\GoMuot-<version>-win-x64.zip
```

Người dùng chạy bản trong `publish\` hoặc file zip không cần cài thêm `.NET Runtime`.

### 5. Code signing tùy chọn

Script build Windows có thể ký các artifact bằng Authenticode trước khi nén zip.

Dùng file PFX:

```powershell
$env:GOMUOT_SIGN_PFX_PATH = "C:\certs\gomuot.pfx"
$env:GOMUOT_SIGN_PFX_PASSWORD = "your-password"
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
```

Hoặc dùng certificate đã cài sẵn trong Windows certificate store:

```powershell
$env:GOMUOT_SIGN_CERT_THUMBPRINT = "YOUR_CERT_THUMBPRINT"
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
```

Biến môi trường tùy chọn:
- `GOMUOT_SIGNTOOL_PATH`: đường dẫn cụ thể tới `signtool.exe`
- `GOMUOT_SIGN_TIMESTAMP_URL`: URL timestamp RFC 3161, mặc định là `http://timestamp.digicert.com`

Code signing giúp giảm cảnh báo SmartScreen, nhưng reputation trên Windows vẫn còn phụ thuộc vào certificate và lịch sử tải xuống.

Nếu chỉ muốn build trong IDE, mở `platforms/windows/GoMuot/GoMuot.csproj` bằng Visual Studio.

Checklist test runtime:
- `docs/windows-runtime-checklist.md`

## Cấu trúc dự án

```text
platforms/windows/
├── GoMuot/
│   ├── GoMuot.csproj      # File project WinForms
│   ├── Program.cs         # Điểm vào ứng dụng
│   ├── GoMuotApplicationContext.cs
│   ├── Core/
│   │   ├── RustBridge.cs   # Cầu nối P/Invoke FFI sang Rust
│   │   ├── KeyboardHook.cs # Low-level keyboard hook
│   │   ├── KeyCodes.cs     # Hằng số virtual key
│   │   └── TextSender.cs   # Wrapper cho API SendInput
│   ├── Views/
│   │   ├── TrayIcon.cs     # Icon system tray
│   │   ├── OnboardingForm.cs # Màn hình hướng dẫn lần đầu
│   │   └── AboutForm.cs      # Hộp thoại giới thiệu
│   ├── Services/
│   │   └── SettingsService # Lưu settings bằng Registry
│   ├── Native/
│   │   └── gomuot_core.dll# Thư viện core Rust
│   └── Resources/
│       └── Icons/          # Icon ứng dụng
└── README.md
```

## Kiến trúc

### Keyboard Hook

Dùng Win32 Low-Level Keyboard Hook (`SetWindowsHookEx` với `WH_KEYBOARD_LL`) để chặn phím ở mức toàn hệ thống. Cách này tương tự `CGEventTap` trên macOS.

### Chèn văn bản

Dùng API `SendInput` với cờ `KEYEVENTF_UNICODE` để chèn ký tự Unicode trực tiếp. Hỗ trợ:
- Xóa bằng backspace để thay thế chữ cũ
- Chèn ký tự Unicode có dấu tiếng Việt
- Đánh dấu injected key để tránh xử lý lặp

### Lưu settings

Settings được lưu trong Windows Registry tại:
```text
HKEY_CURRENT_USER\SOFTWARE\GoMuot
```

Auto-start được quản lý qua:
```text
HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
```

## Trải nghiệm hiện tại

- Một chế độ: `Simple Telex`
- Hotkey bật/tắt: `Ctrl+Space`
- Tray: click trái để đổi `V/E`, click phải để mở menu

## Giấy phép

GPL-3.0-or-later

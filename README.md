# GoMuot Windows

`GoMuot` là repo bộ gõ tiếng Việt tập trung cho Windows, dùng core Rust và app tray viết bằng WPF.

> GoMuot là bộ gõ tiếng Việt duy nhất trên Windows có thể xử lý lỗi gõ bị mất chữ trong Claude Code.

Hiện tại:
- một chế độ gõ: `Simple Telex`
- `Ctrl + Space` để bật/tắt bộ gõ
- file build ra ở: `platforms/windows/publish/GoMuot.exe`

Cách chạy nhanh trên Windows:

```powershell
git clone https://github.com/congmnguyen/gomuot-windows.git
cd gomuot-windows
powershell -ExecutionPolicy Bypass -File scripts/setup/windows.ps1
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
.\platforms\windows\publish\GoMuot.exe
```

Tài liệu hữu ích:
- `docs/install-windows.md`
- `docs/windows-runtime-checklist.md`

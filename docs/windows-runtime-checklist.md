# Windows Runtime Checklist

Checklist này dùng sau khi build bằng:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean
```

Mục tiêu:
- xác nhận hook bàn phím hoạt động
- xác nhận Rust core và Windows bridge đồng bộ
- phát hiện lỗi `SendInput`, backspace-replace, shortcut, và word-boundary

## Chuẩn bị

- Chạy `GoMuot.exe` từ `platforms\windows\publish\`
- Kiểm tra app xuất hiện ở system tray
- Đảm bảo bộ gõ đang ở trạng thái `ON`
- Test ít nhất trên layout bàn phím US hoặc layout bạn dùng thực tế

## Smoke Test

- [ ] App mở được, không crash lúc startup
- [ ] Tray icon hiển thị đúng
- [ ] Bật/tắt từ tray hoạt động
- [ ] `Ctrl+Shift+Space` bật/tắt hoạt động
- [ ] Tắt bộ gõ thì chữ gõ ra giữ nguyên ASCII

## Notepad

### Simple Telex

- [ ] `dd` -> `đ`
- [ ] `aw` -> `ă`
- [ ] `aa` -> `â`
- [ ] `ow` -> `ơ`
- [ ] `uw` -> `ư`
- [ ] `w` -> `w`
- [ ] `Tieesng Vieetj` -> `Tiếng Việt`
- [ ] `hoaf` / `hoà` hoặc `hòa` theo setting modern tone
- [ ] `ko ` -> shortcut/auto-replace không làm hỏng dấu cách cuối nếu có cấu hình shortcut

### Break / Restore

- [ ] `text ` không bị biến thành tiếng Việt sai
- [ ] `expect ` không bị lỗi tone placement
- [ ] `ko.` giữ đúng dấu `.` sau từ
- [ ] `->` hoạt động đúng nếu có shortcut cấu hình
- [ ] Gõ từ có dấu rồi nhấn `Space`
- [ ] Gõ từ có dấu rồi nhấn `Enter`
- [ ] Gõ từ có dấu rồi nhấn `Tab`
- [ ] Backspace sau `Space` khôi phục lại từ trước để sửa tiếp
- [ ] Backspace sau dấu câu khôi phục đúng từ trước

## VS Code

### Editor

- [ ] Gõ Simple Telex trong editor bình thường
- [ ] Không bị nuốt ký tự khi gõ nhanh
- [ ] `Ctrl+C`, `Ctrl+V`, `Ctrl+Z`, `Ctrl+Backspace` không làm buffer hỏng
- [ ] Di chuyển bằng mũi tên rồi gõ tiếp không kéo state cũ sang vị trí mới

### Search

- [ ] Gõ trong ô Search
- [ ] Gõ trong ô Rename Symbol
- [ ] Gõ trong Command Palette

### Integrated Terminal

- [ ] Gõ tiếng Việt trong terminal
- [ ] Không phá command-line khi dùng backspace
- [ ] `Ctrl+C` trong terminal vẫn hoạt động đúng

## Chrome / Edge

- [ ] Gõ trong ô nhập text thông thường
- [ ] Gõ trong thanh địa chỉ
- [ ] Gõ trong textarea dài
- [ ] Gõ rồi chọn text bằng chuột, sau đó gõ tiếp không bị restore nhầm

## Word / Office

- [ ] Gõ tiếng Việt trong tài liệu mới
- [ ] Dấu câu sau từ tiếng Việt không bị mất
- [ ] Backspace replacement không lặp hoặc thiếu ký tự
- [ ] Gõ nhanh liên tục không bị đảo thứ tự ký tự

## Navigation / Focus Change

- [ ] Gõ một từ, click chuột sang vị trí khác, gõ tiếp không lôi buffer cũ
- [ ] Gõ một từ, dùng phím mũi tên, rồi gõ tiếp không bị replace sai chỗ
- [ ] Alt+Tab đổi app rồi quay lại không giữ state cũ
- [ ] Mở app mới và gõ ngay không crash

## Shortcut / System Keys

- [ ] `Ctrl+A`, `Ctrl+C`, `Ctrl+V`, `Ctrl+X` hoạt động bình thường
- [ ] `Alt+Tab` hoạt động bình thường
- [ ] `Ctrl+Shift+Space` bật/tắt bộ gõ đúng như mong muốn
- [ ] `Win+V` và `Win+Shift+S` không bị hook sai
- [ ] Nếu dùng AltGr/layout quốc tế, ký tự AltGr không bị chặn nhầm

## Regression Notes

Ghi lại cho mỗi lỗi:
- app đang test
- chuỗi gõ vào
- kết quả mong muốn
- kết quả thực tế
- trạng thái `ON/OFF`
- có bật/tắt bộ gõ giữa chừng không
- có dùng phím đặc biệt, chuột, hay đổi focus không

## Mức đạt tối thiểu trước khi phát hành beta

- [ ] Pass toàn bộ Smoke Test
- [ ] Pass toàn bộ Notepad
- [ ] Pass VS Code editor + search
- [ ] Pass Chrome text input + address bar
- [ ] Không có crash hoặc treo app trong 15 phút gõ thử

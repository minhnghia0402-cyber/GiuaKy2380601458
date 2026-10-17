# TOEIC Reading – App sinh viên (React Native + Expo)

SV: 2380601458 · Bài giữa kỳ

## Chạy thử trên máy tính (trình duyệt)

1. Chạy backend API: `backend/src/SV2380601458.Web.Host` → `dotnet run` (https://localhost:44311)
2. Tại thư mục `frontend`:
   ```bash
   npm install
   npx expo start --web
   ```
3. Mở http://localhost:8081, bấm F12 → biểu tượng điện thoại (Toggle device toolbar) để xem khung mobile.

Địa chỉ API mặc định là `https://localhost:44311`, có thể đổi ở màn hình đăng nhập → "Cài đặt máy chủ".

## Chạy trên điện thoại thật (tuỳ chọn)

`npx expo start` rồi quét mã QR bằng app **Expo Go**. Khi đó phải đổi địa chỉ API thành IP LAN của máy tính
và cho Web.Host lắng nghe trên mạng LAN.

## Cấu trúc

| File | Vai trò |
|---|---|
| `App.js` | Điều hướng giữa các màn hình |
| `src/api.js` | Gọi API ABP (đăng nhập, danh sách đề, bắt đầu thi, lưu đáp án, nộp bài, kết quả) |
| `src/storage.js` | Lưu phiên đăng nhập và lưu tạm đáp án trên máy |
| `src/screens/LoginScreen.js` | Đăng nhập |
| `src/screens/ExamListScreen.js` | Danh sách đề (tên đề, thời gian, số câu) + lịch sử thi |
| `src/screens/ExamIntroScreen.js` | Thông tin & quy định trước khi thi |
| `src/screens/TakeExamScreen.js` | Làm bài: đếm ngược, cảnh báo 5/1 phút, tự nộp khi hết giờ, lưu tạm, đổi/huỷ đáp án, bảng câu hỏi, xác nhận nộp bài |
| `src/screens/ResultScreen.js` | Kết quả: số câu đúng / sai / bỏ qua, theo Part, xem lại từng câu kèm gợi ý |
| `src/components/Countdown.js` | Đồng hồ đếm ngược |
| `src/components/PassageHtml*.js` | Hiển thị bài đọc (chữ, ảnh, chỗ trống) |

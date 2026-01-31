# 🎾 PCM - Vợt Thủ Phố Núi

Hệ thống quản lý và đặt sân Pickleball, kết hợp sàn thi đấu (Duels) và bảng xếp hạng (Leaderboard).  
Dự án được xây dựng với công nghệ **ASP.NET Core (Backend)** và **Flutter (Mobile/Web)**.

## 📂 Cấu trúc dự án

- **Backend/**: Mã nguồn Web API (.NET 8).
- **Mobile/**: Mã nguồn ứng dụng Mobile/Web (Flutter).

## 🚀 Hướng dẫn cài đặt & Chạy dự án

### 1. Yêu cầu hệ thống (Prerequisites)
- **.NET 8 SDK**
- **Flutter SDK**
- **SQL Server** (Express hoặc Developer Edition)
- **Visual Studio 2022** hoặc **VS Code**

### 2. Thiết lập Backend
1.  Truy cập thư mục API:
    ```bash
    cd Backend/PCM.API
    ```
2.  Cấu hình Database:
    - Mở file `appsettings.json`.
    - Chỉnh sửa `ConnectionStrings` để trỏ tới SQL Server của bạn.
3.  Chạy Database Migration (Tự động tạo Database):
    ```bash
    dotnet ef database update
    ```
4.  Khởi chạy Backend:
    ```bash
    dotnet run
    ```
    - API sẽ chạy tại: `http://localhost:5027`
    - Swagger UI: `http://localhost:5027/swagger`

### 3. Thiết lập Frontend (Mobile/Web)
1.  Truy cập thư mục Mobile:
    ```bash
    cd Mobile/pcm_mobile
    ```
2.  Cài đặt thư viện:
    ```bash
    flutter pub get
    ```
3.  Cấu hình API Endpoint:
    - Mở file `lib/config/api_config.dart`.
    - Đảm bảo `baseUrl` trỏ đúng về Backend (ví dụ `http://localhost:5027/api` hoặc IP LAN nếu test trên điện thoại).
4.  Chạy ứng dụng:
    ```bash
    flutter run -d chrome  # Chạy trên Web
    # Hoặc
    flutter run -d emulator-5554 # Chạy trên Android Emulator
    ```

## ✨ Tính năng chi tiết

### 1. 🔐 Hệ thống Tài khoản & Bảo mật
- **Đăng ký / Đăng nhập**: Xác thực qua JWT Token an toàn.
- **Tự động đăng nhập**: Lưu phiên làm việc, không bị out khi reload trang.
- **Quản lý hồ sơ**: Cập nhật thông tin cá nhân, avatar.

### 2. 📅 Đặt sân (Booking)
- **Lịch trực quan**: Hiển thị trạng thái sân theo màu sắc (✅ Trống, ❌ Đã đặt, 🔒 Của tôi).
- **Booking Flow**: Kiểm tra số dư, check trùng giờ, tính tiền tự động.
- **Recurring Booking**: Hỗ trợ đặt lịch định kỳ (Hàng tuần/Tháng) cho khách VIP.
- **Lịch sử**: Xem lại các sân đã đặt, hỗ trợ hủy sân (theo chính sách).

### 3. ⚔️ Sàn Kèo (Duel System) - *Tính năng nổi bật*
- **Thách đấu**: Tạo kèo 1v1 hoặc 2v2 với số tiền cược tùy chọn.
- **Sàn giao dịch**: Danh sách các kèo đang chờ đối thủ.
- **Quy trình chuẩn**: Tạo kèo -> Giữ tiền cọc -> Đối thủ nhận kèo -> Giữ tiền đối thủ -> Đánh xong -> Admin xác nhận -> Chia thưởng.

### 4. 💰 Ví điện tử & Thanh toán
- **Quản lý số dư**: Hiển thị tiền thật và xu trong game.
- **Nạp tiền**: Hệ thống yêu cầu nạp tiền (Demo), Admin duyệt cộng tiền.
- **Lịch sử giao dịch**: Log chi tiết dòng tiền (Nạp, Trừ tiền đặt sân, Tiền thắng/thua kèo).

### 5. 🏆 Xếp hạng & Thống kê (Leaderboard)
- **Xếp hạng thực**: Tính điểm dựa trên số trận thắng/thua và chỉ số DUPR.
- **Phân cấp (Tier)**: Chia hạng Gold, Silver, Diamond...
- **Dashboard**: Thống kê nhanh số lượng thành viên, sân bãi, trận đấu trong ngày.

### 6. 📰 Tin tức & Tiện ích khác
- **Banner/News**: Ghim tin tức quan trọng lên trang chủ.
- **Giao diện**: Responsive, hỗ trợ Dark/Light mode (tùy chỉnh).

## � Tài khoản Test (Dành cho Giảng viên chấm bài)
- **Email**: `tung@test.com` (hoặc `admin@test.com`)
- **Mật khẩu**: `Pcm@12345`
> *(Lưu ý: Tài khoản này đã được nạp sẵn tiền vào ví để test chức năng đặt sân & kèo)*


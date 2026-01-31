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

## ✨ Tính năng chính
- **Đặt sân online**: Xem lịch, chọn giờ, thanh toán ví.
- **Sàn Kèo (Duel)**: Tạo kèo thách đấu, chấp nhận/từ chối, cập nhật kết quả.
- **Bảng Xếp Hạng**: Top game thủ dựa trên điểm số DUPR/Rank.
- **Ví điện tử**: Nạp tiền, xem lịch sử giao dịch.
- **Tin tức & Sự kiện**: Cập nhật thông tin giải đấu.

## � Tài khoản Test (Dành cho Giảng viên chấm bài)
- **Email**: `tung@test.com` (hoặc `admin@test.com`)
- **Mật khẩu**: `Pcm@12345`
> *(Lưu ý: Tài khoản này đã được nạp sẵn tiền vào ví để test chức năng đặt sân & kèo)*


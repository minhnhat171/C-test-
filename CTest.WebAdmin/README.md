# CTest.WebAdmin

Web quản lý đồ án C# được dựng theo nội dung file Word về hệ thống thuyết minh tự động đa ngôn ngữ cho ẩm thực Vĩnh Khánh.

## Chức năng có sẵn
- Dashboard tổng quan
- Quản lý POI
- Quản lý Audio / TTS
- Quản lý bản dịch
- Quản lý tour
- Lịch sử sử dụng (GPS / QR)

## Công nghệ
- ASP.NET Core MVC (.NET 8)
- Bootstrap 5
- Dữ liệu mẫu in-memory bằng `AppDataService`

## Chạy project
```bash
cd CTest.WebAdmin
dotnet restore
dotnet run
```

Sau đó mở trình duyệt tại địa chỉ do ASP.NET Core trả về, thường là:
- https://localhost:xxxx
- http://localhost:xxxx

## Hướng phát triển tiếp theo
1. Thay `AppDataService` bằng Entity Framework Core + SQL Server/SQLite.
2. Thêm CRUD đầy đủ (Create/Edit/Delete).
3. Tạo đăng nhập admin.
4. Tích hợp sinh QR code thật.
5. Đồng bộ với API/mobile app hiện có trong file zip của bạn.

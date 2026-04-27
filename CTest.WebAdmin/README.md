# CTest.WebAdmin

Web quản trị cho hệ thống thuyết minh tự động ẩm thực Vĩnh Khánh.

## Chức năng chính

- Dashboard tổng quan dữ liệu nghe, thiết bị đang hoạt động và POI nổi bật.
- Quản lý POI, ảnh POI, chủ quán và trạng thái kích hoạt.
- Quản lý audio guide/TTS.
- Quản lý tour và QR.
- Theo dõi lịch sử nghe theo GPS/QR.
- Phân quyền Admin và chủ quán.

## Kiến trúc dữ liệu

- WebAdmin không còn dùng `AppDataService` seed nội bộ.
- WebAdmin đọc/ghi dữ liệu qua `VKFoodAPI`.
- App mobile đọc POI/tour từ cùng API và có snapshot offline để chạy khi mất mạng.
- Dữ liệu demo hiện nằm trong `VKFoodAPI/App_Data`.

## Chạy nhanh

```bash
dotnet run --project VKFoodAPI/VKFoodAPI.csproj
dotnet run --project CTest.WebAdmin/CTest.WebAdmin.csproj
```

Tài khoản demo nằm trong `CTest.WebAdmin/appsettings.json`.

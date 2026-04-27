# Vinh Khanh Guide - hướng dẫn nộp/demo

## Cấu hình môi trường

- Domain public hiện tại cho QR/API demo: `https://jaywalker-eaten-squishier.ngrok-free.dev/`.
- WebAdmin/API Development local: `http://localhost:5088/` khi chạy nội bộ.
- APK Debug/Release mặc định trỏ cùng API public: `https://jaywalker-eaten-squishier.ngrok-free.dev/`
- Nếu cần debug API local riêng, build app với `-p:ApiBaseUrl=http://10.0.2.2:5287/`.
- APK Release có thể truyền domain thật khi build:

```powershell
dotnet publish .\VinhKhanhGuide.App\VinhKhanhGuide.App.csproj -c Release -f net8.0-android -p:ApiBaseUrl=https://jaywalker-eaten-squishier.ngrok-free.dev/
```

Không để `localhost` trong APK release. Nếu build trên máy thật, dùng domain HTTPS public của API.

## API key admin

`AdminApi:ApiKey` trong `appsettings.json` để trống. Khi chạy thật, đặt bằng environment variable hoặc user-secrets:

```powershell
dotnet user-secrets set "AdminApi:ApiKey" "your-strong-admin-key" --project .\CTest.WebAdmin\CTest.WebAdmin.csproj
```

Development đang dùng `dev-admin-api-key-change-me` trong `appsettings.Development.json` để demo cục bộ.

## Tài khoản demo WebAdmin

- Admin: `user` / `12345678`
- Owner: `owner` / `12345678`

Owner bị chặn khi vào `/Pois` và thấy màn hình `Không có quyền truy cập`, không bị đẩy về login.

## Lệnh chạy local

```powershell
dotnet run --project .\CTest.WebAdmin\CTest.WebAdmin.csproj --launch-profile http
dotnet build .\VinhKhanhGuide.sln --no-restore
dotnet test .\VinhKhanhGuide.sln --no-restore
```

`CTest.WebAdmin` hiện host luôn các endpoint `VKFoodAPI` dưới `/api/*`, nên không cần chạy riêng project `VKFoodAPI` khi demo.

QR public được lấy từ `CTest.WebAdmin/appsettings*.json` qua `QrCode:PublicBaseUrl`; app deeplink lấy API từ `QrCode:MobileApiBaseUrl`. Không để hai giá trị này là `localhost` khi in hoặc chia sẻ QR.

Không mở `http://0.0.0.0:5088/` trong trình duyệt. `0.0.0.0` chỉ dùng cho server lắng nghe mạng LAN; trên chính máy tính hãy mở `http://localhost:5088/`.

## Lệnh publish server

```powershell
dotnet publish .\CTest.WebAdmin\CTest.WebAdmin.csproj -c Release -o .\build-output\webadmin
```

## Checklist tính năng cần demo

- WebAdmin CRUD POI, tour, QR, audio.
- WebAdmin có trang `Tài khoản` để quản lý admin, chủ cửa hàng và cập nhật người dùng app.
- App đọc POI từ API; chỉ dùng seed khi offline hoàn toàn.
- Dashboard hiển thị thiết bị active theo `DeviceId + ClientInstanceId`.
- Endpoint debug admin-only: `GET /api/analytics/active-devices/raw`.
- Owner portal có màn hình riêng; owner không được vào màn hình Admin.
- APK release dùng domain API thật, không dùng `localhost`.
- Video/log demo cần có 2 máy thật hoặc 2 emulator cùng gọi heartbeat lên cùng domain API.
Update by MinhNhat
Contribution check by minhnhat171

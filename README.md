# Vinh Khanh Guide - hướng dẫn nộp/demo

## Cấu hình môi trường

- API Development: `http://localhost:5287/`
- WebAdmin Development: `http://localhost:5088/`
- APK Debug mặc định trỏ API emulator: `http://10.0.2.2:5287/`
- APK Release phải truyền domain thật khi build:

```powershell
dotnet publish .\VinhKhanhGuide.App\VinhKhanhGuide.App.csproj -c Release -f net8.0-android -p:ApiBaseUrl=https://your-real-api-domain/
```

Không để `localhost` trong APK release. Nếu build trên máy thật, dùng domain HTTPS public của API.

## API key admin

`AdminApi:ApiKey` trong `appsettings.json` để trống. Khi chạy thật, đặt bằng environment variable hoặc user-secrets:

```powershell
dotnet user-secrets set "AdminApi:ApiKey" "your-strong-admin-key" --project .\VKFoodAPI\VKFoodAPI.csproj
dotnet user-secrets set "AdminApi:ApiKey" "your-strong-admin-key" --project .\CTest.WebAdmin\CTest.WebAdmin.csproj
```

Development đang dùng `dev-admin-api-key-change-me` trong `appsettings.Development.json` để demo cục bộ.

## Tài khoản demo WebAdmin

- Admin: `user` / `12345678`
- Owner: `owner` / `12345678`

Owner bị chặn khi vào `/Pois` và thấy màn hình `Không có quyền truy cập`, không bị đẩy về login.

## Lệnh chạy local

```powershell
dotnet run --project .\VKFoodAPI\VKFoodAPI.csproj --launch-profile http
dotnet run --project .\CTest.WebAdmin\CTest.WebAdmin.csproj --launch-profile http
dotnet build .\VinhKhanhGuide.sln --no-restore
dotnet test .\VinhKhanhGuide.sln --no-restore
```

## Lệnh publish server

```powershell
dotnet publish .\VKFoodAPI\VKFoodAPI.csproj -c Release -o .\build-output\api
dotnet publish .\CTest.WebAdmin\CTest.WebAdmin.csproj -c Release -o .\build-output\webadmin
```

## Checklist tính năng cần demo

- WebAdmin CRUD POI, tour, QR, audio.
- App đọc POI từ API; chỉ dùng seed khi offline hoàn toàn.
- Dashboard hiển thị thiết bị active theo `DeviceId + ClientInstanceId`.
- Endpoint debug admin-only: `GET /api/analytics/active-devices/raw`.
- Owner portal có màn hình riêng; owner không được vào màn hình Admin.
- APK release dùng domain API thật, không dùng `localhost`.
- Video/log demo cần có 2 máy thật hoặc 2 emulator cùng gọi heartbeat lên cùng domain API.

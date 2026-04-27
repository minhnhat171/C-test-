using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;
using Xunit;

namespace VinhKhanhGuide.Core.Tests;

public class ApiRepositoryFlowTests
{
    [Fact]
    public void ActiveDevices_SameDeviceDifferentClientInstances_AreTrackedSeparately()
    {
        using var scope = TestEnvironmentScope.Create();
        var repository = new ActiveDeviceRepository(scope.Environment);

        repository.RegisterHeartbeat(new ActiveDeviceHeartbeatRequest
        {
            DeviceId = "device-1",
            ClientInstanceId = "instance-a",
            UserCode = "guest-a",
            UserDisplayName = "Guest A",
            DevicePlatform = "Android",
            SentAtUtc = DateTimeOffset.UtcNow
        });

        var stats = repository.RegisterHeartbeat(new ActiveDeviceHeartbeatRequest
        {
            DeviceId = "device-1",
            ClientInstanceId = "instance-b",
            UserCode = "guest-b",
            UserDisplayName = "Guest B",
            DevicePlatform = "Android",
            SentAtUtc = DateTimeOffset.UtcNow
        });

        Assert.Equal(2, stats.ActiveDeviceCount);
        Assert.Contains(stats.Devices, device => device.ClientInstanceId == "instance-a");
        Assert.Contains(stats.Devices, device => device.ClientInstanceId == "instance-b");
    }

    [Fact]
    public void ActiveDevices_ZeroCoordinates_AreIgnored()
    {
        using var scope = TestEnvironmentScope.Create();
        var repository = new ActiveDeviceRepository(scope.Environment);

        var stats = repository.RegisterHeartbeat(new ActiveDeviceHeartbeatRequest
        {
            DeviceId = "device-2",
            ClientInstanceId = "instance-a",
            UserCode = "guest-a",
            DevicePlatform = "Android",
            Latitude = 0,
            Longitude = 0,
            SentAtUtc = DateTimeOffset.UtcNow
        });

        var device = Assert.Single(stats.Devices);
        Assert.Null(device.Latitude);
        Assert.Null(device.Longitude);
        Assert.Empty(stats.RoutePoints);
    }

    [Fact]
    public void ApiRepositories_CreateUpdateDelete_CoreContentFlow()
    {
        using var scope = TestEnvironmentScope.Create();
        var poiRepository = new PoiRepository(scope.Environment);
        var tourRepository = new TourRepository(scope.Environment, poiRepository);
        var qrRepository = new QrCodeRepository(scope.Environment, poiRepository, tourRepository);
        var audioRepository = new AudioGuideRepository(scope.Environment, poiRepository);

        var poi = poiRepository.Create(new PoiDto
        {
            Code = "TEST-POI-01",
            Name = "Quán kiểm thử",
            Category = "Ẩm thực",
            Address = "123 Vĩnh Khánh",
            Description = "Dữ liệu kiểm thử",
            NarrationText = "Nội dung thuyết minh kiểm thử",
            Latitude = 10.761,
            Longitude = 106.702,
            TriggerRadiusMeters = 50,
            CooldownMinutes = 5,
            Priority = 1,
            IsActive = true
        });

        poi.Name = "Quán kiểm thử đã cập nhật";
        Assert.True(poiRepository.Update(poi.Id, poi));
        Assert.Equal("Quán kiểm thử đã cập nhật", poiRepository.GetById(poi.Id)?.Name);

        var tour = tourRepository.Create(new TourDto
        {
            Code = "TEST-TOUR-01",
            Name = "Tour kiểm thử",
            Description = "Tour dùng cho kiểm thử integration",
            EstimatedMinutes = 15,
            IsActive = true,
            IsQrEnabled = true,
            PoiIds = [poi.Id]
        });
        Assert.NotNull(tourRepository.GetById(tour.Id));

        var qr = qrRepository.Create(new QrCodeItemSaveRequest
        {
            Code = "TEST-QR-01",
            TargetType = QrTargetKinds.Poi,
            TargetId = poi.Id.ToString(),
            DisplayName = poi.Name,
            Description = poi.Address,
            IsActive = true
        });
        Assert.Equal(poi.Id.ToString(), qrRepository.GetActiveByCode(qr.Code)?.TargetId);

        var audio = audioRepository.Create(new AudioGuideDto
        {
            PoiId = poi.Id,
            LanguageCode = "vi",
            VoiceType = "female",
            SourceType = "tts",
            Script = "Nội dung audio kiểm thử",
            IsPublished = true
        });
        Assert.NotNull(audioRepository.GetById(audio.Id));

        Assert.True(audioRepository.Delete(audio.Id));
        Assert.True(qrRepository.Delete(qr.Id));
        Assert.True(tourRepository.Delete(tour.Id));
        Assert.True(poiRepository.Delete(poi.Id));
    }

    private sealed class TestEnvironmentScope : IDisposable
    {
        private TestEnvironmentScope(string rootPath)
        {
            RootPath = rootPath;
            Environment = new TestHostEnvironment(rootPath);
        }

        public string RootPath { get; }
        public IHostEnvironment Environment { get; }

        public static TestEnvironmentScope Create()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "vinhkhanh-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rootPath);
            return new TestEnvironmentScope(rootPath);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    Directory.Delete(RootPath, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(contentRootPath);
        }

        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "VKFoodAPI.Tests";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}

using CTest.WebAdmin.Models;

namespace CTest.WebAdmin.Services;

public class AppDataService
{
    public List<Poi> Pois { get; } = new();
    public List<AudioGuide> AudioGuides { get; } = new();
    public List<TranslationEntry> Translations { get; } = new();
    public List<TourPlan> Tours { get; } = new();
    public List<UsageLog> UsageLogs { get; } = new();

    public AppDataService()
    {
        Seed();
    }

    private void Seed()
    {
        if (Pois.Any()) return;

        Pois.AddRange(new[]
        {
            new Poi
            {
                Id = 1,
                Name = "Ốc Oanh",
                Address = "534 Vĩnh Khánh, Quận 4, TP.HCM",
                Latitude = 10.7609,
                Longitude = 106.7003,
                RadiusInMeters = 50,
                Priority = 1,
                Description = "Điểm thuyết minh ẩm thực nổi bật tại phố ẩm thực Vĩnh Khánh.",
                MapLink = "https://maps.google.com/?q=10.7609,106.7003",
                NarrationScript = "Chào mừng bạn đến với khu ẩm thực Vĩnh Khánh. Đây là một trong những quán đông khách với nhiều món ốc đặc trưng.",
                IsActive = true
            },
            new Poi
            {
                Id = 2,
                Name = "Ốc Đào 2",
                Address = "212 Khánh Hội, Quận 4, TP.HCM",
                Latitude = 10.7588,
                Longitude = 106.7031,
                RadiusInMeters = 70,
                Priority = 2,
                Description = "POI phục vụ thuyết minh tự động bằng GPS và QR.",
                MapLink = "https://maps.google.com/?q=10.7588,106.7031",
                NarrationScript = "Quán nổi bật với thực đơn đa dạng, phù hợp cho tour khám phá ẩm thực ban đêm.",
                IsActive = true
            },
            new Poi
            {
                Id = 3,
                Name = "Bến xe buýt Khánh Hội",
                Address = "Phường Khánh Hội, Quận 4, TP.HCM",
                Latitude = 10.7612,
                Longitude = 106.6995,
                RadiusInMeters = 100,
                Priority = 3,
                Description = "Điểm QR kích hoạt nội dung, không cần GPS.",
                MapLink = "https://maps.google.com/?q=10.7612,106.6995",
                NarrationScript = "Bạn có thể quét mã QR tại điểm dừng để nghe nội dung giới thiệu ngay lập tức.",
                IsActive = true
            }
        });

        AudioGuides.AddRange(new[]
        {
            new AudioGuide { Id = 1, PoiId = 1, PoiName = "Ốc Oanh", Language = "vi-VN", VoiceType = "Female", SourceType = "TTS", ContentOrFile = "Script tiếng Việt", EstimatedSeconds = 45, IsPublished = true },
            new AudioGuide { Id = 2, PoiId = 1, PoiName = "Ốc Oanh", Language = "en-US", VoiceType = "Female", SourceType = "TTS", ContentOrFile = "English script", EstimatedSeconds = 55, IsPublished = true },
            new AudioGuide { Id = 3, PoiId = 2, PoiName = "Ốc Đào 2", Language = "vi-VN", VoiceType = "Male", SourceType = "File", ContentOrFile = "/audio/oc-dao-2.mp3", EstimatedSeconds = 60, IsPublished = false }
        });

        Translations.AddRange(new[]
        {
            new TranslationEntry { Id = 1, PoiId = 1, PoiName = "Ốc Oanh", Language = "English", Title = "Welcome to Oc Oanh", Body = "A famous seafood stop on Vinh Khanh street.", IsApproved = true },
            new TranslationEntry { Id = 2, PoiId = 2, PoiName = "Ốc Đào 2", Language = "Korean", Title = "Ốc Đào 2 소개", Body = "야간 음식 투어에 적합한 맛집입니다.", IsApproved = false },
            new TranslationEntry { Id = 3, PoiId = 3, PoiName = "Bến xe buýt Khánh Hội", Language = "Japanese", Title = "QR案内ポイント", Body = "QRコードをスキャンすると音声ガイドを再生できます。", IsApproved = true }
        });

        Tours.AddRange(new[]
        {
            new TourPlan { Id = 1, Name = "Tour Ẩm thực đêm", Description = "Lộ trình ngắn cho khách mới.", EstimatedMinutes = 60, PoiSequence = "Ốc Oanh -> Ốc Đào 2 -> Bến xe buýt Khánh Hội", IsQrEnabled = true },
            new TourPlan { Id = 2, Name = "Tour nghe bằng QR", Description = "Tập trung các điểm dừng có mã QR.", EstimatedMinutes = 30, PoiSequence = "Bến xe buýt Khánh Hội", IsQrEnabled = true }
        });

        UsageLogs.AddRange(new[]
        {
            new UsageLog { Id = 1, UserCode = "USR001", TriggerType = "GPS", PoiName = "Ốc Oanh", Language = "vi-VN", StartedAt = DateTime.Now.AddMinutes(-90), ListenSeconds = 42, Completed = true },
            new UsageLog { Id = 2, UserCode = "USR002", TriggerType = "QR", PoiName = "Bến xe buýt Khánh Hội", Language = "en-US", StartedAt = DateTime.Now.AddMinutes(-55), ListenSeconds = 25, Completed = false },
            new UsageLog { Id = 3, UserCode = "USR003", TriggerType = "GPS", PoiName = "Ốc Oanh", Language = "en-US", StartedAt = DateTime.Now.AddMinutes(-40), ListenSeconds = 50, Completed = true },
            new UsageLog { Id = 4, UserCode = "USR004", TriggerType = "GPS", PoiName = "Ốc Đào 2", Language = "vi-VN", StartedAt = DateTime.Now.AddMinutes(-20), ListenSeconds = 35, Completed = true }
        });
    }
}

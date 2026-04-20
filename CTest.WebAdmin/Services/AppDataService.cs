using CTest.WebAdmin.Models;

namespace CTest.WebAdmin.Services;

public class AppDataService
{
    private readonly WebDisplayClock _clock;

    public List<Poi> Pois { get; } = new();
    public List<AudioGuide> AudioGuides { get; } = new();
    public List<TranslationEntry> Translations { get; } = new();
    public List<TourPlan> Tours { get; } = new();
    public List<UsageLog> UsageLogs { get; } = new();

    public AppDataService(WebDisplayClock clock)
    {
        _clock = clock;
        Seed();
    }

    private void Seed()
    {
        if (Pois.Any())
        {
            return;
        }

        Pois.AddRange(new[]
        {
            new Poi
            {
                Id = 1,
                Name = "Oc Oanh",
                Address = "534 Vinh Khanh, Quan 4, TP.HCM",
                Latitude = 10.7609,
                Longitude = 106.7003,
                RadiusInMeters = 50,
                Priority = 1,
                Description = "Diem thuyet minh am thuc noi bat tai pho am thuc Vinh Khanh.",
                MapLink = "https://maps.google.com/?q=10.7609,106.7003",
                NarrationScript = "Chao mung ban den voi khu am thuc Vinh Khanh. Day la mot trong nhung quan dong khach voi nhieu mon oc dac trung.",
                IsActive = true
            },
            new Poi
            {
                Id = 2,
                Name = "Oc Dao 2",
                Address = "212 Khanh Hoi, Quan 4, TP.HCM",
                Latitude = 10.7588,
                Longitude = 106.7031,
                RadiusInMeters = 70,
                Priority = 2,
                Description = "POI phuc vu thuyet minh tu dong bang GPS va QR.",
                MapLink = "https://maps.google.com/?q=10.7588,106.7031",
                NarrationScript = "Quan noi bat voi thuc don da dang, phu hop cho tour kham pha am thuc ban dem.",
                IsActive = true
            },
            new Poi
            {
                Id = 3,
                Name = "Ben xe buyt Khanh Hoi",
                Address = "Phuong Khanh Hoi, Quan 4, TP.HCM",
                Latitude = 10.7612,
                Longitude = 106.6995,
                RadiusInMeters = 100,
                Priority = 3,
                Description = "Diem QR kich hoat noi dung, khong can GPS.",
                MapLink = "https://maps.google.com/?q=10.7612,106.6995",
                NarrationScript = "Ban co the quet ma QR tai diem dung de nghe noi dung gioi thieu ngay lap tuc.",
                IsActive = true
            }
        });

        AudioGuides.AddRange(new[]
        {
            new AudioGuide { Id = 1, PoiId = 1, PoiName = "Oc Oanh", Language = "vi-VN", VoiceType = "Female", SourceType = "TTS", ContentOrFile = "Script tieng Viet", EstimatedSeconds = 45, IsPublished = true },
            new AudioGuide { Id = 2, PoiId = 1, PoiName = "Oc Oanh", Language = "en-US", VoiceType = "Female", SourceType = "TTS", ContentOrFile = "English script", EstimatedSeconds = 55, IsPublished = true },
            new AudioGuide { Id = 3, PoiId = 2, PoiName = "Oc Dao 2", Language = "vi-VN", VoiceType = "Male", SourceType = "File", ContentOrFile = "/audio/oc-dao-2.mp3", EstimatedSeconds = 60, IsPublished = false }
        });

        Translations.AddRange(new[]
        {
            new TranslationEntry { Id = 1, PoiId = 1, PoiName = "Oc Oanh", Language = "English", Title = "Welcome to Oc Oanh", Body = "A famous seafood stop on Vinh Khanh street.", IsApproved = true },
            new TranslationEntry { Id = 2, PoiId = 2, PoiName = "Oc Dao 2", Language = "Korean", Title = "Oc Dao 2", Body = "Suitable for the night food tour.", IsApproved = false },
            new TranslationEntry { Id = 3, PoiId = 3, PoiName = "Ben xe buyt Khanh Hoi", Language = "Japanese", Title = "QR guide point", Body = "Scan the QR code to play the audio guide.", IsApproved = true }
        });

        Tours.AddRange(new[]
        {
            new TourPlan { Id = 1, Name = "Tour am thuc dem", Description = "Lo trinh ngan cho khach moi.", EstimatedMinutes = 60, PoiSequence = "Oc Oanh -> Oc Dao 2 -> Ben xe buyt Khanh Hoi", IsQrEnabled = true },
            new TourPlan { Id = 2, Name = "Tour nghe bang QR", Description = "Tap trung cac diem dung co ma QR.", EstimatedMinutes = 30, PoiSequence = "Ben xe buyt Khanh Hoi", IsQrEnabled = true }
        });

        UsageLogs.AddRange(new[]
        {
            new UsageLog { Id = 1, UserCode = "USR001", TriggerType = "GPS", PoiName = "Oc Oanh", Language = "vi-VN", StartedAt = _clock.Now.AddMinutes(-90).DateTime, ListenSeconds = 42, Completed = true },
            new UsageLog { Id = 2, UserCode = "USR002", TriggerType = "QR", PoiName = "Ben xe buyt Khanh Hoi", Language = "en-US", StartedAt = _clock.Now.AddMinutes(-55).DateTime, ListenSeconds = 25, Completed = false },
            new UsageLog { Id = 3, UserCode = "USR003", TriggerType = "GPS", PoiName = "Oc Oanh", Language = "en-US", StartedAt = _clock.Now.AddMinutes(-40).DateTime, ListenSeconds = 50, Completed = true },
            new UsageLog { Id = 4, UserCode = "USR004", TriggerType = "GPS", PoiName = "Oc Dao 2", Language = "vi-VN", StartedAt = _clock.Now.AddMinutes(-20).DateTime, ListenSeconds = 35, Completed = true }
        });
    }
}

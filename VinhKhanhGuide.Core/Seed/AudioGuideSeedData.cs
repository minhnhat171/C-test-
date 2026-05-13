using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.Core.Seed;

public static class AudioGuideSeedData
{
    public static IReadOnlyList<AudioGuideDto> CreateDefaultDtos()
    {
        return
        [
            new AudioGuideDto
            {
                Id = new Guid("22222222-2222-2222-2222-222222222201"),
                PoiId = new Guid("11111111-1111-1111-1111-111111111101"),
                PoiCode = "VK-FOOD-01",
                PoiName = "Ốc Oanh",
                LanguageCode = "vi",
                VoiceType = "female",
                SourceType = "tts",
                Script = "Bạn đang ở gần quán Ốc Oanh. Đây là một trong những quán ốc nổi tiếng nhất trên phố ẩm thực Vĩnh Khánh.",
                EstimatedSeconds = 36,
                IsPublished = true,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
            },
            new AudioGuideDto
            {
                Id = new Guid("22222222-2222-2222-2222-222222222202"),
                PoiId = new Guid("11111111-1111-1111-1111-111111111101"),
                PoiCode = "VK-FOOD-01",
                PoiName = "Ốc Oanh",
                LanguageCode = "en",
                VoiceType = "female",
                SourceType = "tts",
                Script = "You are near Oc Oanh, one of the most popular seafood spots on Vinh Khanh Food Street.",
                EstimatedSeconds = 32,
                IsPublished = true,
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            },
            new AudioGuideDto
            {
                Id = new Guid("22222222-2222-2222-2222-222222222203"),
                PoiId = new Guid("11111111-1111-1111-1111-111111111103"),
                PoiCode = "VK-FOOD-03",
                PoiName = "Ốc Vũ",
                LanguageCode = "vi",
                VoiceType = "male",
                SourceType = "file",
                FilePath = "audio/oc-vu-vi.mp3",
                EstimatedSeconds = 48,
                IsPublished = false,
                UpdatedAtUtc = DateTime.UtcNow.AddHours(-12)
            },
            new AudioGuideDto
            {
                Id = new Guid("22222222-2222-2222-2222-222222222211"),
                PoiId = new Guid("11111111-1111-1111-1111-111111111111"),
                PoiCode = "VK-FOOD-11",
                PoiName = "Ốc Phát",
                LanguageCode = "vi",
                VoiceType = "female",
                SourceType = "tts",
                Script = "Bạn đang ở gần quán Ốc Phát trên phố ẩm thực Vĩnh Khánh. Quán phục vụ theo phong cách bình dân, hợp cho nhóm bạn và gia đình muốn gọi nhiều món hải sản nóng hổi. Hãy thử nghêu hấp Thái, ốc móng tay xào rau muống, sò huyết rang me hoặc càng ghẹ rang muối.",
                EstimatedSeconds = 42,
                IsPublished = true,
                UpdatedAtUtc = DateTime.UtcNow
            }
        ];
    }
}

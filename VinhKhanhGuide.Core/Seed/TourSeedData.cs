using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.Core.Seed;

public static class TourSeedData
{
    public static IReadOnlyList<TourDto> CreateDefaultDtos()
    {
        return
        [
            new TourDto
            {
                Id = 1,
                Code = "VK-TOUR-01",
                Name = "Tour Làm Quen Vĩnh Khánh",
                Description = "Lộ trình ngắn cho khách mới, đi qua các quán nổi bật và dễ tiếp cận đầu phố.",
                EstimatedMinutes = 45,
                IsActive = true,
                IsQrEnabled = true,
                PoiIds =
                [
                    new Guid("11111111-1111-1111-1111-111111111103"),
                    new Guid("11111111-1111-1111-1111-111111111102"),
                    new Guid("11111111-1111-1111-1111-111111111104")
                ],
                UpdatedAtUtc = DateTime.UtcNow
            },
            new TourDto
            {
                Id = 2,
                Code = "VK-TOUR-02",
                Name = "Tour Hải Sản Buổi Tối",
                Description = "Chuỗi điểm dừng thiên về hải sản, phù hợp để app dẫn tuần tự theo GPS.",
                EstimatedMinutes = 70,
                IsActive = true,
                IsQrEnabled = true,
                PoiIds =
                [
                    new Guid("11111111-1111-1111-1111-111111111101"),
                    new Guid("11111111-1111-1111-1111-111111111107"),
                    new Guid("11111111-1111-1111-1111-111111111109"),
                    new Guid("11111111-1111-1111-1111-111111111108")
                ],
                UpdatedAtUtc = DateTime.UtcNow
            }
        ];
    }
}

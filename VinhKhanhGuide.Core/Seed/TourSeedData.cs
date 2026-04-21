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
                Code = "VK-TOUR-STARTER",
                Name = "Tour Làm Quen Cho Khách Mới",
                Description = "Lộ trình ngắn cho lần đầu ghé Vĩnh Khánh, ưu tiên các quán dễ chọn và dễ nghe theo nhịp.",
                EstimatedMinutes = 22,
                IsActive = true,
                IsQrEnabled = true,
                PoiIds =
                [
                    new Guid("11111111-1111-1111-1111-111111111102"),
                    new Guid("11111111-1111-1111-1111-111111111104"),
                    new Guid("11111111-1111-1111-1111-111111111109")
                ],
                UpdatedAtUtc = DateTime.UtcNow
            },
            new TourDto
            {
                Id = 2,
                Code = "VK-TOUR-OC-MINI",
                Name = "Mini Tour Món Ốc",
                Description = "Đi nhanh qua các quán ốc dễ thử nhất để khách mới cảm nhận đúng chất phố ẩm thực.",
                EstimatedMinutes = 26,
                IsActive = true,
                IsQrEnabled = true,
                PoiIds =
                [
                    new Guid("11111111-1111-1111-1111-111111111102"),
                    new Guid("11111111-1111-1111-1111-111111111104"),
                    new Guid("11111111-1111-1111-1111-111111111103")
                ],
                UpdatedAtUtc = DateTime.UtcNow
            },
            new TourDto
            {
                Id = 3,
                Code = "VK-TOUR-LAU-MINI",
                Name = "Mini Tour Lẩu Và Nướng",
                Description = "Lộ trình ngắn cho nhóm muốn chuyển từ hải sản sang lẩu và nướng trong cùng một buổi tối.",
                EstimatedMinutes = 28,
                IsActive = true,
                IsQrEnabled = true,
                PoiIds =
                [
                    new Guid("11111111-1111-1111-1111-111111111108"),
                    new Guid("11111111-1111-1111-1111-111111111109"),
                    new Guid("11111111-1111-1111-1111-111111111110")
                ],
                UpdatedAtUtc = DateTime.UtcNow
            },
            new TourDto
            {
                Id = 4,
                Code = "VK-TOUR-BO-MINI",
                Name = "Mini Tour Bò Nướng",
                Description = "Phù hợp khi bạn muốn đổi vị khỏi ốc để thử bò nướng và các quán hợp đi nhóm.",
                EstimatedMinutes = 24,
                IsActive = true,
                IsQrEnabled = true,
                PoiIds =
                [
                    new Guid("11111111-1111-1111-1111-111111111110"),
                    new Guid("11111111-1111-1111-1111-111111111109"),
                    new Guid("11111111-1111-1111-1111-111111111108")
                ],
                UpdatedAtUtc = DateTime.UtcNow
            },
            new TourDto
            {
                Id = 5,
                Code = "VK-TOUR-CUA-MINI",
                Name = "Mini Tour Món Cua",
                Description = "Nhịp tour ngắn dành cho khách muốn thử các quán dễ bắt đầu với món cua và hải sản đậm vị.",
                EstimatedMinutes = 25,
                IsActive = true,
                IsQrEnabled = true,
                PoiIds =
                [
                    new Guid("11111111-1111-1111-1111-111111111107"),
                    new Guid("11111111-1111-1111-1111-111111111104"),
                    new Guid("11111111-1111-1111-1111-111111111101")
                ],
                UpdatedAtUtc = DateTime.UtcNow
            }
        ];
    }
}

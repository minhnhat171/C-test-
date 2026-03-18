using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class PoiProvider : IPoiProvider
{
    private static readonly IReadOnlyList<POI> Pois =
    [
        new POI
        {
            Name = "Ốc Oanh",
            Description = "Một trong những quán ốc nổi tiếng nhất phố Vĩnh Khánh.",
            NarrationText = "Bạn đang đến quán Ốc Oanh, một trong những quán ốc nổi tiếng nhất phố Vĩnh Khánh.",
            Latitude = 10.75730,
            Longitude = 106.70550,
            TriggerRadiusMeters = 50,
            Priority = 8,
            CooldownMinutes = 10,
            MapLink = "https://maps.google.com/?q=10.75730,106.70550"
        },
        new POI
        {
            Name = "Phố Ẩm Thực Vĩnh Khánh",
            Description = "Khu phố ẩm thực nổi bật với các món hải sản và đồ nướng.",
            NarrationText = "Bạn đang ở gần phố ẩm thực Vĩnh Khánh. Hãy khám phá các quán hải sản và món nướng đặc trưng.",
            Latitude = 10.75768,
            Longitude = 106.70125,
            TriggerRadiusMeters = 120,
            Priority = 10,
            CooldownMinutes = 5,
            MapLink = "https://maps.google.com/?q=10.75768,106.70125"
        },
        new POI
        {
            Name = "Cầu Kênh Tẻ",
            Description = "Điểm nối giao thông chính giữa Quận 4 và Quận 7.",
            NarrationText = "Bạn đang đến gần khu vực Cầu Kênh Tẻ, một điểm kết nối giao thông quan trọng của Quận 4.",
            Latitude = 10.74493,
            Longitude = 106.70653,
            TriggerRadiusMeters = 180,
            Priority = 8,
            CooldownMinutes = 10,
            MapLink = "https://maps.google.com/?q=10.74493,106.70653"
        },
        new POI
        {
            Name = "Bến Nhà Rồng",
            Description = "Di tích lịch sử nổi tiếng gần trung tâm thành phố.",
            NarrationText = "Bến Nhà Rồng là di tích lịch sử quan trọng gắn với hành trình tìm đường cứu nước của Chủ tịch Hồ Chí Minh.",
            Latitude = 10.76912,
            Longitude = 106.70601,
            TriggerRadiusMeters = 220,
            Priority = 9,
            CooldownMinutes = 15,
            MapLink = "https://maps.google.com/?q=10.76912,106.70601"
        }
    ];

    public Task<IReadOnlyList<POI>> GetPoisAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Pois);
    }
}

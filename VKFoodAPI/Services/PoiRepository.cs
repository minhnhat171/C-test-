using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Seed;

namespace VKFoodAPI.Services;

public class PoiRepository
{
    public List<PoiDto> Pois { get; } = PoiSeedData.CreateDefaultDtos()
        .Select(dto => dto.Clone())
        .ToList();
}

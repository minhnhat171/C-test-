using System.Globalization;
using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.Core.Seed;

public static class PoiSeedData
{
    public static IReadOnlyList<PoiDto> CreateDefaultDtos()
    {
        return
        [
            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111101"),
                code: "VK-FOOD-01",
                name: "Ốc Oanh",
                imageSource: "ocoanh.png",
                address: "534 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Ốc Oanh là một trong những quán ốc nổi tiếng nhất trên phố ẩm thực Vĩnh Khánh, đông khách từ chiều đến tối.",
                specialDish: "Ốc hương xào bơ tỏi, ốc len xào dừa, sò điệp nướng mỡ hành",
                priceRange: "120.000d - 320.000d",
                openingHours: "16:30 - 23:45",
                firstDishSuggestion: "Ốc hương xào bơ tỏi",
                featuredCategories: ["oc", "cua"],
                latitude: 10.7607247,
                longitude: 106.7007223,
                radiusMeters: 48,
                priority: 10,
                viNarration: "Bạn đang ở gần quán Ốc Oanh. Đây là một trong những quán ốc nổi tiếng nhất trên phố ẩm thực Vĩnh Khánh, nổi bật với ốc hương xào bơ tỏi, ốc len xào dừa và sò điệp nướng mỡ hành."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111102"),
                code: "VK-FOOD-02",
                name: "Ốc Thảo",
                imageSource: "octhao.png",
                address: "383 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Ốc Thảo phát triển từ xe ốc nhỏ thành địa chỉ hải sản bình dân quen thuộc với thực khách quận 4.",
                specialDish: "Ốc móng tay xào rau muống, nghêu hấp sả, hàu nướng phô mai",
                priceRange: "90.000d - 240.000d",
                openingHours: "16:00 - 23:30",
                firstDishSuggestion: "Ốc móng tay xào rau muống",
                featuredCategories: ["oc"],
                latitude: 10.7616799,
                longitude: 106.7023636,
                radiusMeters: 52,
                priority: 9,
                viNarration: "Bạn đang ở gần quán Ốc Thảo. Quán nổi tiếng với hải sản tươi, giá hợp lý và các món như ốc móng tay xào rau muống, nghêu hấp sả, hàu nướng phô mai."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111103"),
                code: "VK-FOOD-03",
                name: "Ốc Vũ",
                imageSource: "ocvu.png",
                address: "37 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Ốc Vũ là quán ăn đêm lâu năm, đông khách đến khuya và mang không khí sôi động đặc trưng của phố ẩm thực quận 4.",
                specialDish: "Ốc cà na rang muối, ốc mỡ xào me, sò huyết rang me",
                priceRange: "100.000d - 260.000d",
                openingHours: "17:00 - 00:30",
                firstDishSuggestion: "Ốc cà na rang muối",
                featuredCategories: ["oc"],
                latitude: 10.7614025,
                longitude: 106.7027047,
                radiusMeters: 46,
                priority: 9,
                viNarration: "Bạn đang ở gần quán Ốc Vũ. Đây là điểm ăn khuya quen thuộc trên phố Vĩnh Khánh với các món nổi bật như ốc cà na rang muối, ốc mỡ xào me và sò huyết rang me."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111104"),
                code: "VK-FOOD-04",
                name: "Ốc Sáu Nở",
                imageSource: "ocsauno.png",
                address: "128 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Ốc Sáu Nở được nhiều thực khách nhớ đến nhờ nước chấm đậm vị và các món hải sản đầy đặn, dễ ăn.",
                specialDish: "Cua rang me, sò điệp nướng phô mai, ốc hương xào bơ",
                priceRange: "110.000d - 280.000d",
                openingHours: "16:30 - 23:30",
                firstDishSuggestion: "Cua rang me",
                featuredCategories: ["oc", "cua"],
                latitude: 10.7609643,
                longitude: 106.7029420,
                radiusMeters: 44,
                priority: 8,
                viNarration: "Bạn đang ở gần quán Ốc Sáu Nở. Quán nổi tiếng với nước chấm đậm đà cùng các món như cua rang me, sò điệp nướng phô mai và ốc hương xào bơ."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111105"),
                code: "VK-FOOD-05",
                name: "Bé Ốc",
                imageSource: "beoc.png",
                address: "58/44 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Bé Ốc là quán vỉa hè bình dân, phù hợp cho nhóm bạn thích không khí đường phố Sài Gòn và các món ốc giá mềm.",
                specialDish: "Ốc mỡ xào bơ tỏi, ốc len xào dừa, sò lông nướng",
                priceRange: "80.000d - 220.000d",
                openingHours: "16:00 - 23:00",
                firstDishSuggestion: "Ốc len xào dừa",
                featuredCategories: ["oc"],
                latitude: 10.7633886,
                longitude: 106.7020606,
                radiusMeters: 42,
                priority: 7,
                viNarration: "Bạn đang ở gần quán Bé Ốc. Đây là quán bình dân được nhiều bạn trẻ yêu thích với ốc mỡ xào bơ tỏi, ốc len xào dừa và sò lông nướng."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111106"),
                code: "VK-FOOD-06",
                name: "Ốc Ty",
                imageSource: "ocsono.png",
                address: "12 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Ốc Ty có thực đơn đa dạng, không khí trẻ trung và là một điểm hẹn nhộn nhịp của khu phố ẩm thực về đêm.",
                specialDish: "Ốc móng tay xào bơ tỏi, ốc hương rang muối, hàu nướng",
                priceRange: "95.000d - 250.000d",
                openingHours: "17:00 - 00:00",
                firstDishSuggestion: "Ốc móng tay xào bơ tỏi",
                featuredCategories: ["oc"],
                latitude: 10.7619500,
                longitude: 106.7031500,
                radiusMeters: 43,
                priority: 8,
                viNarration: "Bạn đang ở gần quán Ốc Ty. Quán có không khí trẻ trung và nhiều món hấp dẫn như ốc móng tay xào bơ tỏi, ốc hương rang muối và hàu nướng."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111107"),
                code: "VK-FOOD-07",
                name: "Ốc Hoa Kiều",
                imageSource: "thuyseafood.png",
                address: "598 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Ốc Hoa Kiều được nhiều người chọn nhờ giá khá rẻ, menu phong phú và phù hợp với nhóm bạn trẻ hoặc sinh viên.",
                specialDish: "Cua Cà Mau rang me, sò điệp nướng mỡ hành, nghêu hấp Thái",
                priceRange: "110.000d - 300.000d",
                openingHours: "16:30 - 23:30",
                firstDishSuggestion: "Cua Cà Mau rang me",
                featuredCategories: ["oc", "cua"],
                latitude: 10.7608829,
                longitude: 106.7007709,
                radiusMeters: 45,
                priority: 7,
                viNarration: "Bạn đang ở gần quán Ốc Hoa Kiều. Quán nổi bật bởi giá dễ tiếp cận, thực đơn phong phú và các món như cua Cà Mau rang me, sò điệp nướng mỡ hành, nghêu hấp Thái."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111108"),
                code: "VK-FOOD-08",
                name: "Ớt Xiêm Quán",
                imageSource: "otxiem.png",
                address: "568 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Ớt Xiêm Quán là điểm đổi vị quen thuộc với lẩu và nướng, phù hợp cho những buổi tụ họp đông vui cùng bạn bè.",
                specialDish: "Lẩu hải sản, bạch tuộc nướng, tôm nướng muối ớt",
                priceRange: "160.000d - 420.000d",
                openingHours: "15:30 - 23:30",
                firstDishSuggestion: "Lẩu hải sản",
                featuredCategories: ["lau", "cua"],
                latitude: 10.7611663,
                longitude: 106.7057009,
                radiusMeters: 58,
                priority: 8,
                viNarration: "Bạn đang ở gần Ớt Xiêm Quán. Đây là địa điểm phù hợp nếu bạn muốn đổi vị với lẩu hải sản, bạch tuộc nướng và tôm nướng muối ớt."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111109"),
                code: "VK-FOOD-09",
                name: "Chilli - L?u nu?ng",
                imageSource: "chillilau.png",
                address: "232 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Chilli - Lẩu nướng nổi bật với vị cay kiểu Thái, không khí trẻ trung và rất hợp cho các bữa ăn theo nhóm.",
                specialDish: "Lẩu kim chi hải sản, bạch tuộc nướng, tôm sốt cay",
                priceRange: "170.000d - 430.000d",
                openingHours: "16:00 - 23:45",
                firstDishSuggestion: "Lẩu kim chi hải sản",
                featuredCategories: ["lau", "bo"],
                latitude: 10.7606930,
                longitude: 106.7036324,
                radiusMeters: 56,
                priority: 8,
                viNarration: "Bạn đang ở gần quán Chilli - Lẩu nướng. Quán có phong cách cay đậm, phù hợp nếu bạn muốn đổi nhịp sang lẩu và nướng ở cuối hành trình."),

            CreatePoi(
                id: new Guid("11111111-1111-1111-1111-111111111110"),
                code: "VK-FOOD-10",
                name: "Thế Giới Bò - Nướng & Lẩu",
                imageSource: "thegioibo.png",
                address: "6 Vĩnh Khánh, P. Khánh Hội, Q.4",
                description: "Thế Giới Bò là lựa chọn thú vị khi muốn đổi từ hải sản sang các món bò nướng, lẩu bò và đồ nhắm buổi tối.",
                specialDish: "Sườn bò nướng, bò cuộn nấm, lẩu bò",
                priceRange: "189.000d - 490.000d",
                openingHours: "15:30 - 23:00",
                firstDishSuggestion: "Sườn bò nướng",
                featuredCategories: ["bo", "lau"],
                latitude: 10.7640355,
                longitude: 106.7012784,
                radiusMeters: 62,
                priority: 7,
                viNarration: "Bạn đang ở gần quán Thế Giới Bò - Nướng và Lẩu. Đây là điểm đổi vị hấp dẫn với sườn bò nướng, bò cuộn nấm và lẩu bò.")
        ];
    }

    private static PoiDto CreatePoi(
        Guid id,
        string code,
        string name,
        string imageSource,
        string address,
        string description,
        string specialDish,
        string priceRange,
        string openingHours,
        string firstDishSuggestion,
        IReadOnlyList<string> featuredCategories,
        double latitude,
        double longitude,
        double radiusMeters,
        int priority,
        string viNarration)
    {
        return new PoiDto
        {
            Id = id,
            Code = code,
            Name = name,
            Category = "Ẩm thực",
            ImageSource = imageSource,
            Address = address,
            Description = description,
            SpecialDish = specialDish,
            PriceRange = priceRange,
            OpeningHours = openingHours,
            FirstDishSuggestion = firstDishSuggestion,
            FeaturedCategories = featuredCategories
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            NarrationText = viNarration,
            Latitude = latitude,
            Longitude = longitude,
            TriggerRadiusMeters = radiusMeters,
            Priority = priority,
            CooldownMinutes = 6,
            IsActive = true,
            MapLink = $"https://maps.google.com/?q={latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}",
            AudioAssetPath = string.Empty,
            NarrationTranslations = BuildNarrations(name, address, specialDish, viNarration)
        };
    }

    private static Dictionary<string, string> BuildNarrations(
        string name,
        string address,
        string specialDish,
        string viNarration)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["vi"] = viNarration,
            ["en"] = $"You are near {name}. Address: {address}. Signature dishes: {specialDish}.",
            ["zh"] = $"????? {name}???:{address}?????:{specialDish}?",
            ["ko"] = $"지금 {name} 근처에 있습니다. 주소는 {address}이며 추천 메뉴는 {specialDish} 입니다.",
            ["fr"] = $"Vous êtes près de {name}. Adresse : {address}. Spécialités : {specialDish}."
        };
    }
}

using System.Globalization;
using System.Text;

namespace VinhKhanhGuide.Core.Models;

public class POI
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Mã định danh ổn định toàn hệ thống
    public string Code { get; set; } = string.Empty;

    // Nội dung chính
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Ẩm thực";
    public string Address { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SpecialDish { get; set; } = string.Empty;
    public string PriceRange { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public string FirstDishSuggestion { get; set; } = string.Empty;
    public List<string> FeaturedCategories { get; set; } = [];

    // Media / map / narration
    public string ImageSource { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string NarrationText { get; set; } = string.Empty;
    public string AudioAssetPath { get; set; } = string.Empty;

    // GPS / trigger
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double TriggerRadiusMeters { get; set; } = 50;
    public int Priority { get; set; } = 1;
    public int CooldownMinutes { get; set; } = 5;

    // Trạng thái
    public bool IsActive { get; set; } = true;

    // Đa ngôn ngữ
    public Dictionary<string, string> NarrationTranslations { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);
    public string GetNarrationText(string? languageCode = null)
    {
        if (!string.IsNullOrWhiteSpace(languageCode)
            && NarrationTranslations != null
            && NarrationTranslations.TryGetValue(languageCode, out var translated)
            && !string.IsNullOrWhiteSpace(translated))
        {
            if (string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase) &&
                NeedsEnglishNormalization(translated))
            {
                return BuildEnglishNarration();
            }

            return translated;
        }

        if (string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
        {
            return BuildEnglishNarration();
        }

        return NarrationText ?? string.Empty;
    }

    private string BuildEnglishNarration()
    {
        var englishName = Transliterate(Name);
        var englishAddress = NormalizeAddressForEnglish(Address);
        var englishDishes = TranslateSpecialDishesToEnglish(SpecialDish);

        return $"You are near {englishName} on Vinh Khanh Food Street. " +
               $"The address is {englishAddress}. " +
               $"Recommended dishes include {englishDishes}.";
    }

    private static bool NeedsEnglishNormalization(string translation)
    {
        return ContainsVietnameseCharacters(translation) ||
               translation.Contains(" P.", StringComparison.OrdinalIgnoreCase) ||
               translation.Contains(" Q.", StringComparison.OrdinalIgnoreCase) ||
               translation.Contains(" xào ", StringComparison.OrdinalIgnoreCase) ||
               translation.Contains(" nướng ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsVietnameseCharacters(string value)
    {
        return value.Any(character =>
            character is 'ă' or 'â' or 'đ' or 'ê' or 'ô' or 'ơ' or 'ư' or
                'Ă' or 'Â' or 'Đ' or 'Ê' or 'Ô' or 'Ơ' or 'Ư' or
                'á' or 'à' or 'ả' or 'ã' or 'ạ' or
                'ắ' or 'ằ' or 'ẳ' or 'ẵ' or 'ặ' or
                'ấ' or 'ầ' or 'ẩ' or 'ẫ' or 'ậ' or
                'é' or 'è' or 'ẻ' or 'ẽ' or 'ẹ' or
                'ế' or 'ề' or 'ể' or 'ễ' or 'ệ' or
                'í' or 'ì' or 'ỉ' or 'ĩ' or 'ị' or
                'ó' or 'ò' or 'ỏ' or 'õ' or 'ọ' or
                'ố' or 'ồ' or 'ổ' or 'ỗ' or 'ộ' or
                'ớ' or 'ờ' or 'ở' or 'ỡ' or 'ợ' or
                'ú' or 'ù' or 'ủ' or 'ũ' or 'ụ' or
                'ứ' or 'ừ' or 'ử' or 'ữ' or 'ự' or
                'ý' or 'ỳ' or 'ỷ' or 'ỹ' or 'ỵ');
    }

    private static string NormalizeAddressForEnglish(string address)
    {
        var transliterated = Transliterate(address);
        return transliterated
            .Replace("P. ", "Ward ", StringComparison.OrdinalIgnoreCase)
            .Replace("Q. ", "District ", StringComparison.OrdinalIgnoreCase);
    }

    private static string TranslateSpecialDishesToEnglish(string specialDish)
    {
        return specialDish.Trim() switch
        {
            "Ốc hương xào bơ tỏi, ốc len xào dừa, sò điệp nướng mỡ hành" =>
                "butter-garlic sea snails, coconut stir-fried snails, and scallops with scallion oil",
            "Ốc móng tay xào rau muống, nghêu hấp sả, hàu nướng phô mai" =>
                "razor clams with morning glory, lemongrass steamed clams, and cheese grilled oysters",
            "Ốc cà na rang muối, ốc mỡ xào me, sò huyết rang me" =>
                "salt roasted sea snails, tamarind stir-fried snails, and tamarind blood cockles",
            "Ốc hương xào bơ, sò điệp nướng phô mai, gân cá ngừ nướng muối" =>
                "butter stir-fried sea snails, cheese grilled scallops, and salted grilled tuna tendon",
            "Ốc mỡ xào bơ tỏi, ốc len xào dừa, sò lông nướng" =>
                "butter-garlic snails, coconut stir-fried snails, and grilled hairy scallops",
            "Ốc móng tay xào bơ tỏi, ốc hương rang muối, hàu nướng" =>
                "butter-garlic razor clams, salt roasted sea snails, and grilled oysters",
            "Sò điệp nướng mỡ hành, ốc bươu nướng tiêu, nghêu hấp Thái" =>
                "scallops with scallion oil, pepper grilled apple snails, and Thai-style steamed clams",
            "Cua Cà Mau rang me, sò điệp nướng mỡ hành, nghêu hấp Thái" =>
                "tamarind Ca Mau crab, scallops with scallion oil, and Thai-style steamed clams",
            "Cua rang me, sò điệp nướng phô mai, ốc hương xào bơ" =>
                "tamarind crab, cheese grilled scallops, and butter sea snails",
            "Lẩu hải sản, bạch tuộc nướng, tôm nướng muối ớt" =>
                "seafood hotpot, grilled octopus, and chili salt grilled shrimp",
            "Lẩu Thái hải sản, mực nướng sa tế, tôm nướng" =>
                "Thai seafood hotpot, satay grilled squid, and grilled shrimp",
            "Lẩu kim chi hải sản, bạch tuộc nướng, tôm sốt cay" =>
                "kimchi seafood hotpot, grilled octopus, and spicy shrimp",
            "Bò nướng tảng, bò cuộn nấm, lẩu bò" =>
                "grilled beef steak, mushroom rolled beef, and beef hotpot",
            "Sườn bò nướng, bò cuộn nấm, lẩu bò" =>
                "grilled beef ribs, mushroom rolled beef, and beef hotpot",
            _ => Transliterate(specialDish)
        };
    }

    private static string Transliterate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Replace('đ', 'd')
            .Replace('Đ', 'D')
            .Normalize(NormalizationForm.FormD);

        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }
}

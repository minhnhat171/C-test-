namespace VinhKhanhGuide.App.Models;

public sealed class FeaturedDishItem
{
    public string CategoryKey { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string StartingPrice { get; init; } = string.Empty;
    public string ImageSource { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string StartingPriceLabel { get; init; } = string.Empty;
}

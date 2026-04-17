namespace VinhKhanhGuide.App.Models;

public class FoodCategoryItem
{
    public string Key { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DishCount { get; set; }

    public string CountLabel => $"{DishCount} món";
}

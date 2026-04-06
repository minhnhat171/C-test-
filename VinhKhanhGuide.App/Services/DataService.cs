using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Services;

public class DataService
{
    public List<Restaurant> GetRestaurants()
    {
        return new List<Restaurant>
        {
            new Restaurant
            {
                Name = "Ốc Oanh",
                Address = "534 Vĩnh Khánh",
                Latitude = 10.7570,
                Longitude = 106.7040
            },
            new Restaurant
            {
                Name = "Ốc Thảo",
                Address = "383 Vĩnh Khánh",
                Latitude = 10.7565,
                Longitude = 106.7035
            }
        };
    }
    public List<Food> GetFoods()
    {
        return new List<Food>
    {
        new Food { Name = "Ốc nướng", Image = "food1.jpg" },
        new Food { Name = "Sò nướng", Image = "food2.jpg" },
        new Food { Name = "Hải sản", Image = "food3.jpg" }
    };
    }
}
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class RestaurantService
{
    public List<Restaurant> GetRestaurants()
    {
        return new List<Restaurant>
        {
            new Restaurant
            {
                Name = "Ốc Oanh",
                Address = "534 Vĩnh Khánh, Q4",
                Description = "Quán ốc nổi tiếng nhất phố Vĩnh Khánh",
                SpecialDish = "Ốc hương xào bơ tỏi",
                Latitude = 10.7572,
                Longitude = 106.7056
            },

            new Restaurant
            {
                Name = "Ốc Thảo",
                Address = "383 Vĩnh Khánh",
                Description = "Quán hải sản nổi tiếng với nguyên liệu tươi",
                SpecialDish = "Ốc móng tay xào rau muống",
                Latitude = 10.7568,
                Longitude = 106.7052
            },

            new Restaurant
            {
                Name = "Ốc Vũ",
                Address = "37 Vĩnh Khánh",
                Description = "Quán mở đến khuya, rất đông khách",
                SpecialDish = "Ốc cà na rang muối",
                Latitude = 10.7560,
                Longitude = 106.7048
            }
        };
    }
}
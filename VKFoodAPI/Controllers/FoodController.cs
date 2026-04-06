using Microsoft.AspNetCore.Mvc;

namespace VKFoodAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoodController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetFoods()
        {
            var foods = new List<object>
            {
                new {
                    name = "Ốc Oanh",
                    lat = 10.757,
                    lng = 106.705,
                    description = "Ốc hương xào bơ tỏi"
                },
                new {
                    name = "Ốc Thảo",
                    lat = 10.758,
                    lng = 106.706,
                    description = "Nghêu hấp sả"
                }
            };

            return Ok(foods);
        }
    }
}
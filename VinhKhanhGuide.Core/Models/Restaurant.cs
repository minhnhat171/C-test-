namespace VinhKhanhGuide.Core.Models;

public class Restaurant
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Description { get; set; } = "";
    public string SpecialDish { get; set; } = "";

    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
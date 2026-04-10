namespace CTest.WebAdmin.Models;

public class Poi
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusInMeters { get; set; }
    public int Priority { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MapLink { get; set; } = string.Empty;
    public string NarrationScript { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

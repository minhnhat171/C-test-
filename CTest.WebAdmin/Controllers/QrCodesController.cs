using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class QrCodesController : Controller
{
    private readonly AppDataService _data;

    public QrCodesController(AppDataService data)
    {
        _data = data;
    }

    public IActionResult Index(int? selectedPoiId)
    {
        var items = _data.Pois
            .OrderBy(x => x.Name)
            .Select(poi => new QrCodeItemViewModel
            {
                PoiId = poi.Id,
                PoiName = poi.Name,
                Description = poi.Description,
                ActivationType = GetActivationType(poi),
                Status = poi.IsActive ? "Sẵn sàng" : "Tạm khóa",
                Payload = $"poi://{poi.Id}?name={Uri.EscapeDataString(poi.Name)}&lat={poi.Latitude}&lng={poi.Longitude}"
            })
            .ToList();

        var selected = items.FirstOrDefault(x => x.PoiId == selectedPoiId) ?? items.FirstOrDefault();

        var vm = new QrManagementViewModel
        {
            Items = items,
            SelectedPoiId = selected?.PoiId ?? 0,
            SelectedItem = selected
        };

        return View("Manage", vm);
    }

    private static string GetActivationType(Poi poi)
    {
        return poi.Description.Contains("QR", StringComparison.OrdinalIgnoreCase) ||
               poi.NarrationScript.Contains("QR", StringComparison.OrdinalIgnoreCase)
            ? "QR + GPS"
            : "QR tại điểm";
    }
}

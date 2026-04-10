using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Controllers;

public class QrCodesController : Controller
{
    private readonly PoiApiClient _poiApiClient;

    public QrCodesController(PoiApiClient poiApiClient)
    {
        _poiApiClient = poiApiClient;
    }

    public async Task<IActionResult> Index(Guid? selectedPoiId, CancellationToken cancellationToken = default)
    {
        var vm = new QrManagementViewModel();

        try
        {
            vm.Items = (await _poiApiClient.GetPoisAsync(cancellationToken))
                .OrderBy(x => x.Name)
                .Select(poi => new QrCodeItemViewModel
                {
                    PoiId = poi.Id,
                    PoiCode = string.IsNullOrWhiteSpace(poi.Code)
                        ? $"QR-{poi.Id.ToString("N")[..8].ToUpperInvariant()}"
                        : poi.Code,
                    PoiName = poi.Name,
                    Description = poi.Description,
                    ActivationType = GetActivationType(poi),
                    Status = poi.IsActive ? "San sang" : "Tam khoa",
                    Payload = $"poi://{poi.Id}?code={Uri.EscapeDataString(poi.Code)}&name={Uri.EscapeDataString(poi.Name)}&lat={poi.Latitude}&lng={poi.Longitude}"
                })
                .ToList();

            vm.SelectedItem = vm.Items.FirstOrDefault(x => x.PoiId == selectedPoiId) ?? vm.Items.FirstOrDefault();
            vm.SelectedPoiId = vm.SelectedItem?.PoiId ?? Guid.Empty;
        }
        catch (HttpRequestException)
        {
            vm.LoadErrorMessage = "Khong the ket noi VKFoodAPI. QR se duoc tao lai khi API chay.";
        }

        return View("Manage", vm);
    }

    private static string GetActivationType(PoiDto poi)
    {
        return PoiAdminMappings.ContainsQr(poi.Description, poi.NarrationText)
            ? "QR + GPS"
            : "QR tai diem";
    }
}

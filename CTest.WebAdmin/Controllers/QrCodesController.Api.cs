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
                .Select(poi =>
                {
                    var publicUrl = BuildPublicQrUrl(poi.Id);

                    return new QrCodeItemViewModel
                    {
                        PoiId = poi.Id,
                        PoiCode = string.IsNullOrWhiteSpace(poi.Code)
                            ? $"QR-{poi.Id.ToString("N")[..8].ToUpperInvariant()}"
                            : poi.Code,
                        PoiName = poi.Name,
                        Description = poi.Description,
                        Address = poi.Address,
                        ActivationType = GetActivationType(poi),
                        Status = poi.IsActive ? "San sang" : "Tam khoa",
                        Payload = publicUrl,
                        PublicUrl = publicUrl,
                        NarrationText = poi.NarrationText,
                        MapLink = poi.MapLink,
                        SpecialDish = poi.SpecialDish
                    };
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

    [HttpGet("/qr/{id:guid}")]
    public async Task<IActionResult> Scan(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var poi = await _poiApiClient.GetPoiAsync(id, cancellationToken);
            if (poi is null)
            {
                return NotFound("Khong tim thay diem dung cho ma QR nay.");
            }

            var vm = new QrScanViewModel
            {
                PoiId = poi.Id,
                PoiCode = string.IsNullOrWhiteSpace(poi.Code)
                    ? $"QR-{poi.Id.ToString("N")[..8].ToUpperInvariant()}"
                    : poi.Code,
                PoiName = poi.Name,
                Description = poi.Description,
                Address = poi.Address,
                NarrationText = string.IsNullOrWhiteSpace(poi.NarrationText) ? poi.Description : poi.NarrationText,
                MapLink = poi.MapLink,
                SpecialDish = poi.SpecialDish,
                PublicUrl = BuildPublicQrUrl(poi.Id)
            };

            return View("Scan", vm);
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "He thong tam thoi khong ket noi duoc du lieu POI. Thu lai sau.");
        }
    }

    private static string GetActivationType(PoiDto poi)
    {
        return PoiAdminMappings.ContainsQr(poi.Description, poi.NarrationText)
            ? "QR + GPS"
            : "QR tai diem";
    }

    private string BuildPublicQrUrl(Guid poiId)
    {
        return Url.Action(
                   nameof(Scan),
                   "QrCodes",
                   new { id = poiId },
                   Request.Scheme,
                   Request.Host.Value)
               ?? $"{Request.Scheme}://{Request.Host}/qr/{poiId}";
    }
}

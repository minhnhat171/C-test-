using System.Globalization;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using QRCoder;
using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Controllers;

public class QrCodesController : Controller
{
    private readonly PoiApiClient _poiApiClient;
    private readonly AppDataService _data;
    private readonly QrCodeOptions _qrCodeOptions;

    public QrCodesController(
        PoiApiClient poiApiClient,
        AppDataService data,
        IOptions<QrCodeOptions> qrCodeOptions)
    {
        _poiApiClient = poiApiClient;
        _data = data;
        _qrCodeOptions = qrCodeOptions.Value;
    }

    public async Task<IActionResult> Index(
        string? selectedTargetType,
        string? selectedTargetId,
        Guid? selectedPoiId,
        CancellationToken cancellationToken = default)
    {
        if (selectedPoiId.HasValue && string.IsNullOrWhiteSpace(selectedTargetId))
        {
            selectedTargetType = QrTargetTypes.Poi;
            selectedTargetId = selectedPoiId.Value.ToString();
        }

        var vm = new QrManagementViewModel();
        var targets = new List<QrTargetDescriptor>();

        try
        {
            var pois = await _poiApiClient.GetPoisAsync(cancellationToken);
            targets.AddRange(pois.Select(BuildPoiTarget));
        }
        catch (HttpRequestException)
        {
            vm.LoadErrorMessage = "Khong the ket noi VKFoodAPI. QR POI se xuat hien lai khi API chay; QR tour van co san.";
        }

        targets.AddRange(_data.Tours
            .Where(tour => tour.IsQrEnabled)
            .Select(BuildTourTarget));

        vm.Items = targets
            .OrderBy(target => target.SortOrder)
            .ThenBy(target => target.TargetName)
            .Select(BuildManagementItem)
            .ToList();

        var normalizedTargetType = QrTargetTypes.Normalize(selectedTargetType);
        vm.SelectedItem = vm.Items.FirstOrDefault(item =>
                item.TargetType == normalizedTargetType &&
                item.TargetId == (selectedTargetId ?? string.Empty))
            ?? vm.Items.FirstOrDefault();

        vm.SelectedTargetType = vm.SelectedItem?.TargetType ?? string.Empty;
        vm.SelectedTargetId = vm.SelectedItem?.TargetId ?? string.Empty;

        return View("Manage", vm);
    }

    [HttpGet("/qr/{id:guid}")]
    public IActionResult LegacyPoiScan(Guid id)
    {
        return RedirectToAction(nameof(Scan), new
        {
            targetType = QrTargetTypes.Poi,
            targetId = id.ToString()
        })!;
    }

    [HttpGet("/qr/{targetType}/{targetId}")]
    public async Task<IActionResult> Scan(
        string targetType,
        string targetId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var target = await ResolveTargetAsync(targetType, targetId, cancellationToken);
            if (target is null)
            {
                return NotFound("Khong tim thay target cho ma QR nay.");
            }

            return View("Scan", BuildScanViewModel(target));
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "He thong tam thoi khong ket noi duoc du lieu target. Thu lai sau.");
        }
    }

    [HttpGet("/qr/image/{targetType}/{targetId}")]
    public async Task<IActionResult> Image(
        string targetType,
        string targetId,
        CancellationToken cancellationToken = default)
    {
        QrTargetDescriptor? target;

        try
        {
            target = await ResolveTargetAsync(targetType, targetId, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Khong tao duoc QR luc nay vi target chua truy cap duoc.");
        }

        if (target is null)
        {
            return NotFound();
        }

        var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(
            BuildPublicQrUrl(target.TargetType, target.TargetId),
            QRCodeGenerator.ECCLevel.Q);

        var qrCode = new SvgQRCode(qrData);
        var svg = qrCode.GetGraphic(
            pixelsPerModule: 12,
            darkColorHex: "#0f172a",
            lightColorHex: "#ffffff",
            drawQuietZones: true);

        return Content(svg, "image/svg+xml");
    }

    private async Task<QrTargetDescriptor?> ResolveTargetAsync(
        string targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        return QrTargetTypes.Normalize(targetType) switch
        {
            QrTargetTypes.Tour => ResolveTourTarget(targetId),
            _ => await ResolvePoiTargetAsync(targetId, cancellationToken)
        };
    }

    private async Task<QrTargetDescriptor?> ResolvePoiTargetAsync(
        string targetId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(targetId, out var poiId))
        {
            return null;
        }

        var poi = await _poiApiClient.GetPoiAsync(poiId, cancellationToken);
        return poi is null ? null : BuildPoiTarget(poi);
    }

    private QrTargetDescriptor? ResolveTourTarget(string targetId)
    {
        if (!int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId))
        {
            return null;
        }

        var tour = _data.Tours.FirstOrDefault(item => item.Id == tourId && item.IsQrEnabled);
        return tour is null ? null : BuildTourTarget(tour);
    }

    private QrCodeItemViewModel BuildManagementItem(QrTargetDescriptor target)
    {
        return new QrCodeItemViewModel
        {
            TargetType = target.TargetType,
            TargetId = target.TargetId,
            TargetCode = target.TargetCode,
            TargetName = target.TargetName,
            TargetKindLabel = target.TargetKindLabel,
            Description = target.Description,
            Address = target.Address,
            ActivationType = target.ActivationType,
            Status = target.Status,
            Payload = BuildPublicQrUrl(target.TargetType, target.TargetId),
            PublicUrl = BuildPublicQrUrl(target.TargetType, target.TargetId),
            QrSvgUrl = BuildQrSvgUrl(target.TargetType, target.TargetId),
            DetailUrl = target.DetailUrl,
            DetailActionLabel = target.DetailActionLabel,
            NarrationText = target.NarrationText,
            MapLink = target.MapLink,
            SpecialDish = target.SpecialDish,
            RouteSummary = target.RouteSummary,
            EstimatedDurationLabel = target.EstimatedDurationLabel
        };
    }

    private QrScanViewModel BuildScanViewModel(QrTargetDescriptor target)
    {
        return new QrScanViewModel
        {
            TargetType = target.TargetType,
            TargetId = target.TargetId,
            TargetCode = target.TargetCode,
            TargetName = target.TargetName,
            TargetKindLabel = target.TargetKindLabel,
            Description = target.Description,
            Address = target.Address,
            NarrationText = target.NarrationText,
            MapLink = target.MapLink,
            SpecialDish = target.SpecialDish,
            PublicUrl = BuildPublicQrUrl(target.TargetType, target.TargetId),
            EstimatedDurationLabel = target.EstimatedDurationLabel,
            TourStops = target.TourStops
        };
    }

    private QrTargetDescriptor BuildPoiTarget(PoiDto poi)
    {
        var targetId = poi.Id.ToString();

        return new QrTargetDescriptor
        {
            TargetType = QrTargetTypes.Poi,
            TargetId = targetId,
            TargetCode = string.IsNullOrWhiteSpace(poi.Code)
                ? $"POI-{poi.Id.ToString("N")[..8].ToUpperInvariant()}"
                : poi.Code,
            TargetName = poi.Name,
            TargetKindLabel = "POI",
            Description = poi.Description,
            Address = poi.Address,
            ActivationType = GetPoiActivationType(poi),
            Status = poi.IsActive ? "San sang" : "Tam khoa",
            NarrationText = string.IsNullOrWhiteSpace(poi.NarrationText) ? poi.Description : poi.NarrationText,
            MapLink = poi.MapLink,
            SpecialDish = poi.SpecialDish,
            DetailUrl = Url.Action("Details", "Pois", new { id = poi.Id }) ?? string.Empty,
            DetailActionLabel = "Xem POI",
            RouteSummary = BuildPoiRouteSummary(poi),
            EstimatedDurationLabel = $"Ban kinh kich hoat {poi.TriggerRadiusMeters:0.#} m",
            SortOrder = 0
        };
    }

    private QrTargetDescriptor BuildTourTarget(TourPlan tour)
    {
        var tourStops = SplitPoiSequence(tour.PoiSequence);
        var targetId = tour.Id.ToString(CultureInfo.InvariantCulture);

        return new QrTargetDescriptor
        {
            TargetType = QrTargetTypes.Tour,
            TargetId = targetId,
            TargetCode = $"TOUR-{tour.Id:D3}",
            TargetName = tour.Name,
            TargetKindLabel = "Tour",
            Description = tour.Description,
            Address = string.Empty,
            ActivationType = "QR dieu huong tour",
            Status = tour.IsQrEnabled ? "San sang" : "Tam khoa",
            NarrationText = BuildTourNarration(tour, tourStops),
            MapLink = string.Empty,
            SpecialDish = string.Empty,
            DetailUrl = $"{Url.Action("Index", "Tours") ?? "/Tours"}#tour-{tour.Id}",
            DetailActionLabel = "Xem tour",
            RouteSummary = tour.PoiSequence,
            EstimatedDurationLabel = $"{tour.EstimatedMinutes} phut",
            TourStops = tourStops,
            SortOrder = 1
        };
    }

    private static string GetPoiActivationType(PoiDto poi)
    {
        return PoiAdminMappings.ContainsQr(poi.Description, poi.NarrationText)
            ? "QR + GPS"
            : "QR tai diem";
    }

    private static string BuildPoiRouteSummary(PoiDto poi)
    {
        if (!string.IsNullOrWhiteSpace(poi.Address))
        {
            return poi.Address;
        }

        return $"{poi.Latitude:0.####}, {poi.Longitude:0.####}";
    }

    private static IReadOnlyList<string> SplitPoiSequence(string poiSequence)
    {
        return (poiSequence ?? string.Empty)
            .Split(["->", "=>", "\n", ","], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    private static string BuildTourNarration(TourPlan tour, IReadOnlyList<string> tourStops)
    {
        var stopSummary = tourStops.Count == 0
            ? "Tour chua co diem dung duoc khai bao."
            : $"Cac diem dung chinh: {string.Join(", ", tourStops)}.";

        return $"{tour.Description} Thoi luong du kien {tour.EstimatedMinutes} phut. {stopSummary}";
    }

    private string BuildPublicQrUrl(string targetType, string targetId)
    {
        var relativePath = Url.Action(
                               nameof(Scan),
                               "QrCodes",
                               new
                               {
                                   targetType = QrTargetTypes.Normalize(targetType),
                                   targetId
                               })
                           ?? $"/qr/{QrTargetTypes.Normalize(targetType)}/{Uri.EscapeDataString(targetId)}";

        return CombineWithPublicBase(relativePath);
    }

    private string BuildQrSvgUrl(string targetType, string targetId)
    {
        return Url.Action(
                   nameof(Image),
                   "QrCodes",
                   new
                   {
                       targetType = QrTargetTypes.Normalize(targetType),
                       targetId
                   })
               ?? $"/qr/image/{QrTargetTypes.Normalize(targetType)}/{Uri.EscapeDataString(targetId)}";
    }

    private string CombineWithPublicBase(string relativePath)
    {
        var configuredBase = _qrCodeOptions.PublicBaseUrl?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredBase))
        {
            return new Uri(new Uri(EnsureTrailingSlash(configuredBase)), relativePath).ToString();
        }

        var requestBase = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        return new Uri(new Uri(EnsureTrailingSlash(requestBase)), relativePath).ToString();
    }

    private static string EnsureTrailingSlash(string baseUrl)
    {
        return baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : $"{baseUrl}/";
    }

    private sealed class QrTargetDescriptor
    {
        public string TargetType { get; init; } = string.Empty;
        public string TargetId { get; init; } = string.Empty;
        public string TargetCode { get; init; } = string.Empty;
        public string TargetName { get; init; } = string.Empty;
        public string TargetKindLabel { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public string ActivationType { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string NarrationText { get; init; } = string.Empty;
        public string MapLink { get; init; } = string.Empty;
        public string SpecialDish { get; init; } = string.Empty;
        public string DetailUrl { get; init; } = string.Empty;
        public string DetailActionLabel { get; init; } = string.Empty;
        public string RouteSummary { get; init; } = string.Empty;
        public string EstimatedDurationLabel { get; init; } = string.Empty;
        public IReadOnlyList<string> TourStops { get; init; } = Array.Empty<string>();
        public int SortOrder { get; init; }
    }
}

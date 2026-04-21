using System.Globalization;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QRCoder;
using VinhKhanhGuide.Core.Configuration;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Mappings;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public class QrCodesController : Controller
{
    private static readonly string[] SupportedNarrationLanguages = ["vi", "en", "zh", "ko", "fr"];
    private readonly PoiApiClient _poiApiClient;
    private readonly TourApiClient _tourApiClient;
    private readonly ListeningHistoryApiClient _listeningHistoryApiClient;
    private readonly ActiveDeviceApiClient _activeDeviceApiClient;
    private readonly QrCodeOptions _qrCodeOptions;
    private readonly Uri _poiApiBaseUri;

    public QrCodesController(
        PoiApiClient poiApiClient,
        TourApiClient tourApiClient,
        ListeningHistoryApiClient listeningHistoryApiClient,
        ActiveDeviceApiClient activeDeviceApiClient,
        IOptions<QrCodeOptions> qrCodeOptions,
        IConfiguration configuration)
    {
        _poiApiClient = poiApiClient;
        _tourApiClient = tourApiClient;
        _listeningHistoryApiClient = listeningHistoryApiClient;
        _activeDeviceApiClient = activeDeviceApiClient;
        _qrCodeOptions = qrCodeOptions.Value;
        _poiApiBaseUri = PoiApiDefaults.CreateBaseUri(configuration["PoiApi:BaseUrl"]);
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
            var poisTask = _poiApiClient.GetPoisAsync(cancellationToken);
            var toursTask = _tourApiClient.GetToursAsync(cancellationToken);

            await Task.WhenAll(poisTask, toursTask);

            var pois = poisTask.Result;
            var poiLookup = pois.ToDictionary(item => item.Id);
            targets.AddRange(pois.Select(BuildPoiTarget));
            targets.AddRange(toursTask.Result
                .Where(tour => tour.IsQrEnabled)
                .Select(tour => BuildTourTarget(tour, poiLookup)));
        }
        catch (HttpRequestException)
        {
            vm.LoadErrorMessage = "Không thể kết nối VKFoodAPI. Danh sách QR cho POI và tour sẽ xuất hiện lại khi API chạy.";
        }

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
    [AllowAnonymous]
    public IActionResult LegacyPoiScan(Guid id)
    {
        return RedirectToAction(nameof(Scan), new
        {
            targetType = QrTargetTypes.Poi,
            targetId = id.ToString()
        })!;
    }

    [HttpGet("/qr/{targetType}/{targetId}")]
    [AllowAnonymous]
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
                return NotFound("Không tìm thấy target cho mã QR này.");
            }

            return View("Scan", BuildScanViewModel(target));
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Hệ thống tạm thời không kết nối được dữ liệu target. Thử lại sau.");
        }
    }

    [HttpPost("/qr/analytics/listening-history")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> CreateListeningHistory(
        [FromBody] ListeningHistoryCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var created = await _listeningHistoryApiClient.CreateAsync(request, cancellationToken);
            if (created is null)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "Hệ thống chưa tạo được lịch sử nghe từ VKFoodAPI.");
            }

            return Ok(new { id = created.Id });
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Hệ thống tạm thời chưa ghi nhận được lịch sử nghe trên web.");
        }
    }

    [HttpPut("/qr/analytics/listening-history/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateListeningHistory(
        Guid id,
        [FromBody] ListeningHistoryUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var updated = await _listeningHistoryApiClient.UpdateAsync(id, request, cancellationToken);
            return updated ? NoContent() : NotFound();
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Hệ thống tạm thời chưa cập nhật được lịch sử nghe trên web.");
        }
    }

    [HttpPost("/qr/analytics/active-devices/heartbeat")]
    [AllowAnonymous]
    public async Task<IActionResult> WebHeartbeat(
        [FromBody] ActiveDeviceHeartbeatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("DeviceId is required.");
        }

        try
        {
            await _activeDeviceApiClient.HeartbeatAsync(request, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Hệ thống tạm thời chưa ghi nhận được thiết bị web đang hoạt động.");
        }
    }

    [HttpPost("/qr/analytics/active-devices/disconnect")]
    [AllowAnonymous]
    public async Task<IActionResult> WebDisconnect(
        [FromBody] ActiveDeviceDisconnectRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return BadRequest("DeviceId is required.");
        }

        try
        {
            await _activeDeviceApiClient.DisconnectAsync(request, cancellationToken);
            return NoContent();
        }
        catch (Exception ex) when (
            ex is HttpRequestException ||
            ex is TaskCanceledException ||
            ex is InvalidOperationException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Hệ thống tạm thời chưa đánh dấu được thiết bị web đã ngừng hoạt động.");
        }
    }

    [HttpGet("/qr/image/{targetType}/{targetId}")]
    [AllowAnonymous]
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
                "Không tạo được QR lúc này vì target chưa truy cập được.");
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
            QrTargetTypes.Tour => await ResolveTourTargetAsync(targetId, cancellationToken),
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

    private async Task<QrTargetDescriptor?> ResolveTourTargetAsync(
        string targetId,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId))
        {
            return null;
        }

        var toursTask = _tourApiClient.GetTourAsync(tourId, cancellationToken);
        var poisTask = _poiApiClient.GetPoisAsync(cancellationToken);

        await Task.WhenAll(toursTask, poisTask);

        var tour = toursTask.Result;
        if (tour is null || !tour.IsQrEnabled)
        {
            return null;
        }

        var poiLookup = poisTask.Result.ToDictionary(item => item.Id);
        return BuildTourTarget(tour, poiLookup);
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
            AudioAssetPath = target.AudioAssetPath,
            NarrationByLanguage = new Dictionary<string, string>(
                target.NarrationByLanguage,
                StringComparer.OrdinalIgnoreCase),
            MapLink = target.MapLink,
            SpecialDish = target.SpecialDish,
            PublicUrl = BuildPublicQrUrl(target.TargetType, target.TargetId),
            AppLaunchUrl = BuildAppLaunchUrl(target.TargetType, target.TargetId),
            AutoOpenApp = string.Equals(target.TargetType, QrTargetTypes.Poi, StringComparison.OrdinalIgnoreCase),
            EstimatedDurationLabel = target.EstimatedDurationLabel,
            TourStops = target.TourStops
        };
    }

    private QrTargetDescriptor BuildPoiTarget(PoiDto poi)
    {
        var targetId = poi.Id.ToString();
        var domainPoi = poi.ToDomain();
        var narrationByLanguage = BuildPoiNarrationByLanguage(domainPoi);
        var primaryNarration = narrationByLanguage.TryGetValue("vi", out var vietnameseNarration) &&
                               !string.IsNullOrWhiteSpace(vietnameseNarration)
            ? vietnameseNarration
            : domainPoi.GetNarrationText();

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
            Status = poi.IsActive ? "Sẵn sàng" : "Tạm khóa",
            NarrationText = string.IsNullOrWhiteSpace(primaryNarration) ? poi.Description : primaryNarration,
            AudioAssetPath = ResolvePublicAudioPath(poi.AudioAssetPath),
            NarrationByLanguage = narrationByLanguage,
            MapLink = poi.MapLink,
            SpecialDish = poi.SpecialDish,
            DetailUrl = Url.Action("Details", "Pois", new { id = poi.Id }) ?? string.Empty,
            DetailActionLabel = "Xem POI",
            RouteSummary = BuildPoiRouteSummary(poi),
            EstimatedDurationLabel = $"Bán kính kích hoạt {poi.TriggerRadiusMeters:0.#} m",
            SortOrder = 0
        };
    }

    private QrTargetDescriptor BuildTourTarget(
        TourDto tour,
        IReadOnlyDictionary<Guid, PoiDto> poiLookup)
    {
        var tourStops = ResolveTourStops(tour.PoiIds, poiLookup);
        var targetId = tour.Id.ToString(CultureInfo.InvariantCulture);
        var narrationText = BuildTourNarration(tour, tourStops);
        var routeSummary = tourStops.Count == 0
            ? "Chưa có điểm dừng hợp lệ"
            : string.Join(" -> ", tourStops);

        return new QrTargetDescriptor
        {
            TargetType = QrTargetTypes.Tour,
            TargetId = targetId,
            TargetCode = string.IsNullOrWhiteSpace(tour.Code) ? $"TOUR-{tour.Id:D3}" : tour.Code,
            TargetName = tour.Name,
            TargetKindLabel = "Tour",
            Description = tour.Description,
            Address = string.Empty,
            ActivationType = "QR điều hướng tour",
            Status = tour.IsActive && tour.IsQrEnabled ? "Sẵn sàng" : "Tạm khóa",
            NarrationText = narrationText,
            AudioAssetPath = string.Empty,
            NarrationByLanguage = BuildFlatNarrationByLanguage(narrationText),
            MapLink = string.Empty,
            SpecialDish = string.Empty,
            DetailUrl = Url.Action("Edit", "Tours", new { id = tour.Id }) ?? $"{Url.Action("Index", "Tours") ?? "/Tours"}#tour-{tour.Id}",
            DetailActionLabel = "Xem tour",
            RouteSummary = routeSummary,
            EstimatedDurationLabel = $"{tour.EstimatedMinutes} phút",
            TourStops = tourStops,
            SortOrder = 1
        };
    }

    private static string GetPoiActivationType(PoiDto poi)
    {
        return PoiAdminMappings.ContainsQr(poi.Description, poi.NarrationText)
            ? "QR + GPS"
            : "QR tại điểm";
    }

    private static string BuildPoiRouteSummary(PoiDto poi)
    {
        if (!string.IsNullOrWhiteSpace(poi.Address))
        {
            return poi.Address;
        }

        return $"{poi.Latitude:0.####}, {poi.Longitude:0.####}";
    }

    private static IReadOnlyList<string> ResolveTourStops(
        IEnumerable<Guid> poiIds,
        IReadOnlyDictionary<Guid, PoiDto> poiLookup)
    {
        return poiIds
            .Where(poiId => poiId != Guid.Empty)
            .Select(poiId => poiLookup.TryGetValue(poiId, out var poi)
                ? poi.Name
                : $"POI {poiId.ToString("N")[..6].ToUpperInvariant()}")
            .ToList();
    }

    private static string BuildTourNarration(TourDto tour, IReadOnlyList<string> tourStops)
    {
        var stopSummary = tourStops.Count == 0
            ? "Tour chưa có điểm dừng được khai báo."
            : $"Các điểm dừng chính: {string.Join(", ", tourStops)}.";

        return $"{tour.Description} Thời lượng dự kiến {tour.EstimatedMinutes} phút. {stopSummary}";
    }

    private static Dictionary<string, string> BuildPoiNarrationByLanguage(
        VinhKhanhGuide.Core.Models.POI poi)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var languageCode in SupportedNarrationLanguages)
        {
            var narration = poi.GetNarrationText(languageCode);
            if (!string.IsNullOrWhiteSpace(narration))
            {
                result[languageCode] = narration;
            }
        }

        return result;
    }

    private static Dictionary<string, string> BuildFlatNarrationByLanguage(string narrationText)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var languageCode in SupportedNarrationLanguages)
        {
            result[languageCode] = narrationText;
        }

        return result;
    }

    private string ResolvePublicAudioPath(string? audioAssetPath)
    {
        var trimmed = audioAssetPath?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
        {
            return string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                ? absoluteUri.ToString()
                : trimmed;
        }

        return new Uri(_poiApiBaseUri, trimmed.TrimStart('/')).ToString();
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

    private static string BuildAppLaunchUrl(string targetType, string targetId)
    {
        if (!string.Equals(QrTargetTypes.Normalize(targetType), QrTargetTypes.Poi, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return $"vinhkhanhguide://poi/{Uri.EscapeDataString(targetId)}?autoplay=1&source=bus-stop-qr";
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
        public string AudioAssetPath { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, string> NarrationByLanguage { get; init; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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

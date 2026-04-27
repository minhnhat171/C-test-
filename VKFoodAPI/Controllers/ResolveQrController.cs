using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using VinhKhanhGuide.Core.Contracts;
using VKFoodAPI.Services;

namespace VKFoodAPI.Controllers;

[ApiController]
[Route("api/resolve-qr")]
public sealed class ResolveQrController : ControllerBase
{
    private readonly QrCodeRepository _qrCodeRepository;
    private readonly PoiRepository _poiRepository;
    private readonly TourRepository _tourRepository;

    public ResolveQrController(
        QrCodeRepository qrCodeRepository,
        PoiRepository poiRepository,
        TourRepository tourRepository)
    {
        _qrCodeRepository = qrCodeRepository;
        _poiRepository = poiRepository;
        _tourRepository = tourRepository;
    }

    [HttpGet]
    public ActionResult<ResolveQrResponseDto> Resolve([FromQuery] string? code)
    {
        var rawCode = code?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return BadRequest("code is required.");
        }

        var resolved = ResolveCore(rawCode);
        return resolved.Resolved
            ? Ok(resolved)
            : NotFound(resolved);
    }

    private ResolveQrResponseDto ResolveCore(string rawCode)
    {
        if (TryParseDeepLink(rawCode, out var deepLinkTargetType, out var deepLinkTargetId))
        {
            return ResolveTarget(deepLinkTargetType, deepLinkTargetId, rawCode, "deep-link");
        }

        if (TryParseQrPath(rawCode, out var pathTargetType, out var pathTargetId, out var pathCode))
        {
            if (!string.IsNullOrWhiteSpace(pathTargetType))
            {
                return ResolveTarget(pathTargetType, pathTargetId, rawCode, "qr-path");
            }

            var resolvedByPathCode = ResolveCode(pathCode, "qr-code-path");
            if (resolvedByPathCode.Resolved)
            {
                return resolvedByPathCode;
            }
        }

        return ResolveCode(rawCode, "code");
    }

    private ResolveQrResponseDto ResolveCode(string code, string source)
    {
        var qrItem = _qrCodeRepository.GetActiveByCode(code);
        if (qrItem is not null)
        {
            return ResolveTarget(qrItem.TargetType, qrItem.TargetId, qrItem.Code, "qr-code-item");
        }

        var poi = _poiRepository.GetAll()
            .FirstOrDefault(item =>
                item.IsActive &&
                string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (poi is not null)
        {
            return BuildPoiResponse(poi, code, source);
        }

        var tour = _tourRepository.GetAll()
            .FirstOrDefault(item =>
                item.IsActive &&
                item.IsQrEnabled &&
                string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (tour is not null)
        {
            return BuildTourResponse(tour, code, source);
        }

        return ResolveQrResponseDto.NotFound(code);
    }

    private ResolveQrResponseDto ResolveTarget(string targetType, string targetId, string code, string source)
    {
        if (!QrTargetKinds.IsSupported(targetType))
        {
            return ResolveQrResponseDto.NotFound(code);
        }

        if (string.Equals(QrTargetKinds.Normalize(targetType), QrTargetKinds.Tour, StringComparison.Ordinal))
        {
            if (!int.TryParse(targetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId))
            {
                return ResolveQrResponseDto.NotFound(code);
            }

            var tour = _tourRepository.GetById(tourId);
            return tour is { IsActive: true, IsQrEnabled: true }
                ? BuildTourResponse(tour, code, source)
                : ResolveQrResponseDto.NotFound(code);
        }

        if (!Guid.TryParse(targetId, out var poiId))
        {
            return ResolveQrResponseDto.NotFound(code);
        }

        var poi = _poiRepository.GetById(poiId);
        return poi is { IsActive: true }
            ? BuildPoiResponse(poi, code, source)
            : ResolveQrResponseDto.NotFound(code);
    }

    private static ResolveQrResponseDto BuildPoiResponse(PoiDto poi, string code, string source)
    {
        return new ResolveQrResponseDto
        {
            Resolved = true,
            TargetType = QrTargetKinds.Poi,
            TargetId = poi.Id.ToString(),
            Code = code,
            Source = source,
            DeepLink = $"vinhkhanhguide://poi/{Uri.EscapeDataString(poi.Id.ToString())}?autoplay=1&source=resolve-qr",
            Message = "QR resolved to POI.",
            Poi = poi.Clone()
        };
    }

    private static ResolveQrResponseDto BuildTourResponse(TourDto tour, string code, string source)
    {
        return new ResolveQrResponseDto
        {
            Resolved = true,
            TargetType = QrTargetKinds.Tour,
            TargetId = tour.Id.ToString(CultureInfo.InvariantCulture),
            Code = code,
            Source = source,
            DeepLink = $"vinhkhanhguide://tour/{tour.Id.ToString(CultureInfo.InvariantCulture)}?autoplay=1&source=resolve-qr",
            Message = "QR resolved to tour.",
            Tour = tour.Clone()
        };
    }

    private static bool TryParseDeepLink(string rawCode, out string targetType, out string targetId)
    {
        targetType = string.Empty;
        targetId = string.Empty;

        if (!Uri.TryCreate(rawCode, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, "vinhkhanhguide", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        targetType = uri.Host;
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (string.IsNullOrWhiteSpace(targetType) && segments.Length > 0)
        {
            targetType = segments[0];
            segments = segments.Skip(1).ToArray();
        }

        if (segments.Length == 0)
        {
            return false;
        }

        if (!QrTargetKinds.IsSupported(targetType))
        {
            return false;
        }

        targetType = QrTargetKinds.Normalize(targetType);
        targetId = segments[^1];
        return true;
    }

    private static bool TryParseQrPath(
        string rawCode,
        out string targetType,
        out string targetId,
        out string code)
    {
        targetType = string.Empty;
        targetId = string.Empty;
        code = string.Empty;

        var path = rawCode.Trim();
        if (Uri.TryCreate(rawCode, UriKind.Absolute, out var uri))
        {
            path = uri.AbsolutePath;
        }

        var segments = path
            .Trim()
            .TrimStart('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0 ||
            !string.Equals(segments[0], "qr", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (segments.Length >= 3 &&
            IsSupportedTargetType(segments[1]))
        {
            targetType = QrTargetKinds.Normalize(segments[1]);
            targetId = segments[2];
            return true;
        }

        if (segments.Length >= 2)
        {
            code = segments[1];
            return true;
        }

        return false;
    }

    private static bool IsSupportedTargetType(string value)
    {
        return string.Equals(value, QrTargetKinds.Poi, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, QrTargetKinds.Tour, StringComparison.OrdinalIgnoreCase);
    }
}

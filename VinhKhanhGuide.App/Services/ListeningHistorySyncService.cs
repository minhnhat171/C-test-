using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.Services;

public class ListeningHistorySyncService : IListeningHistorySyncService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly ILogger<ListeningHistorySyncService> _logger;

    public ListeningHistorySyncService(
        HttpClient httpClient,
        IAuthService authService,
        ILogger<ListeningHistorySyncService> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _logger = logger;
    }

    public async Task<Guid?> BeginAsync(
        POI poi,
        string? language,
        string? playbackMode,
        bool autoTriggered,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = _authService.CurrentSession;
            var request = new ListeningHistoryCreateRequest
            {
                PoiId = poi.Id,
                PoiCode = poi.Code,
                PoiName = poi.Name,
                PoiAddress = poi.Address,
                PoiDescription = poi.Description,
                PoiSpecialDish = poi.SpecialDish,
                PoiImageSource = poi.ImageSource,
                PoiMapLink = poi.MapLink,
                UserCode = session?.UserCode ?? "guest",
                UserDisplayName = string.IsNullOrWhiteSpace(session?.FullName) ? "Khách tham quan" : session!.FullName,
                UserEmail = session?.Email ?? string.Empty,
                TriggerType = autoTriggered ? "GPS" : "APP",
                Language = string.IsNullOrWhiteSpace(language) ? "vi" : language!.Trim(),
                PlaybackMode = string.Equals(playbackMode, "audio", StringComparison.OrdinalIgnoreCase) ? "audio" : "tts",
                NarrationSnapshot = poi.GetNarrationText(language),
                AudioAssetPath = poi.AudioAssetPath,
                Source = "app",
                DevicePlatform = DeviceInfo.Current.Platform.ToString(),
                AutoTriggered = autoTriggered,
                StartedAtUtc = DateTimeOffset.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(
                "api/analytics/listening-history",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<ListeningHistoryEntryDto>(
                cancellationToken: cancellationToken);

            return created?.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create listening history entry for POI {PoiId}.", poi.Id);
            return null;
        }
    }

    public async Task CompleteAsync(
        Guid historyId,
        int listenSeconds,
        bool completed,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (historyId == Guid.Empty)
        {
            return;
        }

        try
        {
            var request = new ListeningHistoryUpdateRequest
            {
                ListenSeconds = Math.Max(0, listenSeconds),
                Completed = completed,
                CompletedAtUtc = DateTimeOffset.UtcNow,
                ErrorMessage = errorMessage?.Trim() ?? string.Empty
            };

            var response = await _httpClient.PutAsJsonAsync(
                $"api/analytics/listening-history/{historyId}",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update listening history entry {HistoryId}.", historyId);
        }
    }

    public async Task<IReadOnlyList<ListeningHistoryEntryDto>> GetCurrentUserHistoryAsync(
        string? sortBy,
        string? period,
        int limit = 15,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (userCode, userEmail) = GetCurrentUserScope();
            var requestUri = BuildRequestUri(
                "api/analytics/listening-history",
                ("sortBy", sortBy),
                ("period", period),
                ("userCode", userCode),
                ("userEmail", userEmail),
                ("limit", limit > 0 ? limit.ToString() : null));

            return await _httpClient.GetFromJsonAsync<List<ListeningHistoryEntryDto>>(requestUri, cancellationToken)
                ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load listening history for the current app user.");
            return [];
        }
    }

    public async Task<IReadOnlyList<PoiListeningCountDto>> GetCurrentUserRankingAsync(
        string? period,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (userCode, userEmail) = GetCurrentUserScope();
            var requestUri = BuildRequestUri(
                "api/analytics/listening-history/ranking",
                ("period", period),
                ("userCode", userCode),
                ("userEmail", userEmail));

            return await _httpClient.GetFromJsonAsync<List<PoiListeningCountDto>>(requestUri, cancellationToken)
                ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load listening history ranking for the current app user.");
            return [];
        }
    }

    public async Task<bool> DeleteAsync(
        Guid historyId,
        CancellationToken cancellationToken = default)
    {
        if (historyId == Guid.Empty)
        {
            return false;
        }

        try
        {
            var response = await _httpClient.DeleteAsync(
                $"api/analytics/listening-history/{historyId}",
                cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete listening history entry {HistoryId}.", historyId);
            return false;
        }
    }

    private (string? UserCode, string? UserEmail) GetCurrentUserScope()
    {
        var session = _authService.CurrentSession;
        if (session is null)
        {
            return ("guest", null);
        }

        var userCode = !string.IsNullOrWhiteSpace(session.Email)
            ? session.UserCode.Trim()
            : session.UserCode;

        var userEmail = string.IsNullOrWhiteSpace(session.Email)
            ? null
            : session.Email.Trim();

        return (userCode, userEmail);
    }

    private static string BuildRequestUri(string path, params (string Key, string? Value)[] queryParts)
    {
        var queryString = string.Join(
            "&",
            queryParts
                .Where(part => !string.IsNullOrWhiteSpace(part.Value))
                .Select(part => $"{Uri.EscapeDataString(part.Key)}={Uri.EscapeDataString(part.Value!)}"));

        return string.IsNullOrWhiteSpace(queryString)
            ? path
            : $"{path}?{queryString}";
    }
}

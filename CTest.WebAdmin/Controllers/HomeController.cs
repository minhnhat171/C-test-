using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class HomeController : Controller
{
    private static readonly JsonSerializerOptions SseJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly DashboardService _dashboardService;

    public HomeController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var vm = await _dashboardService.LoadAsync(cancellationToken);
        /*
        var lastSyncedAt = _data.UsageLogs
            .OrderByDescending(x => x.StartedAt)
            .Select(x => (DateTime?)x.StartedAt)
            .FirstOrDefault();

        var dailyListenPoints = Enumerable.Range(0, 7)
            .Select(offset => today.AddDays(offset - 6))
            .Select(date => new DashboardDailyListenPoint
            {
                Date = date,
                Label = date.ToString("dd/MM"),
                Count = _data.UsageLogs.Count(x => x.StartedAt.Date == date)
            })
            .ToList();

        var topPois = _data.UsageLogs
            .GroupBy(x => x.PoiName)
            .Select(group => new DashboardTopPoiItem
            {
                Name = group.Key,
                Count = group.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .Take(5)
            .ToList();

        var totalUsageLogs = _data.UsageLogs.Count;

        // The sample app does not have a dedicated QR entity yet,
        // so we treat POIs mentioning QR as QR-enabled stops.
        var totalQrCodes = _data.Pois.Count(x =>
            x.Description.Contains("QR", StringComparison.OrdinalIgnoreCase) ||
            x.NarrationScript.Contains("QR", StringComparison.OrdinalIgnoreCase));

        var mostPlayed = _data.UsageLogs
            .GroupBy(x => x.PoiName)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault() ?? "Chưa có dữ liệu";

        var vm = new DashboardViewModel
        {
            TotalPois = _data.Pois.Count,
            TotalAudioGuides = _data.AudioGuides.Count,
            TotalTranslations = _data.Translations.Count,
            TotalQrCodes = totalQrCodes,
            TodayListenCount = _data.UsageLogs.Count(x => x.StartedAt.Date == today),
            TotalTours = _data.Tours.Count,
            TotalUsageLogs = totalUsageLogs,
            MostPlayedPoi = mostPlayed,
            AverageListenSeconds = totalUsageLogs == 0 ? 0 : _data.UsageLogs.Average(x => x.ListenSeconds),
            CompletionRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(_data.UsageLogs.Count(x => x.Completed) * 100.0 / totalUsageLogs),
            QrListenRate = totalUsageLogs == 0 ? 0 : (int)Math.Round(_data.UsageLogs.Count(x => x.TriggerType == "QR") * 100.0 / totalUsageLogs),
            PublishedAudioCount = _data.AudioGuides.Count(x => x.IsPublished),
            ActivePoiCount = _data.Pois.Count(x => x.IsActive),
            IsSyncOnline = lastSyncedAt.HasValue && lastSyncedAt.Value >= DateTime.Now.AddHours(-3),
            LastSyncedAt = lastSyncedAt,
            DailyListenPoints = dailyListenPoints,
            TopPois = topPois,
            RecentLogs = _data.UsageLogs.OrderByDescending(x => x.StartedAt).Take(8).ToList()
        };

        */
        return View("Dashboard", vm);
    }

    [HttpGet]
    public async Task<IActionResult> ActiveDevices(CancellationToken cancellationToken = default)
    {
        var stats = await _dashboardService.GetActiveDeviceStatsAsync(cancellationToken);
        return Json(stats);
    }

    [HttpGet]
    public async Task<IActionResult> UsageHistorySnapshot(CancellationToken cancellationToken = default)
    {
        var snapshot = await _dashboardService.GetUsageSnapshotAsync(cancellationToken);
        return Json(snapshot);
    }

    [HttpGet]
    public async Task ActiveDeviceEvents(CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.ContentType = "text/event-stream";

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        var lastPayload = string.Empty;

        while (!cancellationToken.IsCancellationRequested)
        {
            var stats = await _dashboardService.GetActiveDeviceStatsAsync(cancellationToken);
            var payload = JsonSerializer.Serialize(stats, SseJsonOptions);

            if (!string.Equals(payload, lastPayload, StringComparison.Ordinal))
            {
                await Response.WriteAsync("event: active-devices\n", cancellationToken);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                lastPayload = payload;
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(cancellationToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

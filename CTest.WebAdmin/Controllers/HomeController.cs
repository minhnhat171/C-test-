using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class HomeController : Controller
{
    private readonly AppDataService _data;

    public HomeController(AppDataService data)
    {
        _data = data;
    }

    public IActionResult Index()
    {
        var today = DateTime.Today;
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

        return View("Dashboard", vm);
    }
}

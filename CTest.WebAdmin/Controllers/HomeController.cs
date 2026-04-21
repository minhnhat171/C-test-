using System.Text.Json;
using System.Text.Json.Serialization;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
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

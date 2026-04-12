using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class AnalyticsController : Controller
{
    private readonly ListeningHistoryService _listeningHistoryService;

    public AnalyticsController(ListeningHistoryService listeningHistoryService)
    {
        _listeningHistoryService = listeningHistoryService;
    }

    [HttpGet]
    public async Task<IActionResult> ListeningHistory(
        string? sortBy = null,
        string? period = null,
        string? view = null,
        bool partial = false,
        CancellationToken cancellationToken = default)
    {
        var model = await _listeningHistoryService.LoadPageAsync(sortBy, period, view, cancellationToken);

        if (partial || IsAjaxRequest())
        {
            return PartialView("_ListeningHistoryContent", model);
        }

        return View(model);
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}

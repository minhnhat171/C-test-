using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

public class UsageLogsController : Controller
{
    private readonly ListeningHistoryService _listeningHistoryService;

    public UsageLogsController(ListeningHistoryService listeningHistoryService)
    {
        _listeningHistoryService = listeningHistoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? sortBy = null,
        string? period = null,
        string? keyword = null,
        bool partial = false,
        CancellationToken cancellationToken = default)
    {
        var model = await _listeningHistoryService.LoadPageAsync(sortBy, period, keyword, cancellationToken);

        if (partial || IsAjaxRequest())
        {
            return PartialView("_UsageHistoryContent", model);
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
using CTest.WebAdmin.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace CTest.WebAdmin.Controllers;

[Authorize(Policy = WebAdminPolicies.AdminOnly)]
public class AnalyticsController : Controller
{
    [HttpGet]
    public IActionResult ListeningHistory(
        string? sortBy = null,
        string? period = null,
        string? keyword = null,
        bool partial = false)
    {
        var routeValues = new RouteValueDictionary();

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            routeValues[nameof(sortBy)] = sortBy;
        }

        if (!string.IsNullOrWhiteSpace(period))
        {
            routeValues[nameof(period)] = period;
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            routeValues[nameof(keyword)] = keyword;
        }

        if (partial)
        {
            routeValues[nameof(partial)] = true;
        }

        return RedirectToActionPermanent(nameof(UsageLogsController.Index), "UsageLogs", routeValues);
    }
}

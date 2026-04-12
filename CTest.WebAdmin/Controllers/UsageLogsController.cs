using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

public class UsageLogsController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(AnalyticsController.ListeningHistory), "Analytics");
    }
}

using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class UsageLogsController : Controller
{
    private readonly AppDataService _data;

    public UsageLogsController(AppDataService data)
    {
        _data = data;
    }

    public IActionResult Index()
    {
        return View(_data.UsageLogs.OrderBy(x => x.Id).ToList());
    }
}

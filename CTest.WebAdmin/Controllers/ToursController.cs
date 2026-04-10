using Microsoft.AspNetCore.Mvc;
using CTest.WebAdmin.Services;

namespace CTest.WebAdmin.Controllers;

public class ToursController : Controller
{
    private readonly AppDataService _data;

    public ToursController(AppDataService data)
    {
        _data = data;
    }

    public IActionResult Index()
    {
        return View(_data.Tours.OrderBy(x => x.Id).ToList());
    }
}

using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

public class UsersController : Controller
{
    private readonly UserManagementService _userManagementService;

    public UsersController(UserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? status = null,
        string? keyword = null,
        Guid? selectedUserId = null,
        bool partial = false,
        CancellationToken cancellationToken = default)
    {
        var model = await _userManagementService.LoadPageAsync(status, keyword, selectedUserId, cancellationToken);

        if (partial || IsAjaxRequest())
        {
            return PartialView("_UserManagementContent", model);
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

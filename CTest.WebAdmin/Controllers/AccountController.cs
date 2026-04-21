using System.Security.Claims;
using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

public class AccountController : Controller
{
    private readonly IWebAdminAuthService _authService;

    public AccountController(IWebAdminAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToHomeByRole();
        }

        return View(new LoginViewModel
        {
            ReturnUrl = SanitizeReturnUrl(returnUrl)
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var authenticatedUser = _authService.ValidateCredentials(model.Username, model.Password);
        if (authenticatedUser is null)
        {
            model.ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, authenticatedUser.Username),
            new(ClaimTypes.Role, authenticatedUser.Role),
            new(WebAdminClaimTypes.DisplayName, authenticatedUser.DisplayName),
            new(WebAdminClaimTypes.OwnerCode, authenticatedUser.OwnerCode),
            new(WebAdminClaimTypes.OwnerEmail, authenticatedUser.OwnerEmail)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        var returnUrl = SanitizeReturnUrl(model.ReturnUrl);
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToHomeByRole(authenticatedUser.Role);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToHomeByRole(string? role = null)
    {
        var resolvedRole = role;
        if (string.IsNullOrWhiteSpace(resolvedRole))
        {
            resolvedRole = User.IsInRole(WebAdminRoles.Admin)
                ? WebAdminRoles.Admin
                : WebAdminRoles.PoiOwner;
        }

        return string.Equals(resolvedRole, WebAdminRoles.Admin, StringComparison.Ordinal)
            ? RedirectToAction("Index", "Home")
            : RedirectToAction("Index", "Owner");
    }

    private string SanitizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : string.Empty;
    }
}

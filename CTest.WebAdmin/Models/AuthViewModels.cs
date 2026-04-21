namespace CTest.WebAdmin.Models;

public sealed class LoginViewModel
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

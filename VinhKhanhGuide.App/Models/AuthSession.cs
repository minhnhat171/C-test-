namespace VinhKhanhGuide.App.Models;

public class AuthSession
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = "user";

    public string Initials => BuildInitials();

    public string RoleLabel => Role switch
    {
        "admin" => "Quản trị viên",
        "poi_owner" => "Chủ quán",
        _ => "Khách khám phá"
    };

    private string BuildInitials()
    {
        var parts = FullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(2)
            .ToArray();

        if (parts.Length == 0)
        {
            return Email.Length >= 2
                ? Email[..2].ToUpperInvariant()
                : "VK";
        }

        return string.Concat(parts.Select(part => char.ToUpperInvariant(part[0])));
    }
}

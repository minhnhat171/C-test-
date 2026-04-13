namespace CTest.WebAdmin.Models;

public class UserManagementPageViewModel
{
    public string SelectedStatus { get; set; } = "all";
    public string SearchTerm { get; set; } = string.Empty;
    public Guid? SelectedUserId { get; set; }
    public string LoadErrorMessage { get; set; } = string.Empty;
    public int TotalUsers { get; set; }
    public int OnlineUsers { get; set; }
    public int OfflineUsers { get; set; }
    public int FilteredUsers { get; set; }
    public List<UserSummaryViewModel> Items { get; set; } = new();
    public UserDetailViewModel? SelectedUser { get; set; }
    public UserLocationViewModel? SelectedLocation { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(SearchTerm) ||
        !string.Equals(SelectedStatus, "all", StringComparison.OrdinalIgnoreCase);
}

public class UserSummaryViewModel
{
    public Guid Id { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastActiveAtUtc { get; set; }
    public string LastPoiName { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int TotalListenSeconds { get; set; }

    public string LastActiveDisplay => LastActiveAtUtc.HasValue
        ? LastActiveAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : "--";

    public string StatusLabel => IsOnline ? "Online" : "Offline";

    public string StatusBadgeClass => IsOnline ? "text-bg-success" : "text-bg-secondary";

    public string ListenDurationLabel => $"{TotalListenSeconds} giay";
}

public class UserDetailViewModel
{
    public Guid Id { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "offline";
    public bool IsOnline { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastActiveAtUtc { get; set; }
    public DateTimeOffset? LastCompletedAtUtc { get; set; }
    public string PreferredLanguage { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DevicePlatform { get; set; } = string.Empty;
    public string LastTriggerType { get; set; } = string.Empty;
    public string LastSource { get; set; } = string.Empty;
    public string LastPoiName { get; set; } = string.Empty;
    public string LastPoiCode { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int TotalListenSeconds { get; set; }

    public string CreatedAtDisplay => CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public string LastActiveDisplay => LastActiveAtUtc.HasValue
        ? LastActiveAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : "--";

    public string LastCompletedDisplay => LastCompletedAtUtc.HasValue
        ? LastCompletedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : "--";

    public string StatusLabel => IsOnline ? "Online" : "Offline";

    public string StatusBadgeClass => IsOnline ? "text-bg-success" : "text-bg-secondary";
}

public class UserLocationViewModel
{
    public Guid UserId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid PoiId { get; set; }
    public string PoiCode { get; set; } = string.Empty;
    public string PoiName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string MapLink { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public string UpdatedAtDisplay => UpdatedAtUtc.HasValue
        ? UpdatedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : "--";

    public string MapEmbedUrl => $"https://maps.google.com/maps?q={Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&z=15&output=embed";
}

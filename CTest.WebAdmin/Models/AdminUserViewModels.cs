using VinhKhanhGuide.Core.Contracts;

namespace CTest.WebAdmin.Models;

public sealed class AdminUsersPageViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "all";
    public string LoadErrorMessage { get; set; } = string.Empty;
    public Guid? SelectedUserId { get; set; }
    public List<AdminUserSummaryDto> Users { get; set; } = [];
    public AdminUserDetailDto? SelectedUser { get; set; }
    public AdminUserLocationDto? SelectedLocation { get; set; }

    public int TotalUsers => Users.Count;
    public int OnlineUsers => Users.Count(user => user.IsOnline);
    public int OfflineUsers => Users.Count(user => !user.IsOnline);
    public int TotalSessions => Users.Sum(user => user.TotalSessions);
}

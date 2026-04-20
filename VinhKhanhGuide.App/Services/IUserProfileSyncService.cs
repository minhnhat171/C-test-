namespace VinhKhanhGuide.App.Services;

public interface IUserProfileSyncService
{
    Task<bool> SyncCurrentUserAsync(string preferredLanguageCode, CancellationToken cancellationToken = default);
}

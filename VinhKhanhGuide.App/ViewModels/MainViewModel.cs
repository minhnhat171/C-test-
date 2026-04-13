using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.Core.Contracts;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private const int MaxRecentSearches = 6;
    private const int MaxSearchSuggestions = 6;
    private const int MaxPinnedRecentSuggestions = 3;
    private const string UserPreferenceKeyPrefix = "vinhkhanh.user.preferences.v1";
    private static readonly TimeSpan PoiRefreshInterval = TimeSpan.FromSeconds(8);

    public const double EntranceLatitude = 10.7614500;
    public const double EntranceLongitude = 106.7028200;
    public const string EntranceName = "Cổng phố ẩm thực Vĩnh Khánh";
    public const string EntranceAddress = "40 Vĩnh Khánh, P. Khánh Hội, Q.4";

    private readonly ILocationService _locationService;
    private readonly IPoiProvider _poiProvider;
    private readonly INarrationService _narrationService;
    private readonly IAuthService _authService;
    private readonly IUsageHistoryService _usageHistoryService;
    private readonly IListeningHistorySyncService _listeningHistorySyncService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SemaphoreSlim _locationUpdateGate = new(1, 1);
    private readonly SemaphoreSlim _listeningHistoryRefreshGate = new(1, 1);
    private readonly Dictionary<Guid, DateTimeOffset> _lastNarratedAt = new();
    private readonly HashSet<Guid> _insidePoiIds = [];
    private readonly List<string> _recentSearches = [];

    private IReadOnlyList<POI> _pois = Array.Empty<POI>();
    private Task? _poiRefreshLoopTask;
    private bool _isInitialized;
    private bool _isTracking;
    private bool _isNarrating;
    private bool _isSearchFocused;
    private bool _isSearchSuggestionsVisible;
    private bool _isSearchResultEmpty;
    private bool _isAutoNarrationEnabled = true;
    private bool _isRestoringUserPreferences;
    private int _narrationSessionId;
    private Guid? _lastAutoNarratedPoiId;
    private LocationDto? _lastLocation;
    private Guid? _activeNarrationPoiId;
    private bool _hasLocationPermission;
    private bool _hasCheckedLocationPermission;
    private POI? _selectedPoi;
    private bool _hasUserSelectedPoi;
    private string _poiDataSnapshot = string.Empty;
    private string _statusText = "Đang chờ khởi động GPS";
    private string _locationText = "Bạn đang xem cổng phố ẩm thực Vĩnh Khánh";
    private string _nearestPoiText = "Chọn quán hoặc chạm bản đồ để nghe thuyết minh";
    private string _mapModeBadgeText = "Đang tải bản đồ";
    private string _mapPoiBadgeText = "0 POI";
    private string _selectedPoiName = "Ốc Oanh";
    private string _selectedPoiAddress = string.Empty;
    private string _selectedPoiDescription = string.Empty;
    private string _selectedPoiDishText = string.Empty;
    private string _selectedPoiStatusText = string.Empty;
    private string _selectedPoiNarrationPreview = string.Empty;
    private string _selectedPoiMapLink = string.Empty;
    private string _selectedPoiImageSource = string.Empty;
    private string _selectedLanguage = "vi";
    private string _searchQuery = string.Empty;
    private string _currentUserDisplayName = "Khách";
    private string _currentUserStatusLine = "Khách khám phá";
    private string _currentUserInitials = "VK";
    private string _currentUserAccountLabel = "guest";
    private string _currentUserPasswordLabel = "••••••••";

    private bool _isListeningHistoryLoading;
    private string _listeningHistoryLoadError = string.Empty;
    private DateTimeOffset? _lastListeningHistorySyncAt;
    private string _selectedListeningHistoryPeriod = "Tat ca";
    private string _selectedListeningHistorySort = "Moi nhat truoc";
    private string _selectedListeningHistoryView = "Dong thoi gian";

    public MainViewModel(
        ILocationService locationService,
        IPoiProvider poiProvider,
        INarrationService narrationService,
        IAuthService authService,
        IUsageHistoryService usageHistoryService,
        IListeningHistorySyncService listeningHistorySyncService,
        GeofenceEngine geofenceEngine)
    {
        _locationService = locationService;
        _poiProvider = poiProvider;
        _narrationService = narrationService;
        _authService = authService;
        _usageHistoryService = usageHistoryService;
        _listeningHistorySyncService = listeningHistorySyncService;
        _geofenceEngine = geofenceEngine;

        _locationService.LocationUpdated += OnLocationUpdated;
        _authService.SessionChanged += OnAuthSessionChanged;

        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🐚", Name = "Ốc" });
        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🥩", Name = "Bò nướng" });
        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🍲", Name = "Lẩu" });
        FeaturedDishes.Add(new FoodCategoryItem { Icon = "🦀", Name = "Cua" });

        StartTrackingCommand = new Command(async () => await StartAsync(), () => !IsTracking);
        StopTrackingCommand = new Command(async () => await StopAsync(), () => IsTracking);
        StopNarrationCommand = new Command(async () => await StopNarrationAsync(), () => IsNarrating);
        SignOutCommand = new Command(async () => await SignOutAsync());
        ClearEventLogsCommand = new Command(ClearEventLogs, () => HasEventLogs);
        ClearListeningHistoryCommand = new Command(ClearListeningHistory, () => HasListeningHistory);
        ClearViewHistoryCommand = new Command(ClearViewHistory, () => HasViewHistory);
        RefreshListeningHistoryCommand = new Command(async () => await RefreshListeningHistoryAsync());

        SyncCurrentUser();
        LoadPersistedState();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? MapStateChanged;

    public ObservableCollection<FoodCategoryItem> FeaturedDishes { get; } = new();
    public ObservableCollection<PoiStatusItem> PoiStatuses { get; } = new();
    public ObservableCollection<PoiStatusItem> FilteredPoiStatuses { get; } = new();
    public ObservableCollection<SearchSuggestionItem> SearchSuggestions { get; } = new();
    public ObservableCollection<string> EventLogs { get; } = new();
    public ObservableCollection<ListeningHistoryDisplayItem> ListeningHistory { get; } = new();
    public ObservableCollection<ListeningHistoryRankingDisplayItem> ListeningHistoryRanking { get; } = new();
    public ObservableCollection<string> ViewHistory { get; } = new();
    public IReadOnlyList<string> SupportedLanguages { get; } = ["vi", "en", "zh", "ko", "fr"];
    public IReadOnlyList<string> ListeningHistoryPeriodOptions { get; } = ["Tat ca", "24 gio qua", "7 ngay qua", "30 ngay qua"];
    public IReadOnlyList<string> ListeningHistorySortOptions { get; } = ["Moi nhat truoc", "Cu nhat truoc"];
    public IReadOnlyList<string> ListeningHistoryViewOptions { get; } = ["Dong thoi gian", "Xep hang POI"];

    public ICommand StartTrackingCommand { get; }
    public ICommand StopTrackingCommand { get; }
    public ICommand StopNarrationCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand ClearEventLogsCommand { get; }
    public ICommand ClearListeningHistoryCommand { get; }
    public ICommand ClearViewHistoryCommand { get; }
    public ICommand RefreshListeningHistoryCommand { get; }

    public IReadOnlyList<POI> Pois => _pois;
    public LocationDto? LastLocation => _lastLocation;
    public Guid? SelectedPoiId => _selectedPoi?.Id;
    public bool IsSelectedPoiNarrating => _selectedPoi is not null && IsNarrating && _activeNarrationPoiId == _selectedPoi.Id;
    public string SelectedPoiNarrationActionText => IsSelectedPoiNarrating ? "Dừng" : "Nghe thuyết minh";

    public bool IsTracking
    {
        get => _isTracking;
        private set
        {
            if (_isTracking == value)
            {
                return;
            }

            _isTracking = value;
            OnPropertyChanged();
            (StartTrackingCommand as Command)?.ChangeCanExecute();
            (StopTrackingCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool IsNarrating
    {
        get => _isNarrating;
        private set
        {
            if (_isNarrating == value)
            {
                return;
            }

            _isNarrating = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSelectedPoiNarrating));
            OnPropertyChanged(nameof(SelectedPoiNarrationActionText));
            (StopNarrationCommand as Command)?.ChangeCanExecute();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string LocationText
    {
        get => _locationText;
        private set => SetProperty(ref _locationText, value);
    }

    public string NearestPoiText
    {
        get => _nearestPoiText;
        private set => SetProperty(ref _nearestPoiText, value);
    }

    public string SelectedPoiName
    {
        get => _selectedPoiName;
        private set => SetProperty(ref _selectedPoiName, value);
    }

    public string SelectedPoiAddress
    {
        get => _selectedPoiAddress;
        private set => SetProperty(ref _selectedPoiAddress, value);
    }

    public string SelectedPoiDescription
    {
        get => _selectedPoiDescription;
        private set => SetProperty(ref _selectedPoiDescription, value);
    }

    public string SelectedPoiDishText
    {
        get => _selectedPoiDishText;
        private set => SetProperty(ref _selectedPoiDishText, value);
    }

    public string SelectedPoiStatusText
    {
        get => _selectedPoiStatusText;
        private set => SetProperty(ref _selectedPoiStatusText, value);
    }

    public string SelectedPoiNarrationPreview
    {
        get => _selectedPoiNarrationPreview;
        private set => SetProperty(ref _selectedPoiNarrationPreview, value);
    }

    public string SelectedPoiMapLink
    {
        get => _selectedPoiMapLink;
        private set => SetProperty(ref _selectedPoiMapLink, value);
    }

    public string SelectedPoiImageSource
    {
        get => _selectedPoiImageSource;
        private set => SetProperty(ref _selectedPoiImageSource, value);
    }

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (!SetProperty(ref _selectedLanguage, value))
            {
                return;
            }

            PersistUserPreferences();
            UpdateSelectedPoiDetails();
            OnPropertyChanged(nameof(SelectedLanguageDisplayName));
            OnPropertyChanged(nameof(AudioSettingsSummary));

            if (!_isRestoringUserPreferences)
            {
                AddLog($"{NowLabel()} Chuyển ngôn ngữ sang {SelectedLanguageDisplayName}");
            }
        }
    }

    public bool IsAutoNarrationEnabled
    {
        get => _isAutoNarrationEnabled;
        set
        {
            if (!SetProperty(ref _isAutoNarrationEnabled, value))
            {
                return;
            }

            PersistUserPreferences();
            OnPropertyChanged(nameof(AudioSettingsSummary));

            if (!_isRestoringUserPreferences)
            {
                AddLog($"{NowLabel()} {(value ? "Bật" : "Tắt")} tự động phát khi vào vùng");
            }
        }
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (!SetProperty(ref _searchQuery, value ?? string.Empty))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSearchQuery));
            UpdateFilteredPoiStatuses();
            RefreshSearchSuggestions();
        }
    }

    public bool HasSearchQuery => !string.IsNullOrWhiteSpace(SearchQuery);

    public bool IsSearchSuggestionsVisible
    {
        get => _isSearchSuggestionsVisible;
        private set => SetProperty(ref _isSearchSuggestionsVisible, value);
    }

    public bool IsSearchResultEmpty
    {
        get => _isSearchResultEmpty;
        private set => SetProperty(ref _isSearchResultEmpty, value);
    }

    public string CurrentUserDisplayName
    {
        get => _currentUserDisplayName;
        private set => SetProperty(ref _currentUserDisplayName, value);
    }

    public string CurrentUserStatusLine
    {
        get => _currentUserStatusLine;
        private set => SetProperty(ref _currentUserStatusLine, value);
    }

    public string CurrentUserInitials
    {
        get => _currentUserInitials;
        private set => SetProperty(ref _currentUserInitials, value);
    }

    public string CurrentUserAccountLabel
    {
        get => _currentUserAccountLabel;
        private set => SetProperty(ref _currentUserAccountLabel, value);
    }

    public string CurrentUserPasswordLabel
    {
        get => _currentUserPasswordLabel;
        private set => SetProperty(ref _currentUserPasswordLabel, value);
    }

    public string MapModeBadgeText
    {
        get => _mapModeBadgeText;
        private set => SetProperty(ref _mapModeBadgeText, value);
    }

    public string MapPoiBadgeText
    {
        get => _mapPoiBadgeText;
        private set => SetProperty(ref _mapPoiBadgeText, value);
    }

    public bool HasEventLogs => EventLogs.Count > 0;

    public bool IsEventLogEmpty => EventLogs.Count == 0;

    public bool HasListeningHistory => ListeningHistory.Count > 0;

    public bool IsListeningHistoryEmpty => ListeningHistory.Count == 0;

    public bool HasListeningHistoryRanking => ListeningHistoryRanking.Count > 0;

    public bool HasViewHistory => ViewHistory.Count > 0;

    public bool IsViewHistoryEmpty => ViewHistory.Count == 0;

    public string EventLogSummary => HasEventLogs
        ? $"{EventLogs.Count} hoạt động gần nhất của tài khoản hiện tại"
        : "Chưa có hoạt động nào được ghi lại";

    public string ListeningHistorySummary => HasListeningHistory
        ? $"{ListeningHistory.Count} lượt nghe gần nhất"
        : "Chưa có lượt nghe nào";

    public string ViewHistorySummary => HasViewHistory
        ? $"{ViewHistory.Count} lượt xem gần nhất"
        : "Chưa có lượt xem nào";

    public string SelectedLanguageDisplayName => GetLanguageDisplayName(SelectedLanguage);

    public string AudioSettingsSummary =>
        $"{(IsAutoNarrationEnabled ? "Tự động phát khi vào vùng" : "Chỉ phát thủ công")} • {SelectedLanguageDisplayName}";

    public bool IsListeningHistoryLoading
    {
        get => _isListeningHistoryLoading;
        private set => SetProperty(ref _isListeningHistoryLoading, value);
    }

    public string ListeningHistoryLoadError
    {
        get => _listeningHistoryLoadError;
        private set => SetProperty(ref _listeningHistoryLoadError, value);
    }

    public string ListeningHistorySyncStatus => _lastListeningHistorySyncAt.HasValue
        ? $"Dong bo luc {_lastListeningHistorySyncAt.Value.ToLocalTime():HH:mm:ss}"
        : "Chua dong bo lich su nghe";

    public string SelectedListeningHistoryPeriod
    {
        get => _selectedListeningHistoryPeriod;
        set
        {
            if (!SetProperty(ref _selectedListeningHistoryPeriod, value))
            {
                return;
            }

            TriggerListeningHistoryRefresh();
        }
    }

    public string SelectedListeningHistorySort
    {
        get => _selectedListeningHistorySort;
        set
        {
            if (!SetProperty(ref _selectedListeningHistorySort, value))
            {
                return;
            }

            TriggerListeningHistoryRefresh();
        }
    }

    public string SelectedListeningHistoryView
    {
        get => _selectedListeningHistoryView;
        set
        {
            if (!SetProperty(ref _selectedListeningHistoryView, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsListeningHistoryTimelineVisible));
            OnPropertyChanged(nameof(IsListeningHistoryRankingVisible));
        }
    }

    public bool IsListeningHistoryTimelineVisible => string.Equals(
        SelectedListeningHistoryView,
        "Dong thoi gian",
        StringComparison.Ordinal);

    public bool IsListeningHistoryRankingVisible => !IsListeningHistoryTimelineVisible;

    public async Task InitializeAsync()
    {
        EnsurePoiRefreshLoopStarted();

        if (_isInitialized)
        {
            await RefreshPoisIfChangedAsync();
            return;
        }

        StatusText = "Đang tải danh sách quán...";
        await RefreshPoisIfChangedAsync(forceRefresh: true);
        await InitializeMapFlowAsync();
        _isInitialized = true;
    }

    public async Task StartAsync(bool autoStart = false)
    {
        if (IsTracking)
        {
            return;
        }

        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        if (IsTracking)
        {
            return;
        }

        await StartTrackingCoreAsync(autoStart, requestPermissionIfNeeded: !autoStart);
    }

    public async Task StopAsync()
    {
        await _locationService.StopListeningAsync();
        IsTracking = false;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        UpdateMapBadges();

        if (_hasLocationPermission && _lastLocation is not null)
        {
            StatusText = "Đã dừng GPS, vẫn giữ vị trí cuối cùng";
        }
        else
        {
            StatusText = "Bản đồ đang dùng vị trí mặc định";
        }

        AddLog($"{NowLabel()} Dừng tracking GPS");
    }

    private async Task InitializeMapFlowAsync()
    {
        var hasPermission = await EnsureLocationPermissionAsync(requestIfNeeded: false);
        if (!hasPermission)
        {
            await ApplyFallbackMapStateAsync(
                "Bản đồ đang dùng vị trí mặc định",
                "Chưa cấp quyền truy cập vị trí. App vẫn hiển thị bản đồ và các POI đang hoạt động.");
            return;
        }

        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation is null)
        {
            await ApplyFallbackMapStateAsync(
                "Đã cấp quyền vị trí, đang chờ GPS",
                "App đã có quyền vị trí nhưng chưa lấy được tọa độ hiện tại.");
            await StartTrackingCoreAsync(autoStart: true, requestPermissionIfNeeded: false);
            return;
        }

        await ApplyLocationSnapshotAsync(currentLocation, allowAutoNarrate: false);
        await StartTrackingCoreAsync(autoStart: true, requestPermissionIfNeeded: false);
    }

    private async Task<bool> EnsureLocationPermissionAsync(bool requestIfNeeded)
    {
        var hasPermission = await _locationService.EnsurePermissionAsync(requestIfNeeded);
        _hasCheckedLocationPermission = true;
        _hasLocationPermission = hasPermission;

        if (!hasPermission)
        {
            _lastLocation = null;
        }

        await MainThread.InvokeOnMainThreadAsync(UpdateMapBadges);
        return hasPermission;
    }

    private async Task StartTrackingCoreAsync(bool autoStart, bool requestPermissionIfNeeded)
    {
        var hasPermission = _hasLocationPermission;
        if (!hasPermission)
        {
            hasPermission = await EnsureLocationPermissionAsync(requestPermissionIfNeeded);
        }

        if (!hasPermission)
        {
            await ApplyFallbackMapStateAsync(
                "Bản đồ đang dùng vị trí mặc định",
                requestPermissionIfNeeded
                    ? "Chưa cấp quyền truy cập vị trí. App vẫn hiển thị các POI đang hoạt động."
                    : "Chưa cấp quyền truy cập vị trí. App vẫn hiển thị bản đồ và các POI đang hoạt động.");
            return;
        }

        try
        {
            StatusText = "Đang khởi động GPS...";

            if (_lastLocation is null)
            {
                var currentLocation = await _locationService.GetCurrentLocationAsync();
                if (currentLocation is not null)
                {
                    await ApplyLocationSnapshotAsync(currentLocation, allowAutoNarrate: false);
                }
            }

            await _locationService.StartListeningAsync();
            IsTracking = true;
            UpdateMapBadges();
            StatusText = _lastLocation is null
                ? "GPS đang hoạt động, chờ vị trí đầu tiên"
                : "GPS đang hoạt động";
            AddLog($"{NowLabel()} {(autoStart ? "Khởi động" : "Bật")} tracking GPS");
        }
        catch (Exception ex)
        {
            if (!await EnsureLocationPermissionAsync(requestIfNeeded: false))
            {
                await ApplyFallbackMapStateAsync(
                    "Bản đồ đang dùng vị trí mặc định",
                    "Không thể theo dõi GPS vì quyền vị trí hiện chưa sẵn sàng.");
                AddLog($"{NowLabel()} Lỗi GPS: {ex.Message}");
                return;
            }

            StatusText = $"Không thể bật GPS: {ex.Message}";
            UpdateMapBadges();
            AddLog($"{NowLabel()} Lỗi GPS: {ex.Message}");
        }
    }

    public void ShowSearchSuggestions()
    {
        _isSearchFocused = true;
        RefreshSearchSuggestions();
    }

    public void HideSearchSuggestions()
    {
        _isSearchFocused = false;
        IsSearchSuggestionsVisible = false;
    }

    public void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    public async Task ResetHomeViewAsync()
    {
        if (IsNarrating)
        {
            await StopNarrationAsync();
        }

        _hasUserSelectedPoi = false;
        HideSearchSuggestions();
        ClearSearch();

        if (_pois.Count == 0)
        {
            LocationText = "Bạn đang xem cổng phố ẩm thực Vĩnh Khánh";
            NearestPoiText = "Chọn quán hoặc chạm bản đồ để nghe thuyết minh";
            StatusText = IsTracking
                ? "GPS đang hoạt động"
                : _hasLocationPermission
                    ? "GPS đã dừng"
                    : "Bản đồ đang dùng vị trí mặc định";
            UpdateMapBadges();
            RaiseMapStateChanged();
            return;
        }

        RefreshPoiList(Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>(), null);
        SetSelectedPoi(_pois.First(), false, null);

        LocationText = "Bạn đang xem cổng phố ẩm thực Vĩnh Khánh";
        NearestPoiText = "Chọn quán hoặc chạm bản đồ để nghe thuyết minh";
        StatusText = IsTracking
            ? "GPS đang hoạt động"
            : _hasLocationPermission
                ? "GPS đã dừng"
                : "Bản đồ đang dùng vị trí mặc định";

        UpdateMapBadges();
        RaiseMapStateChanged();
    }

    public bool ExecuteSearch(string? query = null)
    {
        if (query is not null)
        {
            SearchQuery = query.Trim();
        }
        else
        {
            SearchQuery = SearchQuery.Trim();
        }

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return false;
        }

        AddRecentSearch(SearchQuery);

        var matchedPoi = GetSearchMatches(SearchQuery)
            .Select(item => _pois.FirstOrDefault(poi => poi.Id == item.PoiId))
            .FirstOrDefault(poi => poi is not null);

        if (matchedPoi is null)
        {
            return false;
        }

        SetSelectedPoi(matchedPoi, true, null);
        return true;
    }

    public bool ApplySearchSuggestion(SearchSuggestionItem suggestion)
    {
        return ExecuteSearch(suggestion.Text);
    }

    public void SelectPoi(Guid poiId, bool userInitiated = true)
    {
        var poi = _pois.FirstOrDefault(item => item.Id == poiId);
        if (poi is null)
        {
            return;
        }

        SetSelectedPoi(poi, userInitiated, null);
    }

    public async Task NarrateSelectedPoiAsync()
    {
        if (_selectedPoi is null)
        {
            StatusText = "Chưa có quán được chọn";
            return;
        }

        if (IsCurrentNarration(_selectedPoi))
        {
            StatusText = $"Nội dung của {_selectedPoi.Name} đang được phát";
            AddLog($"{NowLabel()} Bỏ qua phát lại {_selectedPoi.Name} vì nội dung đang phát");
            return;
        }

        await NarratePoiAsync(_selectedPoi, false, GetDistanceForPoi(_selectedPoi.Id));
    }

    public async Task ToggleSelectedPoiNarrationAsync()
    {
        if (_selectedPoi is null)
        {
            StatusText = "Chưa có quán được chọn";
            return;
        }

        if (IsCurrentNarration(_selectedPoi))
        {
            await StopNarrationAsync();
            return;
        }

        await NarratePoiAsync(_selectedPoi, false, GetDistanceForPoi(_selectedPoi.Id));
    }

    public async Task TogglePoiNarrationAsync(Guid poiId)
    {
        var poi = _pois.FirstOrDefault(item => item.Id == poiId);
        if (poi is null)
        {
            return;
        }

        SetSelectedPoi(poi, true, null);

        if (IsCurrentNarration(poi))
        {
            await StopNarrationAsync();
            return;
        }

        await NarratePoiAsync(poi, false, GetDistanceForPoi(poi.Id));
    }

    public async Task StopNarrationAsync()
    {
        var hadActiveNarration = IsNarrating;

        Interlocked.Increment(ref _narrationSessionId);
        await _narrationService.StopAsync();

        SetActiveNarrationPoiId(null);
        IsNarrating = false;
        StatusText = hadActiveNarration
            ? "Đã dừng thuyết minh"
            : IsTracking
                ? "GPS đang hoạt động"
                : "Sẵn sàng phát thuyết minh";
        await MainThread.InvokeOnMainThreadAsync(() => RefreshNarrationPresentation());

        if (hadActiveNarration)
        {
            AddLog($"{NowLabel()} Dừng thuyết minh");
        }
    }

    public async Task SignOutAsync()
    {
        if (IsNarrating)
        {
            await StopNarrationAsync();
        }

        if (IsTracking)
        {
            await StopAsync();
        }

        _hasUserSelectedPoi = false;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        HideSearchSuggestions();
        ClearSearch();

        await _authService.SignOutAsync();
    }

    private void EnsurePoiRefreshLoopStarted()
    {
        if (_poiRefreshLoopTask is not null)
        {
            return;
        }

        _poiRefreshLoopTask = Task.Run(RunPoiRefreshLoopAsync);
    }

    private async Task RunPoiRefreshLoopAsync()
    {
        using var timer = new PeriodicTimer(PoiRefreshInterval);

        while (await timer.WaitForNextTickAsync())
        {
            await RefreshPoisIfChangedAsync();
        }
    }

    private async Task RefreshPoisIfChangedAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var latestPois = await _poiProvider.GetPoisAsync(cancellationToken);
        var snapshot = CreatePoiSnapshot(latestPois);

        if (!forceRefresh && string.Equals(_poiDataSnapshot, snapshot, StringComparison.Ordinal))
        {
            return;
        }

        await _locationUpdateGate.WaitAsync(cancellationToken);

        try
        {
            var previousSelectedPoiId = _selectedPoi?.Id;
            var previousSnapshot = _poiDataSnapshot;

            _pois = latestPois.ToList();
            _poiDataSnapshot = snapshot;
            CleanupPoiState();

            var nearestPoi = default(POI);
            IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> evaluated =
                Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>();

            if (_lastLocation is not null && _pois.Count > 0)
            {
                evaluated = _geofenceEngine.Evaluate(_lastLocation, _pois);
                nearestPoi = evaluated.FirstOrDefault().Poi;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RefreshPoiList(evaluated, nearestPoi?.Id);

                if (_lastLocation is not null)
                {
                    UpdateLocationSummary(
                        _lastLocation,
                        nearestPoi,
                        nearestPoi is null ? null : GetDistanceForPoi(nearestPoi.Id));
                }
                else if (_pois.Count == 0)
                {
                    NearestPoiText = "Chưa có POI đang hoạt động từ WebAdmin";
                }
                else
                {
                    NearestPoiText = "Chạm marker để xem chi tiết quán";
                }

                ApplySelectedPoiAfterRefresh(previousSelectedPoiId, nearestPoi, evaluated);
                UpdateMapBadges();
            });

            if (_isInitialized && !string.IsNullOrWhiteSpace(previousSnapshot))
            {
                AddLog($"{NowLabel()} Dong bo du lieu moi tu WebAdmin ({_pois.Count} POI)");
            }
        }
        finally
        {
            _locationUpdateGate.Release();
        }
    }

    public async Task HandleMapTapAsync(double latitude, double longitude)
    {
        if (_pois.Count == 0)
        {
            return;
        }

        var tapLocation = new LocationDto
        {
            Latitude = latitude,
            Longitude = longitude
        };

        var results = _geofenceEngine.Evaluate(tapLocation, _pois);
        var candidate = results
            .Where(item => item.IsInside)
            .OrderByDescending(item => item.Poi.Priority)
            .ThenBy(item => item.DistanceMeters)
            .FirstOrDefault();

        if (candidate.Poi is not null)
        {
            SetSelectedPoi(candidate.Poi, true, results);
            StatusText = $"Bạn vừa chạm vùng của {candidate.Poi.Name}";
            AddLog($"{NowLabel()} Chạm Mapsui trong bán kính {candidate.Poi.Name}");
            await NarratePoiAsync(candidate.Poi, false, candidate.DistanceMeters);
            return;
        }

        var nearest = results.FirstOrDefault();
        if (nearest.Poi is not null)
        {
            SetSelectedPoi(nearest.Poi, true, results);
            StatusText = $"Bạn vừa chạm ngoài vùng quán. Gần nhất là {nearest.Poi.Name}";
            AddLog($"{NowLabel()} Chạm bản đồ gần {nearest.Poi.Name}");
        }
    }

    private async void OnLocationUpdated(object? sender, LocationDto location)
    {
        if (!await _locationUpdateGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            await HandleLocationUpdatedAsync(location);
        }
        finally
        {
            _locationUpdateGate.Release();
        }
    }

    private void OnAuthSessionChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            SyncCurrentUser();
            LoadPersistedState();
            TriggerListeningHistoryRefresh();
        });
    }

    private async Task HandleLocationUpdatedAsync(LocationDto location)
    {
        await ApplyLocationSnapshotAsync(location, allowAutoNarrate: true);
    }

    private async Task ApplyLocationSnapshotAsync(LocationDto location, bool allowAutoNarrate)
    {
        _lastLocation = location;
        _hasCheckedLocationPermission = true;
        _hasLocationPermission = true;

        var results = _pois.Count == 0
            ? Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>()
            : _geofenceEngine.Evaluate(location, _pois);
        var nearest = results.FirstOrDefault();
        var insideNow = results
            .Where(item => item.IsInside)
            .Select(item => item.Poi.Id)
            .ToHashSet();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RefreshPoiList(results, nearest.Poi?.Id);
            UpdateLocationSummary(location, nearest.Poi, nearest.Poi is null ? null : nearest.DistanceMeters);

            if (_pois.Count == 0)
            {
                NearestPoiText = "Chưa có POI đang hoạt động từ WebAdmin";
            }
            else if (!_hasUserSelectedPoi)
            {
                SetSelectedPoi(nearest.Poi ?? _selectedPoi ?? _pois.FirstOrDefault(), false, results);
            }
            else
            {
                UpdateSelectedPoiDetails(results);
            }

            UpdateMapBadges();
        });

        if (allowAutoNarrate)
        {
            var candidate = results
                .Where(item => item.IsInside)
                .OrderByDescending(item => item.Poi.Priority)
                .ThenBy(item => item.DistanceMeters)
                .FirstOrDefault();

            if (IsAutoNarrationEnabled && candidate.Poi is not null && ShouldAutoNarrate(candidate.Poi))
            {
                await NarratePoiAsync(candidate.Poi, true, candidate.DistanceMeters);
            }
        }

        _insidePoiIds.Clear();
        foreach (var poiId in insideNow)
        {
            _insidePoiIds.Add(poiId);
        }

        if (_insidePoiIds.Count == 0)
        {
            _lastAutoNarratedPoiId = null;
        }

        RaiseMapStateChanged();
    }

    private async Task ApplyFallbackMapStateAsync(string statusText, string locationText)
    {
        _lastLocation = null;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            RefreshPoiList(Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>(), null);

            if (_pois.Count == 0)
            {
                _selectedPoi = null;
                _hasUserSelectedPoi = false;
                ClearSelectedPoiDetails();
                NearestPoiText = "Chưa có POI đang hoạt động từ WebAdmin";
            }
            else
            {
                NearestPoiText = "Chạm marker để xem chi tiết quán";

                if (_selectedPoi is null)
                {
                    SetSelectedPoi(_pois.FirstOrDefault(), false, null);
                }
                else
                {
                    UpdateSelectedPoiDetails();
                }
            }

            StatusText = statusText;
            LocationText = locationText;
            UpdateMapBadges();
            RaiseMapStateChanged();
        });
    }

    private void CleanupPoiState()
    {
        var activePoiIds = _pois
            .Select(poi => poi.Id)
            .ToHashSet();

        _insidePoiIds.RemoveWhere(poiId => !activePoiIds.Contains(poiId));

        if (_lastAutoNarratedPoiId.HasValue && !activePoiIds.Contains(_lastAutoNarratedPoiId.Value))
        {
            _lastAutoNarratedPoiId = null;
        }

        foreach (var stalePoiId in _lastNarratedAt.Keys.Where(poiId => !activePoiIds.Contains(poiId)).ToList())
        {
            _lastNarratedAt.Remove(stalePoiId);
        }
    }

    private void ApplySelectedPoiAfterRefresh(
        Guid? previousSelectedPoiId,
        POI? nearestPoi,
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> evaluated)
    {
        if (_pois.Count == 0)
        {
            _selectedPoi = null;
            _hasUserSelectedPoi = false;
            ClearSelectedPoiDetails();
            RaiseMapStateChanged();
            return;
        }

        var preservedSelection = previousSelectedPoiId.HasValue
            ? _pois.FirstOrDefault(item => item.Id == previousSelectedPoiId.Value)
            : null;

        if (preservedSelection is null && previousSelectedPoiId.HasValue)
        {
            _hasUserSelectedPoi = false;
        }

        var nextSelectedPoi = preservedSelection
            ?? (!_hasUserSelectedPoi ? nearestPoi : null)
            ?? _pois.FirstOrDefault();

        SetSelectedPoi(nextSelectedPoi, false, evaluated.Count > 0 ? evaluated : null);
    }

    private void UpdateLocationSummary(LocationDto location, POI? nearestPoi, double? nearestDistanceMeters)
    {
        LocationText =
            $"Lat {location.Latitude:F6} | Lng {location.Longitude:F6} | Sai so {location.AccuracyMeters?.ToString("F0") ?? "?"}m";

        NearestPoiText = nearestPoi is null
            ? "Chua xac dinh quan gan nhat"
            : $"Quan gan nhat: {nearestPoi.Name} ({nearestDistanceMeters?.ToString("F0") ?? "?"}m)";
    }

    private void UpdateMapBadges()
    {
        MapPoiBadgeText = _pois.Count == 0
            ? "0 POI"
            : $"{_pois.Count} POI hoạt động";

        MapModeBadgeText = IsTracking
            ? "GPS trực tiếp"
            : _lastLocation is not null
                ? "Đã có vị trí"
                : _hasCheckedLocationPermission
                    ? (_hasLocationPermission ? "Chờ GPS" : "Vị trí mặc định")
                    : "Đang kiểm tra GPS";
    }

    private bool ShouldAutoNarrate(POI poi)
    {
        var isNewEntry = !_insidePoiIds.Contains(poi.Id);
        var candidateChanged = _lastAutoNarratedPoiId != poi.Id;

        if (!isNewEntry && !candidateChanged)
        {
            return false;
        }

        return CanNarrate(poi);
    }

    private async Task NarratePoiAsync(POI poi, bool autoTriggered, double? distanceMeters)
    {
        if (IsCurrentNarration(poi))
        {
            StatusText = $"Nội dung của {poi.Name} đang được phát";
            return;
        }

        var listenStopwatch = Stopwatch.StartNew();
        var narrationSessionId = Interlocked.Increment(ref _narrationSessionId);
        var historyTask = Task.FromResult<Guid?>(null);
        string errorMessage = string.Empty;
        _lastNarratedAt[poi.Id] = DateTimeOffset.UtcNow;
        if (autoTriggered)
        {
            _lastAutoNarratedPoiId = poi.Id;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SetActiveNarrationPoiId(poi.Id);
            IsNarrating = true;
            StatusText = autoTriggered
                ? $"Tự động phát: {poi.Name}"
                : $"Đang phát: {poi.Name}";

            if (!_hasUserSelectedPoi)
            {
                SetSelectedPoi(poi, false, null);
            }

            RefreshNarrationPresentation();
        });

        AddLog(
            $"{NowLabel()} {(autoTriggered ? "Auto" : "Manual")} trigger {poi.Name}" +
            (distanceMeters.HasValue ? $" ({distanceMeters.Value:F0}m)" : string.Empty));
        AddListeningHistory(
            $"{NowLabel()} {(autoTriggered ? "Tự động nghe" : "Nghe thủ công")} {poi.Name}" +
            (distanceMeters.HasValue ? $" ({distanceMeters.Value:F0}m)" : string.Empty));

        historyTask = _listeningHistorySyncService.BeginAsync(poi, SelectedLanguage, autoTriggered);
        _ = RefreshListeningHistoryAfterCreateAsync(historyTask);

        try
        {
            await _narrationService.NarrateAsync(poi, SelectedLanguage);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            AddLog($"{NowLabel()} Lỗi âm thanh: {ex.Message}");

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                StatusText = $"Lỗi phát âm thanh: {ex.Message}";
            }
        }
        finally
        {
            listenStopwatch.Stop();

            var historyId = await historyTask;
            if (historyId.HasValue)
            {
                var completed = string.IsNullOrWhiteSpace(errorMessage)
                    && narrationSessionId == Volatile.Read(ref _narrationSessionId);

                await _listeningHistorySyncService.CompleteAsync(
                    historyId.Value,
                    (int)Math.Round(listenStopwatch.Elapsed.TotalSeconds),
                    completed,
                    completed ? string.Empty : errorMessage);

                await RefreshListeningHistoryAsync();
            }

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetActiveNarrationPoiId(null);
                    IsNarrating = false;
                    RefreshNarrationPresentation();

                    if (!StatusText.StartsWith("Lỗi phát âm thanh:", StringComparison.Ordinal))
                    {
                        StatusText = IsTracking ? "GPS đang hoạt động" : "Sẵn sàng phát thuyết minh";
                    }
                });
            }
        }
    }

    private void RefreshPoiList(
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> evaluated,
        Guid? nearestPoiId)
    {
        var lookup = evaluated.ToDictionary(item => item.Poi.Id);

        PoiStatuses.Clear();

        foreach (var poi in _pois)
        {
            lookup.TryGetValue(poi.Id, out var evaluatedItem);
            var hasDistance = evaluatedItem.Poi is not null;

            PoiStatuses.Add(new PoiStatusItem
            {
                PoiId = poi.Id,
                Code = poi.Code,
                Name = poi.Name,
                Address = poi.Address,
                ImageSource = poi.ImageSource,
                Description = poi.Description,
                SpecialDish = poi.SpecialDish,
                MapLink = poi.MapLink,
                Latitude = poi.Latitude,
                Longitude = poi.Longitude,
                DistanceMeters = hasDistance ? evaluatedItem.DistanceMeters : double.NaN,
                TriggerRadiusMeters = poi.TriggerRadiusMeters,
                IsInsideRadius = hasDistance && evaluatedItem.IsInside,
                IsNearest = nearestPoiId == poi.Id,
                IsNarrationActive = IsNarrating && _activeNarrationPoiId == poi.Id,
                Priority = poi.Priority
            });
        }

        UpdateFilteredPoiStatuses();
        RefreshSearchSuggestions();
    }

    private void SetSelectedPoi(
        POI? poi,
        bool userInitiated,
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)>? evaluated)
    {
        if (poi is null)
        {
            return;
        }

        _selectedPoi = poi;
        _hasUserSelectedPoi = userInitiated || _hasUserSelectedPoi;
        UpdateSelectedPoiDetails(evaluated);
        OnPropertyChanged(nameof(IsSelectedPoiNarrating));
        OnPropertyChanged(nameof(SelectedPoiNarrationActionText));

        if (userInitiated)
        {
            AddViewHistory($"{NowLabel()} Xem thông tin quán {poi.Name}");
        }

        RaiseMapStateChanged();
    }

    private void UpdateSelectedPoiDetails(
        IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)>? evaluated = null)
    {
        if (_selectedPoi is null)
        {
            return;
        }

        double? distanceMeters = null;

        if (evaluated is not null)
        {
            var matchedEvaluation = evaluated.FirstOrDefault(item => item.Poi.Id == _selectedPoi.Id);
            if (matchedEvaluation.Poi is not null)
            {
                distanceMeters = matchedEvaluation.DistanceMeters;
            }
        }

        if (!distanceMeters.HasValue)
        {
            var status = PoiStatuses.FirstOrDefault(item => item.PoiId == _selectedPoi.Id);
            distanceMeters = status is null || double.IsNaN(status.DistanceMeters)
                ? null
                : status.DistanceMeters;
        }

        var distanceLabel = distanceMeters.HasValue
            ? $"Khoảng cách hiện tại: {distanceMeters.Value:F0}m"
            : "Khoảng cách hiện tại: N/A";

        SelectedPoiName = _selectedPoi.Name;
        SelectedPoiAddress = _selectedPoi.Address;
        SelectedPoiDescription = _selectedPoi.Description;
        SelectedPoiDishText = _selectedPoi.SpecialDish;
        SelectedPoiStatusText =
            $"{distanceLabel} | Bán kính phát: {_selectedPoi.TriggerRadiusMeters:F0}m | Ưu tiên: P{_selectedPoi.Priority}";
        SelectedPoiNarrationPreview = _selectedPoi.GetNarrationText(SelectedLanguage);
        SelectedPoiMapLink = _selectedPoi.MapLink;
        SelectedPoiImageSource = _selectedPoi.ImageSource;
    }

    private void ClearSelectedPoiDetails()
    {
        SelectedPoiName = "Chưa có POI đang hoạt động";
        SelectedPoiAddress = "Cập nhật POI trong WebAdmin để app hiển thị lại.";
        SelectedPoiDescription = string.Empty;
        SelectedPoiDishText = string.Empty;
        SelectedPoiStatusText = string.Empty;
        SelectedPoiNarrationPreview = string.Empty;
        SelectedPoiMapLink = string.Empty;
        SelectedPoiImageSource = string.Empty;
        OnPropertyChanged(nameof(IsSelectedPoiNarrating));
        OnPropertyChanged(nameof(SelectedPoiNarrationActionText));
    }

    private double? GetDistanceForPoi(Guid poiId)
    {
        var status = PoiStatuses.FirstOrDefault(item => item.PoiId == poiId);
        return status is null || double.IsNaN(status.DistanceMeters)
            ? null
            : status.DistanceMeters;
    }

    private bool CanNarrate(POI poi)
    {
        if (!_lastNarratedAt.TryGetValue(poi.Id, out var lastNarratedAt))
        {
            return true;
        }

        return DateTimeOffset.UtcNow - lastNarratedAt >= TimeSpan.FromMinutes(poi.CooldownMinutes);
    }

    private bool IsCurrentNarration(POI poi) => IsNarrating && _activeNarrationPoiId == poi.Id;

    private void SetActiveNarrationPoiId(Guid? poiId)
    {
        if (_activeNarrationPoiId == poiId)
        {
            return;
        }

        _activeNarrationPoiId = poiId;
        OnPropertyChanged(nameof(IsSelectedPoiNarrating));
        OnPropertyChanged(nameof(SelectedPoiNarrationActionText));
    }

    private IReadOnlyList<(POI Poi, double DistanceMeters, bool IsInside)> EvaluateCurrentPoiStatuses()
    {
        if (_lastLocation is null || _pois.Count == 0)
        {
            return Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>();
        }

        return _geofenceEngine.Evaluate(_lastLocation, _pois);
    }

    private void RefreshNarrationPresentation()
    {
        var evaluated = EvaluateCurrentPoiStatuses();
        var nearestPoi = evaluated.FirstOrDefault().Poi;

        RefreshPoiList(evaluated, nearestPoi?.Id);

        if (_selectedPoi is not null)
        {
            UpdateSelectedPoiDetails(evaluated.Count > 0 ? evaluated : null);
        }

        RaiseMapStateChanged();
    }

    private void AddLog(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            EventLogs.Insert(0, message);
            _usageHistoryService.AppendEntry(UsageHistoryCategory.Activity, message);

            while (EventLogs.Count > 15)
            {
                EventLogs.RemoveAt(EventLogs.Count - 1);
            }

            RaiseEventLogStateChanged();
        });
    }

    private void RaiseMapStateChanged()
    {
        MainThread.BeginInvokeOnMainThread(() => MapStateChanged?.Invoke(this, EventArgs.Empty));
    }

    private void UpdateFilteredPoiStatuses()
    {
        var visibleItems = string.IsNullOrWhiteSpace(SearchQuery)
            ? PoiStatuses.ToList()
            : GetSearchMatches(SearchQuery);

        FilteredPoiStatuses.Clear();

        foreach (var item in visibleItems)
        {
            FilteredPoiStatuses.Add(item);
        }

        IsSearchResultEmpty = HasSearchQuery && FilteredPoiStatuses.Count == 0;
    }

    private List<PoiStatusItem> GetSearchMatches(string query)
    {
        var normalizedQuery = NormalizeForSearch(query);

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return PoiStatuses.ToList();
        }

        return PoiStatuses
            .Where(item => ContainsNormalized(item.Name, normalizedQuery))
            .OrderBy(item => !NormalizeForSearch(item.Name).StartsWith(normalizedQuery, StringComparison.Ordinal))
            .ThenBy(item => item.DistanceMeters)
            .ThenBy(item => item.Name)
            .ToList();
    }

    private void RefreshSearchSuggestions()
    {
        SearchSuggestions.Clear();

        if (!_isSearchFocused || PoiStatuses.Count == 0)
        {
            IsSearchSuggestionsVisible = false;
            return;
        }

        var query = SearchQuery.Trim();
        var suggestions = new List<SearchSuggestionItem>();
        var seenTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddSuggestion(string text, string supportingText, bool isRecent)
        {
            if (string.IsNullOrWhiteSpace(text) || !seenTexts.Add(text))
            {
                return;
            }

            suggestions.Add(new SearchSuggestionItem
            {
                Text = text,
                SupportingText = supportingText,
                IsRecent = isRecent
            });
        }

        if (_recentSearches.Count > 0)
        {
            IEnumerable<string> recentItems;

            if (string.IsNullOrWhiteSpace(query))
            {
                recentItems = _recentSearches.Take(MaxPinnedRecentSuggestions);
            }
            else
            {
                var normalizedQuery = NormalizeForSearch(query);
                var matchedRecentItems = _recentSearches
                    .Where(item => ContainsNormalized(item, normalizedQuery))
                    .Take(MaxPinnedRecentSuggestions)
                    .ToList();

                recentItems = matchedRecentItems.Count > 0
                    ? matchedRecentItems
                    : _recentSearches.Take(MaxPinnedRecentSuggestions);
            }

            foreach (var recent in recentItems)
            {
                AddSuggestion(recent, "Tìm gần đây", true);

                if (suggestions.Count >= MaxSearchSuggestions)
                {
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            foreach (var poi in PoiStatuses)
            {
                AddSuggestion(poi.Name, poi.Address, false);

                if (suggestions.Count >= MaxSearchSuggestions)
                {
                    break;
                }
            }
        }
        else
        {
            foreach (var poi in GetSearchMatches(query))
            {
                AddSuggestion(poi.Name, poi.Address, false);

                if (suggestions.Count >= MaxSearchSuggestions)
                {
                    break;
                }
            }
        }

        foreach (var suggestion in suggestions.Take(MaxSearchSuggestions))
        {
            SearchSuggestions.Add(suggestion);
        }

        IsSearchSuggestionsVisible = SearchSuggestions.Count > 0;
    }

    private void AddRecentSearch(string query)
    {
        var trimmedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return;
        }

        var existingIndex = _recentSearches.FindIndex(item =>
            string.Equals(item, trimmedQuery, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            _recentSearches.RemoveAt(existingIndex);
        }

        _recentSearches.Insert(0, trimmedQuery);

        while (_recentSearches.Count > MaxRecentSearches)
        {
            _recentSearches.RemoveAt(_recentSearches.Count - 1);
        }

        RefreshSearchSuggestions();
    }

    private static bool ContainsNormalized(string source, string query)
    {
        var normalizedSource = NormalizeForSearch(source);
        return normalizedSource.Contains(query, StringComparison.Ordinal);
    }

    private void TriggerListeningHistoryRefresh()
    {
        _ = RefreshListeningHistoryAsync();
    }

    private async Task RefreshListeningHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _listeningHistoryRefreshGate.WaitAsync(cancellationToken);

        try
        {
            IsListeningHistoryLoading = true;
            ListeningHistoryLoadError = string.Empty;
            OnPropertyChanged(nameof(ListeningHistorySummary));

            var sortBy = MapListeningHistorySortToApi(SelectedListeningHistorySort);
            var period = MapListeningHistoryPeriodToApi(SelectedListeningHistoryPeriod);

            var timelineTask = _listeningHistorySyncService.GetCurrentUserHistoryAsync(
                sortBy,
                period,
                15,
                cancellationToken);
            var rankingTask = _listeningHistorySyncService.GetCurrentUserRankingAsync(period, cancellationToken);

            await Task.WhenAll(timelineTask, rankingTask);

            var timelineItems = timelineTask.Result
                .Select(ToListeningHistoryDisplayItem)
                .ToList();
            var rankingItems = rankingTask.Result
                .Select((item, index) => ToListeningHistoryRankingDisplayItem(item, index))
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ReplaceCollection(ListeningHistory, timelineItems);
                ReplaceCollection(ListeningHistoryRanking, rankingItems);
                _lastListeningHistorySyncAt = DateTimeOffset.Now;

                RaiseListeningHistoryStateChanged();
                OnPropertyChanged(nameof(HasListeningHistoryRanking));
                OnPropertyChanged(nameof(ListeningHistorySyncStatus));
            });
        }
        catch (Exception ex)
        {
            ListeningHistoryLoadError = $"Khong tai duoc lich su nghe: {ex.Message}";
        }
        finally
        {
            IsListeningHistoryLoading = false;
            OnPropertyChanged(nameof(ListeningHistorySummary));
            _listeningHistoryRefreshGate.Release();
        }
    }

    private async Task RefreshListeningHistoryAfterCreateAsync(Task<Guid?> historyTask)
    {
        try
        {
            var historyId = await historyTask;
            if (historyId.HasValue)
            {
                await RefreshListeningHistoryAsync();
            }
        }
        catch
        {
            // Ignore background refresh failures and keep narration flow responsive.
        }
    }

    private static ListeningHistoryDisplayItem ToListeningHistoryDisplayItem(ListeningHistoryEntryDto item)
    {
        var startedAtLocal = item.StartedAtUtc.ToLocalTime();
        var triggerLabel = item.AutoTriggered || string.Equals(item.TriggerType, "GPS", StringComparison.OrdinalIgnoreCase)
            ? "Tu dong"
            : "Thu cong";
        var durationLabel = item.ListenSeconds > 0
            ? $"{item.ListenSeconds} giay"
            : "Dang ghi nhan";

        var statusLabel = item.Completed
            ? "Hoan tat"
            : string.IsNullOrWhiteSpace(item.ErrorMessage)
                ? "Dang nghe / dung som"
                : "Dung vi loi";

        var statusAccentColor = item.Completed
            ? "#15803D"
            : string.IsNullOrWhiteSpace(item.ErrorMessage)
                ? "#C2410C"
                : "#B91C1C";

        var detailParts = new List<string>
        {
            triggerLabel,
            item.Language,
            durationLabel
        };

        if (!string.IsNullOrWhiteSpace(item.DevicePlatform))
        {
            detailParts.Add(item.DevicePlatform);
        }

        return new ListeningHistoryDisplayItem
        {
            Id = item.Id,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            StartedAtLabel = startedAtLocal.ToString("dd/MM/yyyy HH:mm:ss"),
            DetailLabel = string.Join(" • ", detailParts.Where(part => !string.IsNullOrWhiteSpace(part))),
            StatusLabel = statusLabel,
            StatusAccentColor = statusAccentColor,
            ErrorMessage = item.ErrorMessage
        };
    }

    private static ListeningHistoryRankingDisplayItem ToListeningHistoryRankingDisplayItem(
        PoiListeningCountDto item,
        int index)
    {
        var completionRate = item.ListenCount == 0
            ? 0
            : (int)Math.Round(item.CompletedCount * 100.0 / item.ListenCount);

        return new ListeningHistoryRankingDisplayItem
        {
            Rank = index + 1,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            SummaryLabel = $"{item.ListenCount} luot nghe • {item.TotalListenSeconds} giay • {completionRate}% hoan tat",
            LastStartedAtLabel = item.LastStartedAtUtc.HasValue
                ? item.LastStartedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : "--"
        };
    }

    private static string MapListeningHistorySortToApi(string? selectedSort)
    {
        return string.Equals(selectedSort, "Cu nhat truoc", StringComparison.Ordinal)
            ? "time_asc"
            : "time_desc";
    }

    private static string MapListeningHistoryPeriodToApi(string? selectedPeriod)
    {
        return selectedPeriod switch
        {
            "24 gio qua" => "day",
            "7 ngay qua" => "week",
            "30 ngay qua" => "month",
            _ => "all"
        };
    }

    private static void ReplaceCollection<T>(
        ObservableCollection<T> collection,
        IReadOnlyList<T> items)
    {
        collection.Clear();

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    private void SyncCurrentUser()
    {
        var session = _authService.CurrentSession;
        var loginId = session?.LoginId ?? "guest";

        CurrentUserDisplayName = session?.FullName ?? "Khách";
        CurrentUserInitials = session?.Initials ?? "VK";
        CurrentUserAccountLabel = loginId;
        CurrentUserPasswordLabel = session is null
            ? "Chưa đăng nhập"
            : string.Equals(loginId, "user", StringComparison.OrdinalIgnoreCase)
                ? "12345678 (mặc định)"
                : "•••••••• (đã ẩn)";
        CurrentUserStatusLine = session is null
            ? "Khách khám phá"
            : $"{session.RoleLabel} • @{loginId}";
    }

    private void LoadPersistedState()
    {
        LoadUserPreferences();
        LoadPersistedHistory(EventLogs, UsageHistoryCategory.Activity);
        LoadPersistedHistory(ViewHistory, UsageHistoryCategory.Viewing);
        RaiseEventLogStateChanged();
        RaiseListeningHistoryStateChanged();
        RaiseViewHistoryStateChanged();
        TriggerListeningHistoryRefresh();
    }

    private void ClearEventLogs()
    {
        EventLogs.Clear();
        _usageHistoryService.ClearEntries(UsageHistoryCategory.Activity);
        RaiseEventLogStateChanged();
    }

    private void ClearListeningHistory()
    {
        ListeningHistory.Clear();
        ListeningHistoryRanking.Clear();
        ListeningHistoryLoadError = string.Empty;
        _lastListeningHistorySyncAt = null;
        RaiseListeningHistoryStateChanged();
        OnPropertyChanged(nameof(HasListeningHistoryRanking));
        OnPropertyChanged(nameof(ListeningHistorySyncStatus));
    }

    private void ClearViewHistory()
    {
        ViewHistory.Clear();
        _usageHistoryService.ClearEntries(UsageHistoryCategory.Viewing);
        RaiseViewHistoryStateChanged();
    }

    private void RaiseEventLogStateChanged()
    {
        OnPropertyChanged(nameof(HasEventLogs));
        OnPropertyChanged(nameof(IsEventLogEmpty));
        OnPropertyChanged(nameof(EventLogSummary));
        (ClearEventLogsCommand as Command)?.ChangeCanExecute();
    }

    private void RaiseListeningHistoryStateChanged()
    {
        OnPropertyChanged(nameof(HasListeningHistory));
        OnPropertyChanged(nameof(IsListeningHistoryEmpty));
        OnPropertyChanged(nameof(HasListeningHistoryRanking));
        OnPropertyChanged(nameof(ListeningHistorySummary));
        (ClearListeningHistoryCommand as Command)?.ChangeCanExecute();
        (RefreshListeningHistoryCommand as Command)?.ChangeCanExecute();
    }

    private void RaiseViewHistoryStateChanged()
    {
        OnPropertyChanged(nameof(HasViewHistory));
        OnPropertyChanged(nameof(IsViewHistoryEmpty));
        OnPropertyChanged(nameof(ViewHistorySummary));
        (ClearViewHistoryCommand as Command)?.ChangeCanExecute();
    }

    private void AddListeningHistory(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        TriggerListeningHistoryRefresh();
    }

    private void AddViewHistory(string message)
    {
        AddHistoryEntry(ViewHistory, UsageHistoryCategory.Viewing, message, RaiseViewHistoryStateChanged);
    }

    private void AddHistoryEntry(
        ObservableCollection<string> targetCollection,
        UsageHistoryCategory category,
        string message,
        Action stateChangedCallback)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            targetCollection.Insert(0, message);
            _usageHistoryService.AppendEntry(category, message);

            while (targetCollection.Count > 15)
            {
                targetCollection.RemoveAt(targetCollection.Count - 1);
            }

            stateChangedCallback();
        });
    }

    private void LoadPersistedHistory(
        ObservableCollection<string> targetCollection,
        UsageHistoryCategory category)
    {
        targetCollection.Clear();

        foreach (var entry in _usageHistoryService.LoadEntries(category).Take(15))
        {
            targetCollection.Add(entry);
        }
    }

    private void LoadUserPreferences()
    {
        _isRestoringUserPreferences = true;

        try
        {
            var languageKey = $"{GetCurrentUserPreferencePrefix()}.language";
            var savedLanguage = Preferences.Default.Get(languageKey, "vi");

            if (!SupportedLanguages.Contains(savedLanguage, StringComparer.OrdinalIgnoreCase))
            {
                savedLanguage = "vi";
            }

            if (!string.Equals(_selectedLanguage, savedLanguage, StringComparison.OrdinalIgnoreCase))
            {
                _selectedLanguage = savedLanguage;
                OnPropertyChanged(nameof(SelectedLanguage));
            }

            var autoNarrationKey = $"{GetCurrentUserPreferencePrefix()}.autoNarration";
            var savedAutoNarration = Preferences.Default.Get(autoNarrationKey, true);

            if (_isAutoNarrationEnabled != savedAutoNarration)
            {
                _isAutoNarrationEnabled = savedAutoNarration;
                OnPropertyChanged(nameof(IsAutoNarrationEnabled));
            }
        }
        finally
        {
            _isRestoringUserPreferences = false;
        }

        UpdateSelectedPoiDetails();
        OnPropertyChanged(nameof(SelectedLanguageDisplayName));
        OnPropertyChanged(nameof(AudioSettingsSummary));
    }

    private void PersistUserPreferences()
    {
        if (_isRestoringUserPreferences)
        {
            return;
        }

        var preferencePrefix = GetCurrentUserPreferencePrefix();
        Preferences.Default.Set($"{preferencePrefix}.language", SelectedLanguage);
        Preferences.Default.Set($"{preferencePrefix}.autoNarration", IsAutoNarrationEnabled);
    }

    private string GetCurrentUserPreferencePrefix()
    {
        var scope = _authService.CurrentSession?.ScopeKey;
        if (string.IsNullOrWhiteSpace(scope))
        {
            scope = "guest";
        }

        return $"{UserPreferenceKeyPrefix}.{scope}";
    }

    private static string GetLanguageDisplayName(string? languageCode) => languageCode?.ToLowerInvariant() switch
    {
        "vi" => "Tiếng Việt",
        "en" => "English",
        "zh" => "中文",
        "ko" => "한국어",
        "fr" => "Français",
        _ => "Tiếng Việt"
    };

    private static string NormalizeForSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CreatePoiSnapshot(IEnumerable<POI> pois)
    {
        var builder = new StringBuilder();

        foreach (var poi in pois.OrderBy(item => item.Id))
        {
            builder
                .Append(poi.Id).Append('|')
                .Append(poi.Code).Append('|')
                .Append(poi.Name).Append('|')
                .Append(poi.Category).Append('|')
                .Append(poi.ImageSource).Append('|')
                .Append(poi.Address).Append('|')
                .Append(poi.Description).Append('|')
                .Append(poi.SpecialDish).Append('|')
                .Append(poi.NarrationText).Append('|')
                .Append(poi.MapLink).Append('|')
                .Append(poi.AudioAssetPath).Append('|')
                .Append(poi.Priority).Append('|')
                .Append(poi.Latitude).Append('|')
                .Append(poi.Longitude).Append('|')
                .Append(poi.TriggerRadiusMeters).Append('|')
                .Append(poi.CooldownMinutes).Append('|')
                .Append(poi.IsActive);

            foreach (var translation in poi.NarrationTranslations.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                builder
                    .Append('|')
                    .Append(translation.Key)
                    .Append('=')
                    .Append(translation.Value);
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string NowLabel() => DateTime.Now.ToString("HH:mm:ss");

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

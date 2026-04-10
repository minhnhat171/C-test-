using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.Core.Interfaces;
using VinhKhanhGuide.Core.Models;

namespace VinhKhanhGuide.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private const int MaxRecentSearches = 6;
    private const int MaxSearchSuggestions = 6;
    private const int MaxPinnedRecentSuggestions = 3;
    private const string UserPreferenceKeyPrefix = "vinhkhanh.user.preferences.v1";

    public const double EntranceLatitude = 10.7614500;
    public const double EntranceLongitude = 106.7028200;
    public const string EntranceName = "Cổng phố ẩm thực Vĩnh Khánh";
    public const string EntranceAddress = "40 Vĩnh Khánh, P. Khánh Hội, Q.4";

    private readonly ILocationService _locationService;
    private readonly IPoiProvider _poiProvider;
    private readonly INarrationService _narrationService;
    private readonly IAuthService _authService;
    private readonly IUsageHistoryService _usageHistoryService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly SemaphoreSlim _locationUpdateGate = new(1, 1);
    private readonly Dictionary<Guid, DateTimeOffset> _lastNarratedAt = new();
    private readonly HashSet<Guid> _insidePoiIds = [];
    private readonly List<string> _recentSearches = [];

    private IReadOnlyList<POI> _pois = Array.Empty<POI>();
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
    private POI? _selectedPoi;
    private bool _hasUserSelectedPoi;
    private string _statusText = "Đang chờ khởi động GPS";
    private string _locationText = "Bạn đang xem cổng phố ẩm thực Vĩnh Khánh";
    private string _nearestPoiText = "Chọn quán hoặc chạm bản đồ để nghe thuyết minh";
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

    public MainViewModel(
        ILocationService locationService,
        IPoiProvider poiProvider,
        INarrationService narrationService,
        IAuthService authService,
        IUsageHistoryService usageHistoryService,
        GeofenceEngine geofenceEngine)
    {
        _locationService = locationService;
        _poiProvider = poiProvider;
        _narrationService = narrationService;
        _authService = authService;
        _usageHistoryService = usageHistoryService;
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
    public ObservableCollection<string> ListeningHistory { get; } = new();
    public ObservableCollection<string> ViewHistory { get; } = new();
    public IReadOnlyList<string> SupportedLanguages { get; } = ["vi", "en", "zh", "ko", "fr"];

    public ICommand StartTrackingCommand { get; }
    public ICommand StopTrackingCommand { get; }
    public ICommand StopNarrationCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand ClearEventLogsCommand { get; }
    public ICommand ClearListeningHistoryCommand { get; }
    public ICommand ClearViewHistoryCommand { get; }

    public IReadOnlyList<POI> Pois => _pois;
    public LocationDto? LastLocation => _lastLocation;
    public Guid? SelectedPoiId => _selectedPoi?.Id;

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

    public bool HasEventLogs => EventLogs.Count > 0;

    public bool IsEventLogEmpty => EventLogs.Count == 0;

    public bool HasListeningHistory => ListeningHistory.Count > 0;

    public bool IsListeningHistoryEmpty => ListeningHistory.Count == 0;

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

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _pois = await _poiProvider.GetPoisAsync();
        RefreshPoiList(Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>(), null);
        SetSelectedPoi(_pois.FirstOrDefault(), false, null);
        _isInitialized = true;
        RaiseMapStateChanged();
    }

    public async Task StartAsync()
    {
        if (IsTracking)
        {
            return;
        }

        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        try
        {
            StatusText = "Đang khởi động GPS...";
            await _locationService.StartListeningAsync();
            IsTracking = true;
            StatusText = "GPS đang hoạt động";
            AddLog($"{NowLabel()} Bật tracking GPS");
        }
        catch (Exception ex)
        {
            StatusText = $"Không thể bật GPS: {ex.Message}";
            AddLog($"{NowLabel()} Lỗi GPS: {ex.Message}");
        }
    }

    public async Task StopAsync()
    {
        await _locationService.StopListeningAsync();
        IsTracking = false;
        _insidePoiIds.Clear();
        _lastAutoNarratedPoiId = null;
        StatusText = "GPS đã dừng";
        AddLog($"{NowLabel()} Dừng tracking GPS");
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
            StatusText = IsTracking ? "GPS đang hoạt động" : "GPS đã dừng";
            RaiseMapStateChanged();
            return;
        }

        RefreshPoiList(Array.Empty<(POI Poi, double DistanceMeters, bool IsInside)>(), null);
        SetSelectedPoi(_pois.First(), false, null);

        LocationText = "Bạn đang xem cổng phố ẩm thực Vĩnh Khánh";
        NearestPoiText = "Chọn quán hoặc chạm bản đồ để nghe thuyết minh";
        StatusText = IsTracking ? "GPS đang hoạt động" : "GPS đã dừng";

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

        await NarratePoiAsync(_selectedPoi, false, GetDistanceForPoi(_selectedPoi.Id));
    }

    public async Task StopNarrationAsync()
    {
        var hadActiveNarration = IsNarrating;

        Interlocked.Increment(ref _narrationSessionId);
        await _narrationService.StopAsync();

        IsNarrating = false;
        StatusText = hadActiveNarration
            ? "Đã dừng thuyết minh"
            : IsTracking
                ? "GPS đang hoạt động"
                : "Sẵn sàng phát thuyết minh";

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
        });
    }

    private async Task HandleLocationUpdatedAsync(LocationDto location)
    {
        if (_pois.Count == 0)
        {
            return;
        }

        var results = _geofenceEngine.Evaluate(location, _pois);
        var nearest = results.FirstOrDefault();
        var insideNow = results
            .Where(item => item.IsInside)
            .Select(item => item.Poi.Id)
            .ToHashSet();

        _lastLocation = location;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            LocationText =
                $"Lat {location.Latitude:F6} | Lng {location.Longitude:F6} | Sai số {location.AccuracyMeters?.ToString("F0") ?? "?"}m";

            NearestPoiText = nearest.Poi is null
                ? "Chưa xác định quán gần nhất"
                : $"Quán gần nhất: {nearest.Poi.Name} ({nearest.DistanceMeters:F0}m)";

            RefreshPoiList(results, nearest.Poi?.Id);

            if (!_hasUserSelectedPoi)
            {
                SetSelectedPoi(nearest.Poi ?? _selectedPoi ?? _pois.FirstOrDefault(), false, results);
            }
            else
            {
                UpdateSelectedPoiDetails(results);
            }
        });

        var candidate = results
            .Where(item => item.IsInside)
            .OrderByDescending(item => item.Poi.Priority)
            .ThenBy(item => item.DistanceMeters)
            .FirstOrDefault();

        if (IsAutoNarrationEnabled && candidate.Poi is not null && ShouldAutoNarrate(candidate.Poi))
        {
            await NarratePoiAsync(candidate.Poi, true, candidate.DistanceMeters);
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
        var narrationSessionId = Interlocked.Increment(ref _narrationSessionId);
        _lastNarratedAt[poi.Id] = DateTimeOffset.UtcNow;
        if (autoTriggered)
        {
            _lastAutoNarratedPoiId = poi.Id;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            IsNarrating = true;
            StatusText = autoTriggered
                ? $"Tự động phát: {poi.Name}"
                : $"Đang phát: {poi.Name}";

            if (!_hasUserSelectedPoi)
            {
                SetSelectedPoi(poi, false, null);
            }
        });

        AddLog(
            $"{NowLabel()} {(autoTriggered ? "Auto" : "Manual")} trigger {poi.Name}" +
            (distanceMeters.HasValue ? $" ({distanceMeters.Value:F0}m)" : string.Empty));
        AddListeningHistory(
            $"{NowLabel()} {(autoTriggered ? "Tự động nghe" : "Nghe thủ công")} {poi.Name}" +
            (distanceMeters.HasValue ? $" ({distanceMeters.Value:F0}m)" : string.Empty));

        try
        {
            await _narrationService.NarrateAsync(poi, SelectedLanguage);
        }
        catch (Exception ex)
        {
            AddLog($"{NowLabel()} Lỗi âm thanh: {ex.Message}");

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                StatusText = $"Lỗi phát âm thanh: {ex.Message}";
            }
        }
        finally
        {
            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsNarrating = false;

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

    private void SyncCurrentUser()
    {
        var session = _authService.CurrentSession;

        CurrentUserDisplayName = session?.FullName ?? "Khách";
        CurrentUserInitials = session?.Initials ?? "VK";
        CurrentUserAccountLabel = session?.Email ?? "guest";
        CurrentUserPasswordLabel = session is null
            ? "Chưa đăng nhập"
            : string.Equals(session.Email, "user", StringComparison.OrdinalIgnoreCase)
                ? "12345 (mặc định)"
                : "•••••••• (đã ẩn)";
        CurrentUserStatusLine = session is null
            ? "Khách khám phá"
            : $"{session.RoleLabel} • {session.Email}";
    }

    private void LoadPersistedState()
    {
        LoadUserPreferences();
        LoadPersistedHistory(EventLogs, UsageHistoryCategory.Activity);
        LoadPersistedHistory(ListeningHistory, UsageHistoryCategory.Listening);
        LoadPersistedHistory(ViewHistory, UsageHistoryCategory.Viewing);
        RaiseEventLogStateChanged();
        RaiseListeningHistoryStateChanged();
        RaiseViewHistoryStateChanged();
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
        _usageHistoryService.ClearEntries(UsageHistoryCategory.Listening);
        RaiseListeningHistoryStateChanged();
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
        OnPropertyChanged(nameof(ListeningHistorySummary));
        (ClearListeningHistoryCommand as Command)?.ChangeCanExecute();
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
        AddHistoryEntry(ListeningHistory, UsageHistoryCategory.Listening, message, RaiseListeningHistoryStateChanged);
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
        var scope = _authService.CurrentSession?.Email?.Trim().ToLowerInvariant();
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

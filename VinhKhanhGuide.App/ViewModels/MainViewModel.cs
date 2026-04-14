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
    private const string DefaultNarrationMode = "tts";
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
    private readonly IPoiRepository _poiRepository;
    private readonly IPoiOfflineStore _poiOfflineStore;
    private readonly ISearchService _searchService;
    private readonly INarrationService _narrationService;
    private readonly IAuthService _authService;
    private readonly IAudioSettingsService _audioSettingsService;
    private readonly IAudioAssetCacheService _audioAssetCacheService;
    private readonly IAccountProfileValidationService _accountProfileValidationService;
    private readonly IUsageHistoryService _usageHistoryService;
    private readonly IListeningHistorySyncService _listeningHistorySyncService;
    private readonly IMapOfflineTileService _mapOfflineTileService;
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
    private string _selectedPlaybackMode = DefaultNarrationMode;
    private string _draftSelectedLanguage = "vi";
    private string _draftSelectedPlaybackMode = DefaultNarrationMode;
    private bool _draftIsAutoNarrationEnabled = true;
    private string _searchQuery = string.Empty;
    private string _currentUserDisplayName = "Khách tham quan";
    private string _currentUserStatusLine = "Chế độ tham quan nhanh";
    private string _currentUserInitials = "VK";
    private string _currentUserAccountLabel = "Khách tham quan";
    private string _currentUserPasswordLabel = "Vào nhanh bằng nút truy cập";
    private string _accountProfileFullName = string.Empty;
    private string _accountProfileEmail = string.Empty;
    private string _accountProfilePhoneNumber = string.Empty;
    private bool _isSavingAccountProfile;
    private string _accountSettingsErrorMessage = string.Empty;
    private string _accountSettingsSuccessMessage = string.Empty;
    private bool _isSavingAudioSettings;
    private bool _isPreviewingAudioSettings;
    private string _audioSettingsErrorMessage = string.Empty;
    private string _audioSettingsSuccessMessage = string.Empty;
    private bool _isSyncingOfflinePackage;
    private string _offlinePackageErrorMessage = string.Empty;
    private string _offlinePackageSuccessMessage = string.Empty;
    private string _searchResultStatusText = string.Empty;
    private OfflineContentStatus _offlineContentStatus = new();
    private OfflineMapStatus _offlineMapStatus = new();
    private AudioCacheStatus _audioCacheStatus = new();

    private bool _isListeningHistoryLoading;
    private string _listeningHistoryLoadError = string.Empty;
    private DateTimeOffset? _lastListeningHistorySyncAt;
    private string _selectedListeningHistoryPeriod = "Tất cả";
    private string _selectedListeningHistorySort = "Mới nhất trước";
    private string _selectedListeningHistoryView = "Dòng thời gian";

    public MainViewModel(
        ILocationService locationService,
        IPoiRepository poiRepository,
        IPoiOfflineStore poiOfflineStore,
        ISearchService searchService,
        INarrationService narrationService,
        IAuthService authService,
        IAudioSettingsService audioSettingsService,
        IAudioAssetCacheService audioAssetCacheService,
        IAccountProfileValidationService accountProfileValidationService,
        IUsageHistoryService usageHistoryService,
        IListeningHistorySyncService listeningHistorySyncService,
        IMapOfflineTileService mapOfflineTileService,
        GeofenceEngine geofenceEngine)
    {
        _locationService = locationService;
        _poiRepository = poiRepository;
        _poiOfflineStore = poiOfflineStore;
        _searchService = searchService;
        _narrationService = narrationService;
        _authService = authService;
        _audioSettingsService = audioSettingsService;
        _audioAssetCacheService = audioAssetCacheService;
        _accountProfileValidationService = accountProfileValidationService;
        _usageHistoryService = usageHistoryService;
        _listeningHistorySyncService = listeningHistorySyncService;
        _mapOfflineTileService = mapOfflineTileService;
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
        SaveAccountProfileCommand = new Command(async () => await SaveAccountProfileAsync(), () => CanSaveAccountProfile);
        ResetAccountProfileCommand = new Command(ResetAccountProfileEditor, () => CanManageAccountProfile && !IsSavingAccountProfile);
        PreviewAudioSettingsCommand = new Command(async () => await PreviewAudioSettingsAsync(), () => CanPreviewAudioSettings);
        SaveAudioSettingsCommand = new Command(async () => await SaveAudioSettingsAsync(), () => CanSaveAudioSettings);
        ResetAudioSettingsCommand = new Command(ResetAudioSettingsDraft, () => CanResetAudioSettings);
        DownloadOfflinePackageCommand = new Command(async () => await DownloadOfflinePackageAsync(), () => CanDownloadOfflinePackage);
        ClearOfflinePackageCommand = new Command(async () => await ClearOfflinePackageAsync(), () => CanClearOfflinePackage);

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
    public ObservableCollection<string> ListeningHistoryLocalEntries { get; } = new();
    public ObservableCollection<ListeningHistoryRankingDisplayItem> ListeningHistoryRanking { get; } = new();
    public ObservableCollection<string> ViewHistory { get; } = new();
    public IReadOnlyList<AudioSettingsOption> SupportedLanguages { get; } =
    [
        new() { Code = "vi", Label = "Tiếng Việt", Description = "Giọng tiếng Việt chuẩn cho khách nội địa." },
        new() { Code = "en", Label = "English", Description = "Giọng đọc tiếng Anh với accent English." },
        new() { Code = "zh", Label = "中文", Description = "Bản đọc tiếng Trung." },
        new() { Code = "ko", Label = "한국어", Description = "Bản đọc tiếng Hàn." },
        new() { Code = "fr", Label = "Français", Description = "Bản đọc tiếng Pháp." }
    ];
    public IReadOnlyList<AudioSettingsOption> SupportedPlaybackModes { get; } =
    [
        new() { Code = "tts", Label = "TTS", Description = "Đọc bằng giọng máy theo ngôn ngữ đã chọn." },
        new() { Code = "audio", Label = "Audio", Description = "Ưu tiên phát file audio thu sẵn nếu quán đã có asset." }
    ];
    public IReadOnlyList<string> ListeningHistoryPeriodOptions { get; } = ["Tất cả", "24 giờ qua", "7 ngày qua", "30 ngày qua"];
    public IReadOnlyList<string> ListeningHistorySortOptions { get; } = ["Mới nhất trước", "Cũ nhất trước"];
    public IReadOnlyList<string> ListeningHistoryViewOptions { get; } = ["Dòng thời gian", "Xếp hạng POI"];

    public ICommand StartTrackingCommand { get; }
    public ICommand StopTrackingCommand { get; }
    public ICommand StopNarrationCommand { get; }
    public ICommand SignOutCommand { get; }
    public ICommand ClearEventLogsCommand { get; }
    public ICommand ClearListeningHistoryCommand { get; }
    public ICommand ClearViewHistoryCommand { get; }
    public ICommand RefreshListeningHistoryCommand { get; }
    public ICommand SaveAccountProfileCommand { get; }
    public ICommand ResetAccountProfileCommand { get; }
    public ICommand PreviewAudioSettingsCommand { get; }
    public ICommand SaveAudioSettingsCommand { get; }
    public ICommand ResetAudioSettingsCommand { get; }
    public ICommand DownloadOfflinePackageCommand { get; }
    public ICommand ClearOfflinePackageCommand { get; }

    public IReadOnlyList<POI> Pois => _pois;
    public LocationDto? LastLocation => _lastLocation;
    public Guid? SelectedPoiId => _selectedPoi?.Id;
    public bool IsSelectedPoiNarrating => _selectedPoi is not null && IsNarrating && _activeNarrationPoiId == _selectedPoi.Id;
    public string SelectedPoiNarrationActionText => IsSelectedPoiNarrating ? "Dừng thuyết minh" : "Nghe thuyết minh";
    public string HomeNarrationSummary
    {
        get
        {
            var activePoi = _activeNarrationPoiId.HasValue
                ? _pois.FirstOrDefault(item => item.Id == _activeNarrationPoiId.Value)
                : null;

            if (IsNarrating && activePoi is not null)
            {
                return $"Đang phát Talk to Speech cho {activePoi.Name}. Bạn có thể bấm lại trên box quán để dừng.";
            }

            if (_pois.Count == 0)
            {
                return "Khi có dữ liệu quán, mỗi quán sẽ hiện thành một box thông tin riêng để người dùng nghe giới thiệu ngay tại trang chủ.";
            }

            return $"{CurrentUserDisplayName} có thể chủ động bấm \"Nghe thuyết minh\" trên từng box quán để tìm hiểu nội dung trước khi di chuyển tới nơi.";
        }
    }

    public string HomeNarrationAvailabilityText => _pois.Count == 0
        ? "Đang chờ danh sách quán từ hệ thống quản trị."
        : $"{_pois.Count} quán sẵn sàng phát Talk to Speech • Ngôn ngữ hiện tại: {SelectedLanguageDisplayName}";

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
            OnPropertyChanged(nameof(HomeNarrationSummary));
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
        private set
        {
            var normalizedLanguage = NormalizeLanguageCode(value);
            if (!SetProperty(ref _selectedLanguage, normalizedLanguage))
            {
                return;
            }

            UpdateSelectedPoiDetails();
            RefreshNarrationPresentation();
            OnPropertyChanged(nameof(SelectedLanguageDisplayName));
            OnPropertyChanged(nameof(AudioSettingsSummary));
            OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
            OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
            OnPropertyChanged(nameof(AudioSettingsQuickActionText));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            UpdateAudioSettingsCommandStates();
        }
    }

    public string SelectedPlaybackMode
    {
        get => _selectedPlaybackMode;
        private set
        {
            var normalizedPlaybackMode = NormalizePlaybackMode(value);
            if (!SetProperty(ref _selectedPlaybackMode, normalizedPlaybackMode))
            {
                return;
            }

            OnPropertyChanged(nameof(NarrationModeDisplay));
            OnPropertyChanged(nameof(NarrationModeDescription));
            OnPropertyChanged(nameof(AudioSettingsSummary));
            OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
            OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
            OnPropertyChanged(nameof(AudioSettingsQuickActionText));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            UpdateAudioSettingsCommandStates();
        }
    }

    public bool IsAutoNarrationEnabled
    {
        get => _isAutoNarrationEnabled;
        private set
        {
            if (!SetProperty(ref _isAutoNarrationEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(AudioSettingsSummary));
            OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
            OnPropertyChanged(nameof(AudioSettingsQuickActionText));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            UpdateAudioSettingsCommandStates();
        }
    }

    public string DraftSelectedLanguage
    {
        get => _draftSelectedLanguage;
        set
        {
            var normalizedLanguage = NormalizeLanguageCode(value);
            if (!SetProperty(ref _draftSelectedLanguage, normalizedLanguage))
            {
                return;
            }

            OnPropertyChanged(nameof(DraftSelectedLanguageDisplayName));
            OnPropertyChanged(nameof(DraftSelectedLanguageOption));
            OnPropertyChanged(nameof(DraftAudioSettingsSummary));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            ClearAudioSettingsFeedback();
            UpdateAudioSettingsCommandStates();
        }
    }

    public string DraftSelectedPlaybackMode
    {
        get => _draftSelectedPlaybackMode;
        set
        {
            var normalizedPlaybackMode = NormalizePlaybackMode(value);
            if (!SetProperty(ref _draftSelectedPlaybackMode, normalizedPlaybackMode))
            {
                return;
            }

            OnPropertyChanged(nameof(DraftPlaybackModeDisplay));
            OnPropertyChanged(nameof(DraftSelectedPlaybackModeOption));
            OnPropertyChanged(nameof(DraftPlaybackModeDescription));
            OnPropertyChanged(nameof(DraftAudioSettingsSummary));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            ClearAudioSettingsFeedback();
            UpdateAudioSettingsCommandStates();
        }
    }

    public bool DraftIsAutoNarrationEnabled
    {
        get => _draftIsAutoNarrationEnabled;
        set
        {
            if (!SetProperty(ref _draftIsAutoNarrationEnabled, value))
            {
                return;
            }

            OnPropertyChanged(nameof(DraftAudioSettingsSummary));
            OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));
            ClearAudioSettingsFeedback();
            UpdateAudioSettingsCommandStates();
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

    public string SearchResultStatusText
    {
        get => _searchResultStatusText;
        private set => SetProperty(ref _searchResultStatusText, value);
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

    public string AccountProfileFullName
    {
        get => _accountProfileFullName;
        set
        {
            if (SetProperty(ref _accountProfileFullName, value ?? string.Empty))
            {
                ClearAccountSettingsFeedback();
            }
        }
    }

    public string AccountProfileEmail
    {
        get => _accountProfileEmail;
        set
        {
            if (SetProperty(ref _accountProfileEmail, value ?? string.Empty))
            {
                ClearAccountSettingsFeedback();
            }
        }
    }

    public string AccountProfilePhoneNumber
    {
        get => _accountProfilePhoneNumber;
        set
        {
            if (SetProperty(ref _accountProfilePhoneNumber, value ?? string.Empty))
            {
                ClearAccountSettingsFeedback();
            }
        }
    }

    public bool IsSavingAccountProfile
    {
        get => _isSavingAccountProfile;
        private set
        {
            if (!SetProperty(ref _isSavingAccountProfile, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanSaveAccountProfile));
            (SaveAccountProfileCommand as Command)?.ChangeCanExecute();
            (ResetAccountProfileCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool CanManageAccountProfile => !IsGuestAccess();

    public bool CanSaveAccountProfile => CanManageAccountProfile && !IsSavingAccountProfile;

    public string AccountSettingsErrorMessage
    {
        get => _accountSettingsErrorMessage;
        private set
        {
            if (!SetProperty(ref _accountSettingsErrorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAccountSettingsError));
        }
    }

    public bool HasAccountSettingsError => !string.IsNullOrWhiteSpace(AccountSettingsErrorMessage);

    public string AccountSettingsSuccessMessage
    {
        get => _accountSettingsSuccessMessage;
        private set
        {
            if (!SetProperty(ref _accountSettingsSuccessMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAccountSettingsSuccess));
        }
    }

    public bool HasAccountSettingsSuccess => !string.IsNullOrWhiteSpace(AccountSettingsSuccessMessage);

    public string AccountSettingsAccessMessage => CanManageAccountProfile
        ? "Bạn có thể cập nhật hồ sơ cá nhân ngay trên thiết bị này. Hệ thống sẽ kiểm tra dữ liệu trước khi lưu."
        : "Bản hiện tại đang vào app ở chế độ khách tham quan. Luồng QR và tài khoản sẽ được nối lại ở bước sau.";

    public bool IsSavingAudioSettings
    {
        get => _isSavingAudioSettings;
        private set
        {
            if (!SetProperty(ref _isSavingAudioSettings, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanSaveAudioSettings));
            OnPropertyChanged(nameof(CanPreviewAudioSettings));
            OnPropertyChanged(nameof(CanResetAudioSettings));
            UpdateAudioSettingsCommandStates();
        }
    }

    public bool IsPreviewingAudioSettings
    {
        get => _isPreviewingAudioSettings;
        private set
        {
            if (!SetProperty(ref _isPreviewingAudioSettings, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanSaveAudioSettings));
            OnPropertyChanged(nameof(CanPreviewAudioSettings));
            OnPropertyChanged(nameof(CanResetAudioSettings));
            UpdateAudioSettingsCommandStates();
        }
    }

    public string AudioSettingsErrorMessage
    {
        get => _audioSettingsErrorMessage;
        private set
        {
            if (!SetProperty(ref _audioSettingsErrorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAudioSettingsError));
        }
    }

    public bool HasAudioSettingsError => !string.IsNullOrWhiteSpace(AudioSettingsErrorMessage);

    public string AudioSettingsSuccessMessage
    {
        get => _audioSettingsSuccessMessage;
        private set
        {
            if (!SetProperty(ref _audioSettingsSuccessMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasAudioSettingsSuccess));
        }
    }

    public bool HasAudioSettingsSuccess => !string.IsNullOrWhiteSpace(AudioSettingsSuccessMessage);

    public bool IsSyncingOfflinePackage
    {
        get => _isSyncingOfflinePackage;
        private set
        {
            if (!SetProperty(ref _isSyncingOfflinePackage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanDownloadOfflinePackage));
            OnPropertyChanged(nameof(CanClearOfflinePackage));
            UpdateOfflinePackageCommandStates();
        }
    }

    public string OfflinePackageErrorMessage
    {
        get => _offlinePackageErrorMessage;
        private set
        {
            if (!SetProperty(ref _offlinePackageErrorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasOfflinePackageError));
        }
    }

    public bool HasOfflinePackageError => !string.IsNullOrWhiteSpace(OfflinePackageErrorMessage);

    public string OfflinePackageSuccessMessage
    {
        get => _offlinePackageSuccessMessage;
        private set
        {
            if (!SetProperty(ref _offlinePackageSuccessMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasOfflinePackageSuccess));
        }
    }

    public bool HasOfflinePackageSuccess => !string.IsNullOrWhiteSpace(OfflinePackageSuccessMessage);

    public bool CanDownloadOfflinePackage => !IsSyncingOfflinePackage;

    public bool CanClearOfflinePackage =>
        !IsSyncingOfflinePackage &&
        (_offlineContentStatus.HasSnapshot || _offlineMapStatus.HasCachedTiles || _audioCacheStatus.HasCachedAssets);

    public string OfflineContentSummary =>
        _offlineContentStatus.HasSnapshot
            ? $"{_offlineContentStatus.PoiCount} POI trong SQLite • {FormatTimestamp(_offlineContentStatus.LastSyncedAtUtc, "chưa có mốc đồng bộ")}"
            : "Chưa có snapshot POI SQLite trên thiết bị.";

    public string OfflineMapSummary =>
        $"{_offlineMapStatus.CachedTileCount}/{_offlineMapStatus.PlannedTileCount} tile bản đồ khu Vĩnh Khánh • {FormatFileSize(_offlineMapStatus.CachedBytes)}";

    public string OfflineMapDetail =>
        _offlineMapStatus.IsReady
            ? "Các tile chính của khu vực luận văn đã sẵn sàng để dùng khi mất mạng."
            : "App sẽ ưu tiên tile local trước. Các ô chưa tải sẽ không hiển thị khi mất mạng.";

    public string OfflineAudioSummary =>
        !_audioCacheStatus.HasPublishedAssets
            ? "Hiện dữ liệu quán chưa công bố file audio thu sẵn."
            : $"{_audioCacheStatus.CachedAssetCount}/{_audioCacheStatus.AvailableAssetCount} audio asset • {FormatFileSize(_audioCacheStatus.CachedBytes)}";

    public string OfflineAudioDetail =>
        _audioCacheStatus.HasPublishedAssets
            ? $"Audio offline gần nhất: {FormatTimestamp(_audioCacheStatus.LastPreparedAtUtc, "chưa có mốc tải gói")}."
            : "Khi backend bổ sung `AudioAssetPath`, nút tải gói sẽ tải trước các file này xuống máy.";

    public bool HasPendingAudioSettingsChanges =>
        !string.Equals(DraftSelectedLanguage, SelectedLanguage, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(DraftSelectedPlaybackMode, SelectedPlaybackMode, StringComparison.OrdinalIgnoreCase) ||
        DraftIsAutoNarrationEnabled != IsAutoNarrationEnabled;

    public bool CanSaveAudioSettings => HasPendingAudioSettingsChanges && !IsSavingAudioSettings && !IsPreviewingAudioSettings;

    public bool CanPreviewAudioSettings => !IsSavingAudioSettings && !IsPreviewingAudioSettings;

    public bool CanResetAudioSettings => HasPendingAudioSettingsChanges && !IsSavingAudioSettings && !IsPreviewingAudioSettings;

    public string NarrationModeDisplay => GetPlaybackModeLabel(SelectedPlaybackMode);

    public string NarrationModeDescription =>
        string.Equals(SelectedPlaybackMode, "audio", StringComparison.OrdinalIgnoreCase)
            ? "Ứng dụng sẽ ưu tiên phát file audio thu sẵn. Nếu quán chưa có file audio, hệ thống sẽ tự chuyển sang TTS để không bị gián đoạn."
            : "Ứng dụng đang dùng Talk to Speech làm chế độ phát mặc định cho phần thuyết minh.";

    public string DraftSelectedLanguageDisplayName => GetLanguageDisplayName(DraftSelectedLanguage);

    public AudioSettingsOption? DraftSelectedLanguageOption
    {
        get => SupportedLanguages.FirstOrDefault(item =>
            string.Equals(item.Code, DraftSelectedLanguage, StringComparison.OrdinalIgnoreCase));
        set => DraftSelectedLanguage = value?.Code ?? "vi";
    }

    public string DraftPlaybackModeDisplay => GetPlaybackModeLabel(DraftSelectedPlaybackMode);

    public AudioSettingsOption? DraftSelectedPlaybackModeOption
    {
        get => SupportedPlaybackModes.FirstOrDefault(item =>
            string.Equals(item.Code, DraftSelectedPlaybackMode, StringComparison.OrdinalIgnoreCase));
        set => DraftSelectedPlaybackMode = value?.Code ?? DefaultNarrationMode;
    }

    public string DraftPlaybackModeDescription =>
        string.Equals(DraftSelectedPlaybackMode, "audio", StringComparison.OrdinalIgnoreCase)
            ? "Nghe bằng file audio thu sẵn nếu quán đã có asset."
            : "Nghe bằng giọng đọc máy theo ngôn ngữ bạn chọn.";

    public string CurrentAudioSettingsSummary =>
        $"{NarrationModeDisplay} • {SelectedLanguageDisplayName} • {(IsAutoNarrationEnabled ? "Tự động phát" : "Chỉ phát thủ công")}";

    public string DraftAudioSettingsSummary =>
        $"{DraftPlaybackModeDisplay} • {DraftSelectedLanguageDisplayName} • {(DraftIsAutoNarrationEnabled ? "Tự động phát" : "Chỉ phát thủ công")}";

    public string AudioSettingsQuickActionText =>
        $"{NarrationModeDisplay} • {SelectedLanguageDisplayName}";

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

    public bool HasListeningHistoryLocalEntries => ListeningHistoryLocalEntries.Count > 0;

    public bool IsListeningHistoryFallbackVisible => !HasListeningHistory && HasListeningHistoryLocalEntries;

    public bool IsListeningHistoryDisplayEmpty => !HasListeningHistory && !HasListeningHistoryLocalEntries;

    public bool HasListeningHistoryRanking => ListeningHistoryRanking.Count > 0;

    public bool HasViewHistory => ViewHistory.Count > 0;

    public bool IsViewHistoryEmpty => ViewHistory.Count == 0;

    public string EventLogSummary => HasEventLogs
        ? $"{EventLogs.Count} hoạt động gần nhất của tài khoản hiện tại"
        : "Chưa có hoạt động nào được ghi lại";

    public string ListeningHistorySummary => HasListeningHistory
        ? $"{ListeningHistory.Count} bản ghi nghe gần nhất"
        : HasListeningHistoryLocalEntries
            ? $"{ListeningHistoryLocalEntries.Count} lượt nghe vừa ghi nhận"
            : "Chưa có bản ghi nghe nào";

    public string ListeningHistoryFallbackSummary => HasListeningHistoryLocalEntries
        ? $"{ListeningHistoryLocalEntries.Count} lượt nghe đã được lưu cục bộ trên thiết bị này."
        : "Các lượt nghe mới sẽ được lưu cục bộ trên thiết bị này.";

    public string ViewHistorySummary => HasViewHistory
        ? $"{ViewHistory.Count} lượt xem gần nhất"
        : "Chưa có lượt xem nào";

    public string SelectedLanguageDisplayName => GetLanguageDisplayName(SelectedLanguage);

    public string AudioSettingsSummary =>
        $"{(IsAutoNarrationEnabled ? "Tự động phát khi vào vùng" : "Chỉ phát thủ công")} • {SelectedLanguageDisplayName} • {NarrationModeDisplay}";

    public bool IsListeningHistoryLoading
    {
        get => _isListeningHistoryLoading;
        private set => SetProperty(ref _isListeningHistoryLoading, value);
    }

    public string ListeningHistoryLoadError
    {
        get => _listeningHistoryLoadError;
        private set
        {
            if (!SetProperty(ref _listeningHistoryLoadError, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasListeningHistoryLoadError));
        }
    }

    public bool HasListeningHistoryLoadError => !string.IsNullOrWhiteSpace(ListeningHistoryLoadError);

    public string ListeningHistorySyncStatus => _lastListeningHistorySyncAt.HasValue
        ? $"Đồng bộ lúc {_lastListeningHistorySyncAt.Value.ToLocalTime():HH:mm:ss}"
        : "Chưa đồng bộ lịch sử nghe";

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
        "Dòng thời gian",
        StringComparison.Ordinal);

    public bool IsListeningHistoryRankingVisible => !IsListeningHistoryTimelineVisible;

    public async Task InitializeAsync(bool enableLocationFlow = true)
    {
        EnsurePoiRefreshLoopStarted();

        if (_isInitialized)
        {
            await RefreshPoisIfChangedAsync();
            await RefreshOfflinePackageStatusAsync();
            return;
        }

        StatusText = "Đang tải danh sách quán...";
        await RefreshPoisIfChangedAsync(forceRefresh: true);

        if (enableLocationFlow)
        {
            await InitializeMapFlowAsync();
        }
        else
        {
            await ApplyFallbackMapStateAsync(
                "Che do QR: san sang phat thuyet minh",
                "Dang mo noi dung tai diem dung xe buyt Khanh Hoi, Vinh Hoi, Xom Chieu. Khong can GPS.");
        }

        await RefreshOfflinePackageStatusAsync();
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
        UpdateFilteredPoiStatuses();
        return FilteredPoiStatuses.Count > 0;
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

    public async Task<bool> OpenPoiFromQrAsync(
        Guid poiId,
        bool autoPlay = true,
        CancellationToken cancellationToken = default)
    {
        if (poiId == Guid.Empty)
        {
            StatusText = "Ma QR khong hop le";
            return false;
        }

        if (!_isInitialized)
        {
            await InitializeAsync(enableLocationFlow: false);
        }
        else if (_pois.All(item => item.Id != poiId))
        {
            await RefreshPoisIfChangedAsync(forceRefresh: true, cancellationToken);
        }

        var poi = _pois.FirstOrDefault(item => item.Id == poiId)
                  ?? await _poiRepository.GetPoiByIdAsync(poiId, cancellationToken);

        if (poi is null)
        {
            StatusText = "Khong tim thay POI tu ma QR nay";
            return false;
        }

        if (!poi.IsActive)
        {
            StatusText = "POI nay hien dang tam khoa";
            return false;
        }

        SetSelectedPoi(poi, true, null);
        StatusText = autoPlay
            ? $"Dang mo QR cua {poi.Name}"
            : $"Da mo QR cua {poi.Name}";
        AddLog($"{NowLabel()} QR mo {poi.Name} (khong can GPS)");

        if (!IsTracking)
        {
            LocationText = "Che do QR tai diem dung xe buyt: Khong can GPS.";
        }

        if (!autoPlay)
        {
            RefreshNarrationPresentation();
            return true;
        }

        if (IsCurrentNarration(poi))
        {
            await StopNarrationAsync();
        }

        await NarratePoiAsync(poi, false, GetDistanceForPoi(poi.Id), syncSelectedPoi: false);
        return true;
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

        if (IsCurrentNarration(poi))
        {
            await StopNarrationAsync();
            return;
        }

        await NarratePoiAsync(
            poi,
            false,
            GetDistanceForPoi(poi.Id),
            syncSelectedPoi: false);
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

    public async Task<bool> OpenListeningHistoryDetailAsync(Guid historyId)
    {
        var item = FindListeningHistoryItem(historyId);
        if (item is null)
        {
            StatusText = "Không tìm thấy bản ghi lịch sử nghe";
            return false;
        }

        var poi = await ResolvePoiForListeningHistoryAsync(item);
        if (poi is null)
        {
            StatusText = "Không thể mở chi tiết quán từ bản ghi này";
            return false;
        }

        SetSelectedPoi(poi, true, null);
        StatusText = $"Đang xem lại lịch sử nghe của {item.PoiName}";
        return true;
    }

    public async Task ReplayListeningHistoryAsync(Guid historyId)
    {
        var item = FindListeningHistoryItem(historyId);
        if (item is null)
        {
            StatusText = "Không tìm thấy bản ghi lịch sử nghe";
            return;
        }

        if (string.IsNullOrWhiteSpace(item.NarrationSnapshot) &&
            string.IsNullOrWhiteSpace(item.AudioAssetPath))
        {
            StatusText = "Bản ghi này chưa có nội dung để phát lại";
            return;
        }

        var playbackRequest = ResolvePlaybackRequest(
            item.PlaybackMode,
            item.AudioAssetPath,
            allowAudioFallback: true);
        var narrationSessionId = Interlocked.Increment(ref _narrationSessionId);

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            SetActiveNarrationPoiId(item.PoiId == Guid.Empty ? null : item.PoiId);
            IsNarrating = true;
            StatusText = $"Phát lại lịch sử: {item.PoiName}";
            RefreshNarrationPresentation();
        });

        AddLog($"{NowLabel()} Phát lại lịch sử nghe {item.PoiName} [{item.PlaybackModeLabel}]");
        if (!string.IsNullOrWhiteSpace(playbackRequest.FallbackMessage))
        {
            AddLog($"{NowLabel()} {playbackRequest.FallbackMessage}");
        }

        try
        {
            await _narrationService.SpeakAsync(
                item.NarrationSnapshot,
                item.Language,
                playbackRequest.PlaybackMode,
                playbackRequest.AudioAssetPath);
        }
        catch (Exception ex)
        {
            AddLog($"{NowLabel()} Lỗi phát lại lịch sử: {ex.Message}");

            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                StatusText = $"Lỗi phát lại lịch sử: {ex.Message}";
            }
        }
        finally
        {
            if (narrationSessionId == Volatile.Read(ref _narrationSessionId))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetActiveNarrationPoiId(null);
                    IsNarrating = false;
                    RefreshNarrationPresentation();

                    if (!StatusText.StartsWith("Lỗi phát lại lịch sử:", StringComparison.Ordinal))
                    {
                        StatusText = IsTracking ? "GPS đang hoạt động" : "Sẵn sàng phát thuyết minh";
                    }
                });
            }
        }
    }

    public async Task<bool> DeleteListeningHistoryEntryAsync(Guid historyId)
    {
        var deleted = await _listeningHistorySyncService.DeleteAsync(historyId);
        if (!deleted)
        {
            ListeningHistoryLoadError = "Không xóa được bản ghi lịch sử nghe này.";
            return false;
        }

        await RefreshListeningHistoryAsync();
        StatusText = "Đã cập nhật danh sách lịch sử nghe";
        return true;
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
        var latestPois = await _poiRepository.GetPoisAsync(cancellationToken);
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

    private async Task NarratePoiAsync(
        POI poi,
        bool autoTriggered,
        double? distanceMeters,
        bool syncSelectedPoi = true)
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
        var playbackRequest = ResolvePlaybackRequest(
            SelectedPlaybackMode,
            poi.AudioAssetPath,
            allowAudioFallback: true);
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

            if (syncSelectedPoi && !_hasUserSelectedPoi)
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

        if (!string.IsNullOrWhiteSpace(playbackRequest.FallbackMessage))
        {
            AddLog($"{NowLabel()} {playbackRequest.FallbackMessage}");
        }

        historyTask = _listeningHistorySyncService.BeginAsync(
            poi,
            SelectedLanguage,
            playbackRequest.PlaybackMode,
            autoTriggered);
        _ = RefreshListeningHistoryAfterCreateAsync(historyTask);

        try
        {
            await _narrationService.NarrateAsync(
                poi,
                SelectedLanguage,
                playbackRequest.PlaybackMode);
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
                NarrationPreview = BuildNarrationPreview(poi.GetNarrationText(SelectedLanguage)),
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
        OnPropertyChanged(nameof(HomeNarrationSummary));
        OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
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
        OnPropertyChanged(nameof(HomeNarrationSummary));
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
        var searchResult = _searchService.Search(SearchQuery);
        var visibleItems = searchResult.HasKeyword
            ? searchResult.Results
                .Select(result => PoiStatuses.FirstOrDefault(item => item.PoiId == result.Id))
                .Where(item => item is not null)
                .Cast<PoiStatusItem>()
                .ToList()
            : PoiStatuses.ToList();

        FilteredPoiStatuses.Clear();

        foreach (var item in visibleItems)
        {
            FilteredPoiStatuses.Add(item);
        }

        IsSearchResultEmpty = searchResult.HasKeyword && FilteredPoiStatuses.Count == 0;
        SearchResultStatusText = IsSearchResultEmpty
            ? searchResult.EmptyStateMessage
            : string.Empty;
    }

    private void RefreshSearchSuggestions()
    {
        SearchSuggestions.Clear();

        if (!_isSearchFocused || PoiStatuses.Count == 0)
        {
            IsSearchSuggestionsVisible = false;
            return;
        }

        var suggestions = _searchService.BuildSuggestions(
            SearchQuery,
            _recentSearches,
            MaxSearchSuggestions,
            MaxPinnedRecentSuggestions);

        foreach (var suggestion in suggestions)
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

            var timelineItems = await BuildListeningHistoryDisplayItemsAsync(
                timelineTask.Result,
                cancellationToken);
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
            ListeningHistoryLoadError = $"Không tải được lịch sử nghe: {ex.Message}";
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

    private async Task<List<ListeningHistoryDisplayItem>> BuildListeningHistoryDisplayItemsAsync(
        IReadOnlyList<ListeningHistoryEntryDto> entries,
        CancellationToken cancellationToken)
    {
        if (entries.Count == 0)
        {
            return [];
        }

        var poiLookup = _pois.ToDictionary(item => item.Id);
        var missingPoiIds = entries
            .Select(item => item.PoiId)
            .Where(poiId => poiId != Guid.Empty && !poiLookup.ContainsKey(poiId))
            .Distinct()
            .ToList();

        if (missingPoiIds.Count > 0)
        {
            var lookupTasks = missingPoiIds
                .Select(async poiId => new
                {
                    PoiId = poiId,
                    Poi = await _poiRepository.GetPoiByIdAsync(poiId, cancellationToken)
                })
                .ToList();

            var results = await Task.WhenAll(lookupTasks);
            foreach (var result in results)
            {
                if (result.Poi is not null)
                {
                    poiLookup[result.PoiId] = result.Poi;
                }
            }
        }

        return entries
            .Select(item => ToListeningHistoryDisplayItem(
                item,
                poiLookup.TryGetValue(item.PoiId, out var poi) ? poi : null))
            .ToList();
    }

    private static ListeningHistoryDisplayItem ToListeningHistoryDisplayItem(
        ListeningHistoryEntryDto item,
        POI? poi)
    {
        var startedAtLocal = item.StartedAtUtc.ToLocalTime();
        var triggerLabel = item.AutoTriggered || string.Equals(item.TriggerType, "GPS", StringComparison.OrdinalIgnoreCase)
            ? "Tự động"
            : "Thủ công";
        var durationLabel = item.ListenSeconds > 0
            ? $"{item.ListenSeconds} giây"
            : "Đang ghi nhận";
        var languageLabel = GetLanguageDisplayName(item.Language);
        var playbackModeLabel = GetPlaybackModeLabel(item.PlaybackMode);

        var statusLabel = item.Completed
            ? "Hoàn tất"
            : string.IsNullOrWhiteSpace(item.ErrorMessage)
                ? "Đang nghe / dừng sớm"
                : "Dừng vì lỗi";

        var statusAccentColor = item.Completed
            ? "#15803D"
            : string.IsNullOrWhiteSpace(item.ErrorMessage)
                ? "#C2410C"
                : "#B91C1C";

        var summaryParts = new List<string>
        {
            languageLabel,
            playbackModeLabel,
            durationLabel
        };

        var detailParts = new List<string>
        {
            triggerLabel
        };

        if (!string.IsNullOrWhiteSpace(item.DevicePlatform))
        {
            detailParts.Add(item.DevicePlatform);
        }

        var address = string.IsNullOrWhiteSpace(item.PoiAddress)
            ? poi?.Address ?? string.Empty
            : item.PoiAddress;
        var description = string.IsNullOrWhiteSpace(item.PoiDescription)
            ? poi?.Description ?? string.Empty
            : item.PoiDescription;
        var specialDish = string.IsNullOrWhiteSpace(item.PoiSpecialDish)
            ? poi?.SpecialDish ?? string.Empty
            : item.PoiSpecialDish;
        var imageSource = string.IsNullOrWhiteSpace(item.PoiImageSource)
            ? poi?.ImageSource ?? string.Empty
            : item.PoiImageSource;
        var mapLink = string.IsNullOrWhiteSpace(item.PoiMapLink)
            ? poi?.MapLink ?? string.Empty
            : item.PoiMapLink;
        var narrationSnapshot = string.IsNullOrWhiteSpace(item.NarrationSnapshot)
            ? poi?.GetNarrationText(item.Language) ?? string.Empty
            : item.NarrationSnapshot;

        return new ListeningHistoryDisplayItem
        {
            Id = item.Id,
            PoiId = item.PoiId,
            PoiCode = item.PoiCode,
            PoiName = item.PoiName,
            Address = address,
            Description = string.IsNullOrWhiteSpace(description)
                ? "Bản ghi này chưa có mô tả ngắn từ quán."
                : description,
            SpecialDish = specialDish,
            ImageSource = imageSource,
            MapLink = mapLink,
            Language = item.Language,
            LanguageLabel = languageLabel,
            PlaybackMode = item.PlaybackMode,
            PlaybackModeLabel = playbackModeLabel,
            NarrationSnapshot = narrationSnapshot,
            AudioAssetPath = item.AudioAssetPath,
            NarrationPreview = BuildNarrationPreview(narrationSnapshot),
            StartedAtLabel = startedAtLocal.ToString("dd/MM/yyyy HH:mm:ss"),
            StartedAtShortLabel = startedAtLocal.ToString("HH:mm"),
            DetailSummaryLabel = string.Join(" • ", summaryParts.Where(part => !string.IsNullOrWhiteSpace(part))),
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
            SummaryLabel = $"{item.ListenCount} lượt nghe • {item.TotalListenSeconds} giây • {completionRate}% hoàn tất",
            LastStartedAtLabel = item.LastStartedAtUtc.HasValue
                ? item.LastStartedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : "--"
        };
    }

    private static string MapListeningHistorySortToApi(string? selectedSort)
    {
        return string.Equals(selectedSort, "Cũ nhất trước", StringComparison.Ordinal)
            ? "time_asc"
            : "time_desc";
    }

    private static string MapListeningHistoryPeriodToApi(string? selectedPeriod)
    {
        return selectedPeriod switch
        {
            "24 giờ qua" => "day",
            "7 ngày qua" => "week",
            "30 ngày qua" => "month",
            _ => "all"
        };
    }

    private static string GetPlaybackModeLabel(string? playbackMode)
    {
        return playbackMode?.Trim().ToLowerInvariant() switch
        {
            "audio" => "Audio",
            _ => "TTS"
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
        var isGuestAccess = IsGuestAccess(session);

        CurrentUserDisplayName = isGuestAccess ? "Khách tham quan" : session!.FullName;
        CurrentUserInitials = isGuestAccess ? "VK" : session!.Initials;
        CurrentUserAccountLabel = isGuestAccess ? "Khách tham quan" : $"@{loginId}";
        CurrentUserPasswordLabel = isGuestAccess
            ? "Vào nhanh bằng nút truy cập"
            : string.Equals(loginId, "user", StringComparison.OrdinalIgnoreCase)
                ? "12345678 (mặc định)"
                : "•••••••• (đã ẩn)";
        CurrentUserStatusLine = isGuestAccess
            ? "Chế độ tham quan nhanh"
            : $"{session!.RoleLabel} • @{loginId}";
        LoadAccountProfileEditor(clearFeedback: true);
        OnPropertyChanged(nameof(CanManageAccountProfile));
        OnPropertyChanged(nameof(CanSaveAccountProfile));
        OnPropertyChanged(nameof(AccountSettingsAccessMessage));
        OnPropertyChanged(nameof(CurrentAudioSettingsSummary));
        OnPropertyChanged(nameof(AudioSettingsQuickActionText));
        OnPropertyChanged(nameof(NarrationModeDisplay));
        OnPropertyChanged(nameof(NarrationModeDescription));
        (SaveAccountProfileCommand as Command)?.ChangeCanExecute();
        (ResetAccountProfileCommand as Command)?.ChangeCanExecute();
        UpdateAudioSettingsCommandStates();
        OnPropertyChanged(nameof(HomeNarrationSummary));
    }

    public void ResetAccountProfileEditor()
    {
        LoadAccountProfileEditor(clearFeedback: true);
    }

    public void ResetAudioSettingsDraft()
    {
        _draftSelectedLanguage = SelectedLanguage;
        _draftSelectedPlaybackMode = SelectedPlaybackMode;
        _draftIsAutoNarrationEnabled = IsAutoNarrationEnabled;

        OnPropertyChanged(nameof(DraftSelectedLanguage));
        OnPropertyChanged(nameof(DraftSelectedLanguageDisplayName));
        OnPropertyChanged(nameof(DraftSelectedLanguageOption));
        OnPropertyChanged(nameof(DraftSelectedPlaybackMode));
        OnPropertyChanged(nameof(DraftPlaybackModeDisplay));
        OnPropertyChanged(nameof(DraftSelectedPlaybackModeOption));
        OnPropertyChanged(nameof(DraftPlaybackModeDescription));
        OnPropertyChanged(nameof(DraftIsAutoNarrationEnabled));
        OnPropertyChanged(nameof(DraftAudioSettingsSummary));
        OnPropertyChanged(nameof(HasPendingAudioSettingsChanges));

        ClearAudioSettingsFeedback();
        UpdateAudioSettingsCommandStates();
    }

    private async Task SaveAccountProfileAsync()
    {
        if (!CanManageAccountProfile)
        {
            AccountSettingsErrorMessage = "Chế độ khách tham quan chưa hỗ trợ cập nhật hồ sơ.";
            AccountSettingsSuccessMessage = string.Empty;
            return;
        }

        if (IsSavingAccountProfile)
        {
            return;
        }

        ClearAccountSettingsFeedback();

        var request = new AccountProfileUpdateRequest
        {
            FullName = AccountProfileFullName,
            Email = AccountProfileEmail,
            PhoneNumber = AccountProfilePhoneNumber
        };

        var validationResult = _accountProfileValidationService.Validate(request);
        if (!validationResult.IsValid)
        {
            AccountSettingsErrorMessage = validationResult.ErrorMessage;
            return;
        }

        IsSavingAccountProfile = true;

        try
        {
            var result = await _authService.UpdateCurrentUserProfileAsync(request);
            if (!result.Succeeded)
            {
                AccountSettingsErrorMessage = result.Message;
                return;
            }

            AccountSettingsSuccessMessage = result.Message;
            AddLog($"{NowLabel()} Cập nhật hồ sơ tài khoản");
        }
        finally
        {
            IsSavingAccountProfile = false;
        }
    }

    private async Task PreviewAudioSettingsAsync()
    {
        if (!CanPreviewAudioSettings)
        {
            return;
        }

        ClearAudioSettingsFeedback();
        IsPreviewingAudioSettings = true;

        try
        {
            await StopNarrationAsync();

            var previewPoi = GetAudioPreviewPoi();
            var playbackRequest = ResolvePlaybackRequest(
                DraftSelectedPlaybackMode,
                previewPoi?.AudioAssetPath,
                allowAudioFallback: false);

            if (!string.IsNullOrWhiteSpace(playbackRequest.FallbackMessage))
            {
                AudioSettingsErrorMessage = playbackRequest.FallbackMessage;
                return;
            }

            var previewText = BuildAudioSettingsPreviewText(DraftSelectedLanguage, previewPoi);
            await _narrationService.SpeakAsync(
                previewText,
                DraftSelectedLanguage,
                playbackRequest.PlaybackMode,
                playbackRequest.AudioAssetPath);

            AudioSettingsSuccessMessage =
                $"Đã nghe thử {DraftPlaybackModeDisplay} bằng {DraftSelectedLanguageDisplayName}.";
        }
        catch (Exception ex)
        {
            AudioSettingsErrorMessage = $"Không thể nghe thử cấu hình âm thanh: {ex.Message}";
        }
        finally
        {
            IsPreviewingAudioSettings = false;
        }
    }

    private Task SaveAudioSettingsAsync()
    {
        if (!CanSaveAudioSettings)
        {
            return Task.CompletedTask;
        }

        ClearAudioSettingsFeedback();
        IsSavingAudioSettings = true;

        try
        {
            var settings = new AudioSettingsState
            {
                LanguageCode = DraftSelectedLanguage,
                PlaybackMode = DraftSelectedPlaybackMode,
                AutoNarrationEnabled = DraftIsAutoNarrationEnabled
            };

            ApplyAudioSettings(settings, shouldLog: true);
            PersistUserPreferences();

            AudioSettingsSuccessMessage = "Đã lưu cài đặt âm thanh cho toàn bộ ứng dụng.";
        }
        finally
        {
            IsSavingAudioSettings = false;
        }

        return Task.CompletedTask;
    }

    private void LoadAccountProfileEditor(bool clearFeedback)
    {
        var session = _authService.CurrentSession;

        _accountProfileFullName = session?.FullName ?? string.Empty;
        _accountProfileEmail = session?.Email ?? string.Empty;
        _accountProfilePhoneNumber = session?.PhoneNumber ?? string.Empty;

        OnPropertyChanged(nameof(AccountProfileFullName));
        OnPropertyChanged(nameof(AccountProfileEmail));
        OnPropertyChanged(nameof(AccountProfilePhoneNumber));

        if (clearFeedback)
        {
            ClearAccountSettingsFeedback();
        }
    }

    private void ClearAccountSettingsFeedback()
    {
        AccountSettingsErrorMessage = string.Empty;
        AccountSettingsSuccessMessage = string.Empty;
    }

    private void ClearAudioSettingsFeedback()
    {
        AudioSettingsErrorMessage = string.Empty;
        AudioSettingsSuccessMessage = string.Empty;
    }

    private async Task DownloadOfflinePackageAsync()
    {
        if (!CanDownloadOfflinePackage)
        {
            return;
        }

        ClearOfflinePackageFeedback();
        IsSyncingOfflinePackage = true;

        try
        {
            var pois = await _poiRepository.GetPoisAsync();
            var mapResult = await _mapOfflineTileService.PrefetchAsync();
            var audioResult = await _audioAssetCacheService.PrefetchAsync(pois);

            await RefreshOfflinePackageStatusAsync();

            var successMessage =
                $"Đã cập nhật gói offline: SQLite {_offlineContentStatus.PoiCount} POI, map {mapResult.CachedTileCount}/{mapResult.PlannedTileCount} tile, audio {audioResult.CachedAssetCount}/{audioResult.AvailableAssetCount} asset.";

            var notes = new List<string>();
            if (mapResult.FailedTileCount > 0)
            {
                notes.Add($"Map còn {mapResult.FailedTileCount} tile chưa tải được");
            }

            if (audioResult.FailedAssetCount > 0)
            {
                notes.Add($"Audio còn {audioResult.FailedAssetCount} asset chưa tải được");
            }

            if (audioResult.AvailableAssetCount == 0)
            {
                notes.Add("Dữ liệu quán hiện chưa có audio thu sẵn để tải");
            }

            OfflinePackageSuccessMessage = notes.Count == 0
                ? successMessage
                : $"{successMessage} {string.Join(". ", notes)}.";

            AddLog($"{NowLabel()} Cập nhật gói offline trên thiết bị");
        }
        catch (Exception ex)
        {
            OfflinePackageErrorMessage = $"Không thể cập nhật gói offline: {ex.Message}";
        }
        finally
        {
            IsSyncingOfflinePackage = false;
        }
    }

    private async Task ClearOfflinePackageAsync()
    {
        if (!CanClearOfflinePackage)
        {
            return;
        }

        ClearOfflinePackageFeedback();
        IsSyncingOfflinePackage = true;

        try
        {
            await _mapOfflineTileService.ClearAsync();
            await _audioAssetCacheService.ClearAsync();
            await _poiOfflineStore.ClearAsync();
            await RefreshOfflinePackageStatusAsync();

            OfflinePackageSuccessMessage = "Đã xóa gói offline trên thiết bị.";
            AddLog($"{NowLabel()} Xóa gói offline trên thiết bị");
        }
        catch (Exception ex)
        {
            OfflinePackageErrorMessage = $"Không thể xóa gói offline: {ex.Message}";
        }
        finally
        {
            IsSyncingOfflinePackage = false;
        }
    }

    private async Task RefreshOfflinePackageStatusAsync(CancellationToken cancellationToken = default)
    {
        var offlinePois = await _poiOfflineStore.GetPoisAsync(cancellationToken);
        var lastSyncedAt = await _poiOfflineStore.GetLastSyncedAtAsync(cancellationToken);
        var mapStatus = await _mapOfflineTileService.GetStatusAsync(cancellationToken);
        var audioStatus = await _audioAssetCacheService.GetStatusAsync(
            _pois.Count > 0 ? _pois : offlinePois,
            cancellationToken);

        _offlineContentStatus = new OfflineContentStatus
        {
            PoiCount = offlinePois.Count,
            LastSyncedAtUtc = lastSyncedAt
        };
        _offlineMapStatus = mapStatus;
        _audioCacheStatus = audioStatus;

        RaiseOfflinePackageStateChanged();
    }

    private async Task RefreshOfflinePackageStatusSafeAsync()
    {
        try
        {
            await RefreshOfflinePackageStatusAsync();
        }
        catch
        {
        }
    }

    private void ClearOfflinePackageFeedback()
    {
        OfflinePackageErrorMessage = string.Empty;
        OfflinePackageSuccessMessage = string.Empty;
    }

    private void LoadPersistedState()
    {
        LoadUserPreferences();
        LoadPersistedHistory(EventLogs, UsageHistoryCategory.Activity);
        LoadPersistedHistory(ListeningHistoryLocalEntries, UsageHistoryCategory.Listening);
        LoadPersistedHistory(ViewHistory, UsageHistoryCategory.Viewing);
        RaiseEventLogStateChanged();
        RaiseListeningHistoryStateChanged();
        RaiseViewHistoryStateChanged();
        TriggerListeningHistoryRefresh();
        _ = RefreshOfflinePackageStatusSafeAsync();
    }

    private ListeningHistoryDisplayItem? FindListeningHistoryItem(Guid historyId)
    {
        return ListeningHistory.FirstOrDefault(item => item.Id == historyId);
    }

    private async Task<POI?> ResolvePoiForListeningHistoryAsync(
        ListeningHistoryDisplayItem item,
        CancellationToken cancellationToken = default)
    {
        POI? basePoi = null;

        if (item.PoiId != Guid.Empty)
        {
            basePoi = _pois.FirstOrDefault(poi => poi.Id == item.PoiId)
                ?? await _poiRepository.GetPoiByIdAsync(item.PoiId, cancellationToken);
        }

        return BuildPoiFromListeningHistoryItem(item, basePoi);
    }

    private static POI? BuildPoiFromListeningHistoryItem(
        ListeningHistoryDisplayItem item,
        POI? basePoi)
    {
        var poiId = item.PoiId != Guid.Empty
            ? item.PoiId
            : basePoi?.Id ?? Guid.Empty;

        if (poiId == Guid.Empty &&
            string.IsNullOrWhiteSpace(item.PoiName) &&
            string.IsNullOrWhiteSpace(basePoi?.Name))
        {
            return null;
        }

        var narrationText = string.IsNullOrWhiteSpace(item.NarrationSnapshot)
            ? basePoi?.GetNarrationText(item.Language) ?? string.Empty
            : item.NarrationSnapshot;

        return new POI
        {
            Id = poiId == Guid.Empty ? Guid.NewGuid() : poiId,
            Code = string.IsNullOrWhiteSpace(item.PoiCode) ? basePoi?.Code ?? string.Empty : item.PoiCode,
            Name = string.IsNullOrWhiteSpace(item.PoiName) ? basePoi?.Name ?? "POI lịch sử" : item.PoiName,
            Category = basePoi?.Category ?? "Ẩm thực",
            Address = string.IsNullOrWhiteSpace(item.Address) ? basePoi?.Address ?? string.Empty : item.Address,
            Description = string.IsNullOrWhiteSpace(item.Description)
                ? basePoi?.Description ?? string.Empty
                : item.Description,
            SpecialDish = string.IsNullOrWhiteSpace(item.SpecialDish)
                ? basePoi?.SpecialDish ?? string.Empty
                : item.SpecialDish,
            ImageSource = string.IsNullOrWhiteSpace(item.ImageSource)
                ? basePoi?.ImageSource ?? string.Empty
                : item.ImageSource,
            MapLink = string.IsNullOrWhiteSpace(item.MapLink)
                ? basePoi?.MapLink ?? string.Empty
                : item.MapLink,
            NarrationText = narrationText,
            AudioAssetPath = string.IsNullOrWhiteSpace(item.AudioAssetPath)
                ? basePoi?.AudioAssetPath ?? string.Empty
                : item.AudioAssetPath,
            Priority = basePoi?.Priority ?? 1,
            Latitude = basePoi?.Latitude ?? 0,
            Longitude = basePoi?.Longitude ?? 0,
            TriggerRadiusMeters = basePoi?.TriggerRadiusMeters ?? 50,
            CooldownMinutes = basePoi?.CooldownMinutes ?? 5,
            IsActive = basePoi?.IsActive ?? true,
            NarrationTranslations = !string.IsNullOrWhiteSpace(item.Language) &&
                !string.IsNullOrWhiteSpace(narrationText)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [item.Language] = narrationText
                }
                : new Dictionary<string, string>(
                    basePoi?.NarrationTranslations ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase)
        };
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
        ListeningHistoryLocalEntries.Clear();
        ListeningHistoryRanking.Clear();
        _usageHistoryService.ClearEntries(UsageHistoryCategory.Listening);
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
        OnPropertyChanged(nameof(HasListeningHistoryLocalEntries));
        OnPropertyChanged(nameof(IsListeningHistoryFallbackVisible));
        OnPropertyChanged(nameof(IsListeningHistoryDisplayEmpty));
        OnPropertyChanged(nameof(HasListeningHistoryRanking));
        OnPropertyChanged(nameof(ListeningHistoryFallbackSummary));
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

        AddHistoryEntry(
            ListeningHistoryLocalEntries,
            UsageHistoryCategory.Listening,
            message,
            RaiseListeningHistoryStateChanged);
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
            var settings = _audioSettingsService.Load(GetCurrentUserPreferencePrefix());
            ApplyAudioSettings(settings, shouldLog: false);
        }
        finally
        {
            _isRestoringUserPreferences = false;
        }

        ResetAudioSettingsDraft();
        RefreshNarrationPresentation();
        OnPropertyChanged(nameof(SelectedLanguageDisplayName));
        OnPropertyChanged(nameof(AudioSettingsSummary));
        OnPropertyChanged(nameof(HomeNarrationAvailabilityText));
    }

    private void PersistUserPreferences()
    {
        if (_isRestoringUserPreferences)
        {
            return;
        }

        _audioSettingsService.Save(
            GetCurrentUserPreferencePrefix(),
            new AudioSettingsState
            {
                LanguageCode = SelectedLanguage,
                PlaybackMode = SelectedPlaybackMode,
                AutoNarrationEnabled = IsAutoNarrationEnabled
            });
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

    private void ApplyAudioSettings(AudioSettingsState settings, bool shouldLog)
    {
        var normalizedLanguage = NormalizeLanguageCode(settings.LanguageCode);
        var normalizedPlaybackMode = NormalizePlaybackMode(settings.PlaybackMode);
        var languageChanged = !string.Equals(_selectedLanguage, normalizedLanguage, StringComparison.OrdinalIgnoreCase);
        var playbackModeChanged = !string.Equals(_selectedPlaybackMode, normalizedPlaybackMode, StringComparison.OrdinalIgnoreCase);
        var autoNarrationChanged = _isAutoNarrationEnabled != settings.AutoNarrationEnabled;

        SelectedLanguage = normalizedLanguage;
        SelectedPlaybackMode = normalizedPlaybackMode;
        IsAutoNarrationEnabled = settings.AutoNarrationEnabled;

        if (!shouldLog || _isRestoringUserPreferences)
        {
            return;
        }

        if (languageChanged)
        {
            AddLog($"{NowLabel()} Chuyển ngôn ngữ sang {SelectedLanguageDisplayName}");
        }

        if (playbackModeChanged)
        {
            AddLog($"{NowLabel()} Chuyển chế độ phát sang {NarrationModeDisplay}");
        }

        if (autoNarrationChanged)
        {
            AddLog($"{NowLabel()} {(IsAutoNarrationEnabled ? "Bật" : "Tắt")} tự động phát khi vào vùng");
        }
    }

    private void UpdateAudioSettingsCommandStates()
    {
        OnPropertyChanged(nameof(CanSaveAudioSettings));
        OnPropertyChanged(nameof(CanPreviewAudioSettings));
        OnPropertyChanged(nameof(CanResetAudioSettings));
        (PreviewAudioSettingsCommand as Command)?.ChangeCanExecute();
        (SaveAudioSettingsCommand as Command)?.ChangeCanExecute();
        (ResetAudioSettingsCommand as Command)?.ChangeCanExecute();
    }

    private void RaiseOfflinePackageStateChanged()
    {
        OnPropertyChanged(nameof(CanClearOfflinePackage));
        OnPropertyChanged(nameof(OfflineContentSummary));
        OnPropertyChanged(nameof(OfflineMapSummary));
        OnPropertyChanged(nameof(OfflineMapDetail));
        OnPropertyChanged(nameof(OfflineAudioSummary));
        OnPropertyChanged(nameof(OfflineAudioDetail));
        UpdateOfflinePackageCommandStates();
    }

    private void UpdateOfflinePackageCommandStates()
    {
        OnPropertyChanged(nameof(CanDownloadOfflinePackage));
        OnPropertyChanged(nameof(CanClearOfflinePackage));
        (DownloadOfflinePackageCommand as Command)?.ChangeCanExecute();
        (ClearOfflinePackageCommand as Command)?.ChangeCanExecute();
    }

    private POI? GetAudioPreviewPoi()
    {
        return _selectedPoi ?? _pois.FirstOrDefault();
    }

    private string BuildAudioSettingsPreviewText(string languageCode, POI? previewPoi)
    {
        if (previewPoi is not null)
        {
            return previewPoi.GetNarrationText(languageCode);
        }

        return languageCode.Trim().ToLowerInvariant() switch
        {
            "en" => "Hello. This is an English voice preview for Vinh Khanh Food Street.",
            "zh" => "您好，這是永慶美食街的中文語音試聽。",
            "ko" => "안녕하세요. 빈칸 음식 거리의 한국어 음성 미리 듣기입니다.",
            "fr" => "Bonjour. Ceci est un aperçu audio en français pour la rue gastronomique Vinh Khanh.",
            _ => "Xin chào. Đây là bản nghe thử âm thanh cho phố ẩm thực Vĩnh Khánh."
        };
    }

    private PlaybackRequest ResolvePlaybackRequest(
        string? preferredPlaybackMode,
        string? audioAssetPath,
        bool allowAudioFallback)
    {
        var normalizedPlaybackMode = NormalizePlaybackMode(preferredPlaybackMode);
        if (!string.Equals(normalizedPlaybackMode, "audio", StringComparison.OrdinalIgnoreCase))
        {
            return new PlaybackRequest("tts", null, null);
        }

        if (!string.IsNullOrWhiteSpace(audioAssetPath))
        {
            return new PlaybackRequest("audio", audioAssetPath.Trim(), null);
        }

        return allowAudioFallback
            ? new PlaybackRequest(
                "tts",
                null,
                "Nội dung này chưa có file audio, hệ thống tạm dùng TTS để tiếp tục phát.")
            : new PlaybackRequest(
                "audio",
                null,
                "Cấu hình Audio đang được chọn nhưng nội dung hiện tại chưa có file audio để nghe thử.");
    }

    private bool IsSupportedLanguage(string? languageCode)
    {
        return SupportedLanguages.Any(item =>
            string.Equals(item.Code, languageCode, StringComparison.OrdinalIgnoreCase));
    }

    private string NormalizeLanguageCode(string? languageCode)
    {
        return IsSupportedLanguage(languageCode)
            ? languageCode!.Trim().ToLowerInvariant()
            : "vi";
    }

    private string NormalizePlaybackMode(string? playbackMode)
    {
        return SupportedPlaybackModes.Any(item =>
            string.Equals(item.Code, playbackMode, StringComparison.OrdinalIgnoreCase))
            ? playbackMode!.Trim().ToLowerInvariant()
            : DefaultNarrationMode;
    }

    private bool IsGuestAccess(AuthSession? session = null)
    {
        var currentSession = session ?? _authService.CurrentSession;
        return currentSession is null ||
               string.Equals(currentSession.Role, "guest", StringComparison.OrdinalIgnoreCase);
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

    private static string BuildNarrationPreview(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "Chưa có nội dung thuyết minh cho quán này.";
        }

        var normalized = string.Join(
            ' ',
            text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return normalized.Length <= 150
            ? normalized
            : $"{normalized[..147].TrimEnd()}...";
    }

    private static string FormatTimestamp(DateTimeOffset? timestamp, string emptyText)
    {
        return timestamp.HasValue
            ? $"cập nhật {timestamp.Value.ToLocalTime():HH:mm dd/MM}"
            : emptyText;
    }

    private static string FormatFileSize(long byteCount)
    {
        if (byteCount <= 0)
        {
            return "0 B";
        }

        string[] units = ["B", "KB", "MB", "GB"];
        var size = (double)byteCount;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }

    private readonly record struct PlaybackRequest(
        string PlaybackMode,
        string? AudioAssetPath,
        string? FallbackMessage);

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

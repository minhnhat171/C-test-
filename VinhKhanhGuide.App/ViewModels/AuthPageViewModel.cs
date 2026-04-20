using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;

namespace VinhKhanhGuide.App.ViewModels;

public class AuthPageViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;
    private readonly IAudioSettingsService _audioSettingsService;

    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private string _selectedLanguage = "vi";

    public AuthPageViewModel(
        IAuthService authService,
        IAudioSettingsService audioSettingsService)
    {
        _authService = authService;
        _audioSettingsService = audioSettingsService;
        var settings = _audioSettingsService.Load(UserPreferenceScope.BuildAudioSettingsPrefix(UserPreferenceScope.GuestScopeKey));
        _selectedLanguage = NormalizeLanguage(settings.LanguageCode);
        EnterAppCommand = new Command(async () => await EnterAppAsync(), () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand EnterAppCommand { get; }

    public IReadOnlyList<AudioSettingsOption> SupportedLanguages { get; } =
    [
        new() { Code = "vi", Label = "Tiếng Việt", Description = "Phù hợp cho khách nội địa và giọng đọc tiếng Việt." },
        new() { Code = "en", Label = "English", Description = "Good for international visitors who want English narration." },
        new() { Code = "zh", Label = "中文", Description = "适合希望使用中文界面和中文讲解的游客。" },
        new() { Code = "ko", Label = "한국어", Description = "한국어 화면과 음성 안내를 원하는 방문객에게 적합합니다." },
        new() { Code = "fr", Label = "Français", Description = "Adapté aux visiteurs qui souhaitent une interface et une narration en français." }
    ];

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsNotBusy));
            (EnterAppCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool IsNotBusy => !IsBusy;

    public string WelcomeTitle => SelectedLanguage switch
    {
        "en" => "Enter the food street",
        "zh" => "进入美食街导览",
        "ko" => "음식 거리 가이드 입장",
        "fr" => "Entrer dans le guide",
        _ => "Vào nhanh phố ẩm thực"
    };

    public string WelcomeSubtitle => SelectedLanguage switch
    {
        "en" => "Visitors can pick their language before entering the main interface. Narration will follow this choice automatically.",
        "zh" => "游客可以在进入主界面前先选择语言，语音讲解也会自动跟随这个选择。",
        "ko" => "방문객은 메인 화면에 들어가기 전에 언어를 먼저 고를 수 있고, 음성 안내도 그 선택을 따라갑니다.",
        "fr" => "Les visiteurs peuvent choisir leur langue avant d'entrer dans l'interface principale. La narration suivra automatiquement ce choix.",
        _ => "Du khách có thể chọn ngôn ngữ phù hợp trước khi vào giao diện chính. Talk to Speech bên trong sẽ tự đổi theo lựa chọn này."
    };

    public string WelcomeHint => SelectedLanguage switch
    {
        "en" => "The app still runs in visitor mode, with no sign-in or registration required.",
        "zh" => "应用仍以访客模式运行，无需登录或注册。",
        "ko" => "앱은 방문객 모드로 동작하므로 로그인이나 회원가입이 필요하지 않습니다.",
        "fr" => "L'application fonctionne en mode visiteur, sans connexion ni inscription.",
        _ => "App vẫn chạy ở chế độ khách tham quan, không cần đăng nhập hoặc đăng ký."
    };

    public string HeroDescription => SelectedLanguage switch
    {
        "en" => "Pick a language once and the whole app, including TTS, will follow it.",
        "zh" => "先选语言，进入后整套界面和 TTS 会一起切换。",
        "ko" => "언어를 먼저 고르면 앱 화면과 TTS가 함께 바뀝니다.",
        "fr" => "Choisissez la langue une fois, puis l'interface et le TTS suivront.",
        _ => "Chọn ngôn ngữ một lần để toàn bộ giao diện và TTS đổi theo."
    };

    public string HeroNote => SelectedLanguage switch
    {
        "en" => "The QR entry flow is still reserved and ready for the next prompt.",
        "zh" => "二维码进入流程已经预留好，下一步可以继续接上。",
        "ko" => "QR 진입 흐름도 다음 단계에서 바로 연결할 수 있도록 자리를 남겨 두었습니다.",
        "fr" => "Le flux d'entrée par QR est déjà réservé pour l'étape suivante.",
        _ => "Luồng quét QR là hướng đúng và mình đã chừa chỗ để nối ở prompt sau."
    };

    public string StepOneTitle => SelectedLanguage switch
    {
        "en" => "Choose language",
        "zh" => "选择语言",
        "ko" => "언어 선택",
        "fr" => "Choisir la langue",
        _ => "Chọn ngôn ngữ"
    };

    public string StepTwoTitle => SelectedLanguage switch
    {
        "en" => "Explore the app",
        "zh" => "开始探索",
        "ko" => "앱 둘러보기",
        "fr" => "Découvrir l'app",
        _ => "Khám phá app"
    };

    public string StepThreeTitle => SelectedLanguage switch
    {
        "en" => "Tap to listen",
        "zh" => "点击收听",
        "ko" => "터치해 듣기",
        "fr" => "Touchez pour écouter",
        _ => "Chọn box để nghe"
    };

    public string LanguagePickerPlaceholder => SelectedLanguage switch
    {
        "en" => "Select language",
        "zh" => "选择语言",
        "ko" => "언어 선택",
        "fr" => "Choisir la langue",
        _ => "Chọn ngôn ngữ"
    };

    public string EnterAppButtonText => SelectedLanguage switch
    {
        "en" => "Explore the food street",
        "zh" => "探索美食街",
        "ko" => "음식 거리를 둘러보기",
        "fr" => "Explorer la rue gastronomique",
        _ => "Khám phá phố ẩm thực"
    };

    public string LanguagePromptTitle => SelectedLanguage switch
    {
        "en" => "Where are you from?",
        "zh" => "您来自哪里？",
        "ko" => "어느 나라에서 오셨나요?",
        "fr" => "D'où venez-vous ?",
        _ => "Bạn đến từ đâu?"
    };

    public string LanguagePromptSubtitle => SelectedLanguage switch
    {
        "en" => "The app UI and TTS will switch together.",
        "zh" => "界面与 TTS 会一起切换。",
        "ko" => "앱 UI와 TTS가 함께 전환됩니다.",
        "fr" => "L'interface et le TTS changeront ensemble.",
        _ => "Giao diện và TTS sẽ đổi cùng lúc."
    };

    public string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            var normalizedLanguage = NormalizeLanguage(value);
            if (!SetProperty(ref _selectedLanguage, normalizedLanguage))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedLanguageOption));
            OnPropertyChanged(nameof(SelectedLanguageDisplayName));
            OnPropertyChanged(nameof(SelectedLanguageSummary));
            OnPropertyChanged(nameof(WelcomeTitle));
            OnPropertyChanged(nameof(WelcomeSubtitle));
            OnPropertyChanged(nameof(WelcomeHint));
            OnPropertyChanged(nameof(LanguagePromptTitle));
            OnPropertyChanged(nameof(LanguagePromptSubtitle));
            OnPropertyChanged(nameof(EnterAppButtonText));
            OnPropertyChanged(nameof(HeroDescription));
            OnPropertyChanged(nameof(HeroNote));
            OnPropertyChanged(nameof(StepOneTitle));
            OnPropertyChanged(nameof(StepTwoTitle));
            OnPropertyChanged(nameof(StepThreeTitle));
            OnPropertyChanged(nameof(LanguagePickerPlaceholder));
        }
    }

    public AudioSettingsOption? SelectedLanguageOption
    {
        get => SupportedLanguages.FirstOrDefault(item =>
            string.Equals(item.Code, SelectedLanguage, StringComparison.OrdinalIgnoreCase));
        set => SelectedLanguage = value?.Code ?? "vi";
    }

    public string SelectedLanguageDisplayName => GetLanguageDisplayName(SelectedLanguage);

    public string SelectedLanguageSummary => SelectedLanguage switch
    {
        "en" => $"{GetSelectedLanguageDescription(SelectedLanguage)} TTS inside the app will also switch to {SelectedLanguageDisplayName}.",
        "zh" => $"{GetSelectedLanguageDescription(SelectedLanguage)} App 内的 TTS 也会同步切换为 {SelectedLanguageDisplayName}。",
        "ko" => $"{GetSelectedLanguageDescription(SelectedLanguage)} 앱 안의 TTS도 {SelectedLanguageDisplayName}(으)로 함께 바뀝니다.",
        "fr" => $"{GetSelectedLanguageDescription(SelectedLanguage)} Le TTS dans l'application passera aussi en {SelectedLanguageDisplayName}.",
        _ => $"{GetSelectedLanguageDescription(SelectedLanguage)} TTS bên trong app cũng sẽ tự đổi theo {SelectedLanguageDisplayName}."
    };

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (!SetProperty(ref _errorMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasErrorMessage));
        }
    }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    private async Task EnterAppAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            SaveGuestLanguagePreference();
            var result = await _authService.ContinueAsGuestAsync();
            if (!result.Succeeded)
            {
                ErrorMessage = result.Message;
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool SetProperty<T>(
        ref T backingStore,
        T value,
        [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SaveGuestLanguagePreference()
    {
        var preferencePrefix = UserPreferenceScope.BuildAudioSettingsPrefix(UserPreferenceScope.GuestScopeKey);
        var currentSettings = _audioSettingsService.Load(preferencePrefix);

        _audioSettingsService.Save(
            preferencePrefix,
            new AudioSettingsState
            {
                LanguageCode = SelectedLanguage,
                PlaybackMode = currentSettings.PlaybackMode,
                AutoNarrationEnabled = currentSettings.AutoNarrationEnabled
            });
    }

    private string NormalizeLanguage(string? languageCode)
    {
        return SupportedLanguages.Any(item =>
            string.Equals(item.Code, languageCode, StringComparison.OrdinalIgnoreCase))
            ? languageCode!.Trim().ToLowerInvariant()
            : "vi";
    }

    private static string GetLanguageDisplayName(string? languageCode) => languageCode?.Trim().ToLowerInvariant() switch
    {
        "en" => "English",
        "zh" => "中文",
        "ko" => "한국어",
        "fr" => "Français",
        _ => "Tiếng Việt"
    };

    private string GetSelectedLanguageDescription(string? languageCode)
    {
        return SupportedLanguages.FirstOrDefault(item =>
                   string.Equals(item.Code, languageCode, StringComparison.OrdinalIgnoreCase))
               ?.Description
           ?? languageCode?.Trim().ToLowerInvariant() switch
           {
               "en" => "The app will prioritize narration in your selected language.",
               "zh" => "应用会优先使用你所选择的语言进行讲解。",
               "ko" => "앱은 선택한 언어에 맞춰 음성 안내를 제공합니다.",
               "fr" => "L'application donnera la priorité à la narration dans la langue choisie.",
               _ => "App sẽ ưu tiên phần thuyết minh đúng với ngôn ngữ đã chọn."
           };
    }
}

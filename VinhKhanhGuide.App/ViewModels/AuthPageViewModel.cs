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
        new() { Code = "vi", Label = "Tiếng Việt", FlagEmoji = "🇻🇳", Description = "Phù hợp cho khách nội địa và giọng đọc tiếng Việt." },
        new() { Code = "en", Label = "English", FlagEmoji = "🇺🇸", Description = "Good for international visitors who want English narration." },
        new() { Code = "zh", Label = "中文", FlagEmoji = "🇨🇳", Description = "适合希望使用中文界面和中文讲解的游客。" },
        new() { Code = "ja", Label = "日本語", FlagEmoji = "🇯🇵", Description = "日本語の案内と音声ガイドを希望する来訪者向けです。" },
        new() { Code = "de", Label = "Deutsch", FlagEmoji = "🇩🇪", Description = "Geeignet für Gäste, die deutsche Hinweise und Erzählungen wünschen." }
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
        "ja" => "グルメ通りガイドへ",
        "de" => "Zum Food-Street-Guide",
        _ => "Vào nhanh phố ẩm thực"
    };

    public string WelcomeSubtitle => SelectedLanguage switch
    {
        "en" => "Visitors can pick their language before entering the main interface. Narration will follow this choice automatically.",
        "zh" => "游客可以先选择语言，再进入主界面。语音讲解会自动跟随这个选择。",
        "ja" => "来訪者はメイン画面に入る前に言語を選べます。音声ガイドもこの選択に合わせます。",
        "de" => "Besucher wählen vor dem Start ihre Sprache. Die Erzählung folgt dieser Auswahl automatisch.",
        _ => "Du khách có thể chọn ngôn ngữ phù hợp trước khi vào app. Phần thuyết minh sẽ đổi theo lựa chọn này."
    };

    public string WelcomeHint => SelectedLanguage switch
    {
        "en" => "The app still runs in visitor mode, with no sign-in or registration required.",
        "zh" => "App 会以游客模式运行，不需要登录或注册。",
        "ja" => "アプリはゲストモードで動作し、ログインや登録は不要です。",
        "de" => "Die App läuft im Besuchermodus, ohne Anmeldung oder Registrierung.",
        _ => "App vẫn chạy ở chế độ khách tham quan, không cần đăng nhập hoặc đăng ký."
    };

    public string HeroDescription => SelectedLanguage switch
    {
        "en" => "Pick a language once and the guide will speak in that language.",
        "zh" => "选择一次语言，导览就会用该语言讲解。",
        "ja" => "一度言語を選ぶと、ガイドはその言語で案内します。",
        "de" => "Wählen Sie einmal eine Sprache, dann spricht der Guide in dieser Sprache.",
        _ => "Chọn ngôn ngữ một lần để app và thuyết minh đổi theo."
    };

    public string HeroNote => SelectedLanguage switch
    {
        "en" => "You can start with a tour, a nearby place, or a QR code.",
        "zh" => "您可以从路线、附近店铺或二维码开始。",
        "ja" => "ツアー、近くのお店、QRコードから開始できます。",
        "de" => "Sie können mit einer Tour, einem nahegelegenen Ort oder einem QR-Code starten.",
        _ => "Bạn có thể bắt đầu bằng tour, quán gần bạn hoặc mã QR."
    };

    public string StepOneTitle => SelectedLanguage switch
    {
        "en" => "Choose language",
        "zh" => "选择语言",
        "ja" => "言語を選択",
        "de" => "Sprache wählen",
        _ => "Chọn ngôn ngữ"
    };

    public string StepTwoTitle => SelectedLanguage switch
    {
        "en" => "Explore the app",
        "zh" => "开始探索",
        "ja" => "アプリを探索",
        "de" => "App erkunden",
        _ => "Khám phá app"
    };

    public string StepThreeTitle => SelectedLanguage switch
    {
        "en" => "Tap to listen",
        "zh" => "点击收听",
        "ja" => "タップして聞く",
        "de" => "Zum Anhören tippen",
        _ => "Bấm để nghe"
    };

    public string LanguagePickerPlaceholder => SelectedLanguage switch
    {
        "en" => "Select language",
        "zh" => "选择语言",
        "ja" => "言語を選択",
        "de" => "Sprache wählen",
        _ => "Chọn ngôn ngữ"
    };

    public string EnterAppButtonText => SelectedLanguage switch
    {
        "en" => "Explore the food street",
        "zh" => "探索美食街",
        "ja" => "グルメ通りを探索",
        "de" => "Food Street erkunden",
        _ => "Khám phá phố ẩm thực"
    };

    public string LanguagePromptTitle => SelectedLanguage switch
    {
        "en" => "Where are you from?",
        "zh" => "您来自哪里？",
        "ja" => "どちらから来ましたか？",
        "de" => "Woher kommen Sie?",
        _ => "Bạn đến từ đâu?"
    };

    public string LanguagePromptSubtitle => SelectedLanguage switch
    {
        "en" => "The interface and narration will switch together.",
        "zh" => "界面与语音讲解会一起切换。",
        "ja" => "画面表示と音声ガイドが一緒に切り替わります。",
        "de" => "Oberfläche und Erzählung wechseln gemeinsam.",
        _ => "Giao diện và thuyết minh sẽ đổi cùng lúc."
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
            OnPropertyChanged(nameof(SelectedLanguageDisplayLabel));
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

    public string SelectedLanguageDisplayLabel =>
        SelectedLanguageOption?.DisplayLabel ?? SelectedLanguageDisplayName;

    public string SelectedLanguageSummary => SelectedLanguage switch
    {
        "en" => $"{GetSelectedLanguageDescription(SelectedLanguage)} Narration will also switch to {SelectedLanguageDisplayName}.",
        "zh" => $"{GetSelectedLanguageDescription(SelectedLanguage)} 讲解也会同步切换为 {SelectedLanguageDisplayName}。",
        "ja" => $"{GetSelectedLanguageDescription(SelectedLanguage)} 音声ガイドも {SelectedLanguageDisplayName} に切り替わります。",
        "de" => $"{GetSelectedLanguageDescription(SelectedLanguage)} Die Erzählung wechselt ebenfalls zu {SelectedLanguageDisplayName}.",
        _ => $"{GetSelectedLanguageDescription(SelectedLanguage)} Phần thuyết minh cũng sẽ tự đổi theo {SelectedLanguageDisplayName}."
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
        "ja" => "日本語",
        "de" => "Deutsch",
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
               "ja" => "アプリは選択した言語の音声ガイドを優先します。",
               "de" => "Die App priorisiert die Erzählung in der gewählten Sprache.",
               _ => "App sẽ ưu tiên phần thuyết minh đúng với ngôn ngữ đã chọn."
           };
    }
}

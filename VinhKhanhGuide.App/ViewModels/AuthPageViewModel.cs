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
        new() { Code = "zh", Label = "中文", Description = "Phù hợp cho khách nói tiếng Trung." },
        new() { Code = "ko", Label = "한국어", Description = "Phù hợp cho khách nói tiếng Hàn." },
        new() { Code = "fr", Label = "Français", Description = "Phù hợp cho khách muốn nghe bản tiếng Pháp." }
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

    public string WelcomeTitle => "Chọn ngôn ngữ rồi vào app";

    public string WelcomeSubtitle =>
        "Du khách có thể chọn ngôn ngữ phù hợp trước khi vào giao diện chính. Talk to Speech bên trong sẽ tự đổi theo lựa chọn này.";

    public string WelcomeHint =>
        "App vẫn chạy ở chế độ khách tham quan, không cần đăng nhập hoặc đăng ký.";

    public string EnterAppButtonText => SelectedLanguage switch
    {
        "en" => "Explore the food street",
        "zh" => "探索美食街",
        "ko" => "음식 거리를 둘러보기",
        "fr" => "Explorer la rue gastronomique",
        _ => "Khám phá phố ẩm thực"
    };

    public string LanguagePromptTitle => "Where are you from?";

    public string LanguagePromptSubtitle => SelectedLanguage switch
    {
        "en" => "Choose your language before entering. The app and Talk to Speech will follow this choice.",
        "zh" => "进入应用前先选择语言，应用和语音讲解都会跟着切换。",
        "ko" => "앱에 들어가기 전에 언어를 선택하면 앱과 음성 안내가 함께 바뀝니다.",
        "fr" => "Choisissez votre langue avant d'entrer. L'application et la narration vocale suivront ce choix.",
        _ => "Chọn ngôn ngữ trước khi khám phá để app và Talk to Speech phát nội dung đúng với khách hàng."
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
            OnPropertyChanged(nameof(LanguagePromptSubtitle));
            OnPropertyChanged(nameof(EnterAppButtonText));
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

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using VinhKhanhGuide.App.Services;

namespace VinhKhanhGuide.App.ViewModels;

public class AuthPageViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;

    private bool _isBusy;
    private string _errorMessage = string.Empty;

    public AuthPageViewModel(IAuthService authService)
    {
        _authService = authService;
        EnterAppCommand = new Command(async () => await EnterAppAsync(), () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand EnterAppCommand { get; }

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

    public string WelcomeTitle => "Vào ứng dụng ngay";

    public string WelcomeSubtitle =>
        "Phiên bản hiện tại ưu tiên du khách đi thẳng vào giao diện chính. Luồng quét QR sẽ được nối ở prompt sau.";

    public string WelcomeHint =>
        "Tạm thời app chạy ở chế độ khách tham quan, không cần đăng nhập hoặc đăng ký.";

    public string EnterAppButtonText => "Khám phá phố ẩm thực";

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
}

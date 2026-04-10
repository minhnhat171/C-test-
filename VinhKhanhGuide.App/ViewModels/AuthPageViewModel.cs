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
    private bool _isLoginMode = true;
    private string _fullName = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;

    public AuthPageViewModel(IAuthService authService)
    {
        _authService = authService;

        SwitchToLoginCommand = new Command(SwitchToLoginMode);
        SwitchToRegisterCommand = new Command(SwitchToRegisterMode);
        SubmitCommand = new Command(async () => await SubmitAsync(), () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand SwitchToLoginCommand { get; }
    public ICommand SwitchToRegisterCommand { get; }
    public ICommand SubmitCommand { get; }

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
            (SubmitCommand as Command)?.ChangeCanExecute();
        }
    }

    public bool IsNotBusy => !IsBusy;

    public bool IsLoginMode
    {
        get => _isLoginMode;
        private set
        {
            if (!SetProperty(ref _isLoginMode, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsRegisterMode));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(FormSubtitle));
            OnPropertyChanged(nameof(FormHint));
            OnPropertyChanged(nameof(SubmitButtonText));
            ClearFeedback();
        }
    }

    public bool IsRegisterMode => !IsLoginMode;

    public string FormTitle => IsLoginMode ? "Đăng nhập để vào app" : "Tạo tài khoản mới";

    public string FormSubtitle => IsLoginMode
        ? "Tiếp tục hành trình khám phá ẩm thực Vĩnh Khánh bằng tên đăng nhập hoặc email."
        : "Tạo tài khoản cục bộ để khóa luồng truy cập trước khi vào app.";

    public string FormHint => IsLoginMode
        ? "Tài khoản quản trị mặc định hiện có sẵn: user / 12345."
        : "Tài khoản hiện được lưu nội bộ trên máy và có thể thay bằng auth server sau này.";

    public string SubmitButtonText => IsLoginMode ? "Đăng nhập" : "Tạo tài khoản";

    public string FullName
    {
        get => _fullName;
        set
        {
            if (SetProperty(ref _fullName, value))
            {
                ClearFeedback();
            }
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                ClearFeedback();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                ClearFeedback();
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
            {
                ClearFeedback();
            }
        }
    }

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

    private void SwitchToLoginMode()
    {
        IsLoginMode = true;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    private void SwitchToRegisterMode()
    {
        IsLoginMode = false;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    private async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ClearFeedback();

        try
        {
            var result = IsLoginMode
                ? await _authService.SignInAsync(Email, Password)
                : await _authService.RegisterAsync(FullName, Email, Password, ConfirmPassword);

            if (!result.Succeeded)
            {
                ErrorMessage = result.Message;
                return;
            }

            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ClearFeedback()
    {
        ErrorMessage = string.Empty;
    }

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

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
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _selectedAccountType = "Khách khám phá";
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;

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

    public IReadOnlyList<string> AccountTypeOptions { get; } =
    [
        "Khách khám phá",
        "Chủ quán"
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

    public string FormTitle => IsLoginMode ? "Đăng nhập để tiếp tục" : "Tạo tài khoản mới";

    public string FormSubtitle => IsLoginMode
        ? "Xác thực nhanh để vào bản đồ, GPS và phần thuyết minh tự động."
        : "Làm theo đúng sequence: kiểm tra dữ liệu, lưu tài khoản rồi quay lại đăng nhập.";

    public string FormHint => IsLoginMode
        ? "Tài khoản mẫu đang có sẵn: user / 12345678."
        : "Đăng ký xong app sẽ quay lại tab đăng nhập. Tên đăng nhập nên viết liền, không dấu.";

    public string SubmitButtonText => IsLoginMode ? "Đăng nhập" : "Tạo tài khoản";

    public string SelectedAccountTypeHint => SelectedAccountType == "Chủ quán"
        ? "Phù hợp cho quán muốn quản lý nội dung và dữ liệu tại điểm."
        : "Dành cho người dùng khám phá, nghe thuyết minh và theo dõi hành trình.";

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

    public string Username
    {
        get => _username;
        set
        {
            if (SetProperty(ref _username, value))
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

    public string SelectedAccountType
    {
        get => _selectedAccountType;
        set
        {
            if (!SetProperty(ref _selectedAccountType, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedAccountTypeHint));
            ClearFeedback();
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

    public string SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (!SetProperty(ref _successMessage, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSuccessMessage));
        }
    }

    public bool HasSuccessMessage => !string.IsNullOrWhiteSpace(SuccessMessage);

    private void SwitchToLoginMode()
    {
        IsLoginMode = true;
        FullName = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        SelectedAccountType = AccountTypeOptions[0];
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
                ? await _authService.SignInAsync(Username, Password)
                : await _authService.RegisterAsync(
                    FullName,
                    Username,
                    Password,
                    ConfirmPassword,
                    GetSelectedRole());

            if (!result.Succeeded)
            {
                ErrorMessage = result.Message;
                return;
            }

            Password = string.Empty;
            ConfirmPassword = string.Empty;

            if (IsRegisterMode)
            {
                FullName = string.Empty;
                SelectedAccountType = AccountTypeOptions[0];
                IsLoginMode = true;
            }

            SuccessMessage = result.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string GetSelectedRole()
    {
        return SelectedAccountType == "Chủ quán" ? "poi_owner" : "user";
    }

    private void ClearFeedback()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
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

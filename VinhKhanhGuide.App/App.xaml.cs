using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.App.Views;

namespace VinhKhanhGuide.App;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;
    private readonly IActiveDeviceTracker _activeDeviceTracker;
    private bool _isHandlingQrRequests;
    private bool _isSigningInGuestForQr;

    public App(
        IServiceProvider serviceProvider,
        IAuthService authService,
        IActiveDeviceTracker activeDeviceTracker)
    {
        _serviceProvider = serviceProvider;
        _authService = authService;
        _activeDeviceTracker = activeDeviceTracker;
        _authService.SessionChanged += OnSessionChanged;
        QrDeepLinkBroker.PendingRequestAvailable += OnPendingQrRequestAvailable;

        UpdateRootPage();
        _ = _activeDeviceTracker.StartAsync();
        _ = DrainPendingQrRequestsAsync();
    }

    private void OnPendingQrRequestAvailable(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => _ = DrainPendingQrRequestsAsync());
    }

    private void OnSessionChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateRootPage();
            _ = _activeDeviceTracker.SendHeartbeatAsync();
            _ = DrainPendingQrRequestsAsync();
        });
    }

    protected override void OnStart()
    {
        _ = _activeDeviceTracker.StartAsync();
    }

    protected override void OnResume()
    {
        _ = _activeDeviceTracker.StartAsync();
    }

    protected override void OnSleep()
    {
        _ = StopActiveDeviceTrackerAsync();
    }

    private async Task StopActiveDeviceTrackerAsync()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await _activeDeviceTracker.StopAsync(cancellation.Token);
    }

    private void UpdateRootPage()
    {
        if (!_authService.IsAuthenticated)
        {
            if (MainPage is NavigationPage navigationPage &&
                navigationPage.CurrentPage is AuthPage)
            {
                return;
            }

            MainPage = CreateAuthRootPage();
            return;
        }

        try
        {
            if (MainPage is AppShell)
            {
                return;
            }

            var shell = _serviceProvider.GetRequiredService<AppShell>();
            MainPage = shell;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Failed to create AppShell: {ex}");

            var fallbackRoot = CreateAuthRootPage();
            MainPage = fallbackRoot;

            _ = fallbackRoot.CurrentPage.DisplayAlert(
                "Không thể mở ứng dụng",
                $"Có lỗi khi khởi tạo giao diện chính: {ex.Message}",
                "OK");
        }
    }

    private async Task DrainPendingQrRequestsAsync()
    {
        if (_isHandlingQrRequests)
        {
            return;
        }

        _isHandlingQrRequests = true;

        try
        {
            while (QrDeepLinkBroker.TryConsumePendingRequest(out var request))
            {
                await HandleQrRequestAsync(request);
            }
        }
        finally
        {
            _isHandlingQrRequests = false;
        }
    }

    private async Task HandleQrRequestAsync(QrDeepLinkRequest request)
    {
        try
        {
            if (!_authService.IsAuthenticated)
            {
                if (_isSigningInGuestForQr)
                {
                    return;
                }

                _isSigningInGuestForQr = true;

                try
                {
                    var authResult = await _authService.ContinueAsGuestAsync();
                    if (!authResult.Succeeded)
                    {
                        return;
                    }
                }
                finally
                {
                    _isSigningInGuestForQr = false;
                }
            }

            UpdateRootPage();

            var shell = await WaitForShellAsync();
            if (shell is null)
            {
                return;
            }

            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            var opened = await mainViewModel.OpenPoiFromQrAsync(request.PoiId, request.AutoPlay);
            if (!opened)
            {
                await shell.DisplayAlert(
                    "Chưa mở được mã QR",
                    "Mã QR này chưa liên kết với nội dung thuyết minh.",
                    "OK");
                return;
            }

            await OpenPoiDetailAsync(shell);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Failed to handle QR deep link: {ex}");
        }
    }

    private async Task<AppShell?> WaitForShellAsync()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            if (MainPage is AppShell shell)
            {
                return shell;
            }

            UpdateRootPage();
            await Task.Delay(100);
        }

        return MainPage as AppShell;
    }

    private async Task OpenPoiDetailAsync(AppShell shell)
    {
        var navigation = shell.Navigation;
        if (navigation.NavigationStack.LastOrDefault() is PoiDetailPage)
        {
            return;
        }

        var detailPage = _serviceProvider.GetRequiredService<PoiDetailPage>();
        await navigation.PushAsync(detailPage);
    }

    private NavigationPage CreateAuthRootPage()
    {
        return new NavigationPage(_serviceProvider.GetRequiredService<AuthPage>())
        {
            BarBackgroundColor = Color.FromArgb("#2F80FF"),
            BarTextColor = Colors.White
        };
    }
}

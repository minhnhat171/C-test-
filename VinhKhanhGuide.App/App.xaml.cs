using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.App.Views;
using VinhKhanhGuide.Core.Contracts;

namespace VinhKhanhGuide.App;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthService _authService;
    private readonly IActiveDeviceTracker _activeDeviceTracker;
    private readonly IApiEndpointService _apiEndpointService;
    private bool _isHandlingQrRequests;
    private bool _isSigningInGuestForQr;

    public App(
        IServiceProvider serviceProvider,
        IAuthService authService,
        IActiveDeviceTracker activeDeviceTracker,
        IApiEndpointService apiEndpointService)
    {
        _serviceProvider = serviceProvider;
        _authService = authService;
        _activeDeviceTracker = activeDeviceTracker;
        _apiEndpointService = apiEndpointService;
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
            if (_apiEndpointService.TrySetBaseUrl(request.ApiBaseUrl))
            {
                _ = _activeDeviceTracker.SendHeartbeatAsync();
            }

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

            var opened = await OpenQrTargetAsync(request, shell);
            if (!opened)
            {
                await shell.DisplayAlert(
                    "Chưa mở được mã QR",
                    "Mã QR này chưa liên kết với nội dung thuyết minh.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[App] Failed to handle QR deep link: {ex}");
        }
    }

    private async Task<bool> OpenQrTargetAsync(QrDeepLinkRequest request, AppShell shell)
    {
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

        if (string.Equals(request.TargetType, QrTargetKinds.Tour, StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(request.TargetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId))
            {
                return false;
            }

            var openedTour = await mainViewModel.OpenTourFromQrAsync(tourId);
            if (openedTour)
            {
                await OpenActiveTourPageAsync(shell);
            }

            return openedTour;
        }

        if (!Guid.TryParse(request.TargetId, out var poiId))
        {
            return false;
        }

        var openedPoi = await mainViewModel.OpenPoiFromQrAsync(poiId, request.AutoPlay);
        if (openedPoi)
        {
            await OpenPoiDetailAsync(shell);
        }

        return openedPoi;
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

    private async Task OpenActiveTourPageAsync(AppShell shell)
    {
        var navigation = shell.Navigation;
        if (navigation.NavigationStack.LastOrDefault() is ActiveTourPage)
        {
            return;
        }

        var activeTourPage = _serviceProvider.GetRequiredService<ActiveTourPage>();
        await navigation.PushAsync(activeTourPage);
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

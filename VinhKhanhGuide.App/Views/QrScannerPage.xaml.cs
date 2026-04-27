using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Xaml;
using VinhKhanhGuide.App.Services;
using VinhKhanhGuide.App.ViewModels;
using VinhKhanhGuide.Core.Contracts;
using ZXing.Net.Maui;

namespace VinhKhanhGuide.App.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class QrScannerPage : ContentPage
{
    private readonly IQrResolveService _qrResolveService;
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;
    private int _isHandlingResult;

    public QrScannerPage(
        IQrResolveService qrResolveService,
        MainViewModel viewModel,
        IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _qrResolveService = qrResolveService;
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;

        CameraReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false,
            TryHarder = true
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await EnsureCameraReadyAsync();
    }

    protected override void OnDisappearing()
    {
        CameraReader.IsDetecting = false;
        base.OnDisappearing();
    }

    private async Task EnsureCameraReadyAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            var shouldRequest = await DisplayAlert(
                "Cần dùng camera",
                "Ứng dụng cần camera để quét mã QR tại quán. Nếu không muốn cấp quyền, bạn vẫn có thể nhập mã thủ công.",
                "Cho phép",
                "Nhập mã");

            if (!shouldRequest)
            {
                StatusLabel.Text = "Bạn có thể nhập mã QR thủ công ở bên dưới.";
                ResumeButton.IsVisible = true;
                return;
            }

            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            StatusLabel.Text = "Ứng dụng chưa được cấp quyền camera. Hãy nhập mã thủ công nếu cần.";
            ResumeButton.IsVisible = true;
            return;
        }

        Interlocked.Exchange(ref _isHandlingResult, 0);
        ResumeButton.IsVisible = false;
        StatusLabel.Text = "Đưa mã QR vào khung camera";
        CameraReader.IsDetecting = true;
    }

    private void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        var code = e.Results?
            .Select(result => result.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(code) ||
            Interlocked.Exchange(ref _isHandlingResult, 1) == 1)
        {
            return;
        }

        Dispatcher.Dispatch(async () => await HandleCodeAsync(code));
    }

    private async Task HandleCodeAsync(string code)
    {
        CameraReader.IsDetecting = false;
        ResumeButton.IsVisible = false;
        StatusLabel.Text = "Đang kiểm tra mã QR...";

        var resolved = await _qrResolveService.ResolveAsync(code);
        if (resolved is null || !resolved.Resolved)
        {
            await ShowScanErrorAsync("Mã QR này chưa liên kết với POI hoặc tour.");
            return;
        }

        if (string.Equals(resolved.TargetType, QrTargetKinds.Tour, StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(resolved.TargetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tourId) ||
                !await _viewModel.OpenTourFromQrAsync(tourId))
            {
                await ShowScanErrorAsync("Không mở được tour từ mã QR này.");
                return;
            }

            await CloseScannerAndPushAsync(_serviceProvider.GetRequiredService<ActiveTourPage>());
            return;
        }

        if (!Guid.TryParse(resolved.TargetId, out var poiId) ||
            !await _viewModel.OpenPoiFromQrAsync(poiId))
        {
            await ShowScanErrorAsync("Không mở được POI từ mã QR này.");
            return;
        }

        await CloseScannerAndPushAsync(_serviceProvider.GetRequiredService<PoiDetailPage>());
    }

    private async Task SubmitManualCodeAsync()
    {
        var code = ManualCodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            StatusLabel.Text = "Nhập mã QR hoặc đường dẫn QR để mở.";
            return;
        }

        if (Interlocked.Exchange(ref _isHandlingResult, 1) == 1)
        {
            return;
        }

        await HandleCodeAsync(code);
    }

    private async Task ShowScanErrorAsync(string message)
    {
        StatusLabel.Text = message;
        ResumeButton.IsVisible = true;
        await DisplayAlert("Chưa mở được mã QR", message, "OK");
        Interlocked.Exchange(ref _isHandlingResult, 0);
    }

    private async Task CloseScannerAndPushAsync(Page nextPage)
    {
        var navigation = Navigation;
        CameraReader.IsDetecting = false;

        if (navigation.NavigationStack.LastOrDefault() == this)
        {
            await navigation.PopAsync(false);
        }

        await navigation.PushAsync(nextPage);
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        CameraReader.IsDetecting = false;
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
    }

    private async void OnResumeClicked(object? sender, EventArgs e)
    {
        await EnsureCameraReadyAsync();
    }

    private void OnTorchClicked(object? sender, EventArgs e)
    {
        CameraReader.IsTorchOn = !CameraReader.IsTorchOn;
    }

    private async void OnManualSubmitClicked(object? sender, EventArgs e)
    {
        await SubmitManualCodeAsync();
    }

    private async void OnManualCodeCompleted(object? sender, EventArgs e)
    {
        await SubmitManualCodeAsync();
    }
}

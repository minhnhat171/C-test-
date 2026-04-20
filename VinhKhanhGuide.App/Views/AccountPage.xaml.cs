using Microsoft.Extensions.DependencyInjection;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class AccountPage : ContentPage
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public AccountPage(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.ResetAccountProfileEditor();
        _viewModel.ResetAudioSettingsDraft();
        _viewModel.RefreshListeningHistoryCommand.Execute(null);
    }

    private async void OnListeningHistoryItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject bindable ||
            bindable.BindingContext is not Models.ListeningHistoryDisplayItem item)
        {
            return;
        }

        var canOpen = await _viewModel.OpenListeningHistoryDetailAsync(item.Id);
        if (!canOpen)
        {
            await DisplayAlert(
                _viewModel.ListeningHistorySectionTitle,
                GetLocalizedText(
                    "Không thể mở chi tiết từ bản ghi này.",
                    "Could not open details from this record.",
                    "无法从这条记录打开详情。",
                    "이 기록에서 상세를 열 수 없습니다.",
                    "Impossible d'ouvrir les détails depuis cet enregistrement."),
                GetLocalizedText("OK", "OK", "好的", "확인", "OK"));
            return;
        }

        var detailPage = _serviceProvider.GetRequiredService<PoiDetailPage>();
        await Navigation.PushAsync(detailPage);
    }

    private string GetLocalizedText(string vi, string en, string zh, string ko, string fr)
    {
        return _viewModel.SelectedLanguage switch
        {
            "en" => en,
            "zh" => zh,
            "ko" => ko,
            "fr" => fr,
            _ => vi
        };
    }
}

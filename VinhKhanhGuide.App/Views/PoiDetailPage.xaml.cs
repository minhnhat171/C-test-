using Microsoft.Maui.ApplicationModel;
using VinhKhanhGuide.App.ViewModels;

namespace VinhKhanhGuide.App.Views;

public partial class PoiDetailPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public PoiDetailPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private async void OnNarrateSelectedClicked(object? sender, EventArgs e)
    {
        await _viewModel.ToggleSelectedPoiNarrationAsync();
    }

    private async void OnOpenMapClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_viewModel.SelectedPoiMapLink))
        {
            await DisplayAlert(
                _viewModel.PoiDetailPageTitle,
                GetLocalizedText(
                    "Quán hiện tại chưa có link chỉ đường.",
                    "This place does not have a navigation link yet.",
                    "该地点暂时没有导航链接。",
                    "이 장소에는 아직 길찾기 링크가 없습니다.",
                    "Ce lieu ne dispose pas encore d'un lien d'itinéraire."),
                GetLocalizedText("OK", "OK", "好的", "확인", "OK"));
            return;
        }

        await Browser.Default.OpenAsync(_viewModel.SelectedPoiMapLink, BrowserLaunchMode.SystemPreferred);
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

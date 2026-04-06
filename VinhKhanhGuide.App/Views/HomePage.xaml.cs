using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Devices.Sensors;
using VinhKhanhGuide.App.Models;

namespace VinhKhanhGuide.App.Views;

public partial class HomePage : ContentPage
{
    public ObservableCollection<Food> Foods { get; set; }
    public ObservableCollection<Place> Places { get; set; }

    public HomePage()
    {
        InitializeComponent();

        // FOOD
        Foods = new ObservableCollection<Food>
        {
            new Food { Name="Ốc", Image="food1.png" },
            new Food { Name="Hàu", Image="food2.png" },
            new Food { Name="Tôm", Image="food3.png" },
            new Food { Name="Mực", Image="food4.png" }
        };

        // PLACE
        Places = new ObservableCollection<Place>
        {
            new Place { Name="Ốc Oanh", Description="Ốc nổi tiếng đông khách", Image="ocoanh.png"},
            new Place { Name="Ốc Thảo", Description="Hải sản tươi giá rẻ", Image="octhao.png"},
            new Place { Name="Ốc Vũ", Description="Mở khuya, ăn đêm", Image="ocvu.png"},
            new Place { Name="Ốc Sáu Nở", Description="Nước chấm đậm đà", Image="ocsauno.png"},
            new Place { Name="Bê Ốc", Description="Vỉa hè thoải mái", Image="beoc.png"},
            new Place { Name="Ốc Sò Nò", Description="Menu đa dạng", Image="ocsono.png"},
            new Place { Name="Thủy Seafood", Description="Giá rẻ 20k", Image="thuyseafood.png"},
            new Place { Name="Ớt Xiêm", Description="Lẩu + nướng", Image="otxiem.png"},
            new Place { Name="Chilli", Description="Lẩu Thái cay", Image="chillilau.png"},
            new Place { Name="Thế Giới Bò", Description="Bò nướng + lẩu", Image="thegioibo.png"},
        };
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // load map
        MyWebView.Source = "file:///android_asset/map.html";

        try
        {
            var location = await Geolocation.GetLastKnownLocationAsync();

            if (location != null)
            {
                double lat = location.Latitude;
                double lng = location.Longitude;

                string script = $"addUserMarker({lat}, {lng});";
                await MyWebView.EvaluateJavaScriptAsync(script);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private void OnFoodSelected(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.Cast<Food>().ToList();

        DisplayAlert("Bạn chọn", string.Join(", ", selected.Select(x => x.Name)), "OK");
    }
}
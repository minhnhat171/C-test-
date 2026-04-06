using System.Collections.ObjectModel;
using VinhKhanhGuide.App.Models;
using VinhKhanhGuide.App.Services;

namespace VinhKhanhGuide.App.ViewModels;

public class HomeViewModel
{
    public ObservableCollection<Food> Foods { get; set; }
    public ObservableCollection<Restaurant> Restaurants { get; set; }

    public HomeViewModel()
    {
        var service = new DataService();

        Foods = new ObservableCollection<Food>(service.GetFoods());
        Restaurants = new ObservableCollection<Restaurant>(service.GetRestaurants());
    }
}
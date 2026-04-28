using Microsoft.Maui.Controls.Xaml;
using VinhKhanhGuide.App.Views;

namespace VinhKhanhGuide.App;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage)
    {
        InitializeComponent();
        Items.Add(new ShellContent
        {
            Content = mainPage
        });
    }
}

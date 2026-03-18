using VinhKhanhGuide.App.Views;   // 👈 THÊM DÒNG NÀY

namespace VinhKhanhGuide.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new HomePage(); // 👈 dùng HomePage
	}
}
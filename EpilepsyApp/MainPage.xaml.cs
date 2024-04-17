using EpilepsyApp.ViewModel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;

namespace EpilepsyApp
{
	public partial class MainPage : ContentPage
	{
      public MainPage(MainViewModel vm)
		{
			InitializeComponent();
         BindingContext = vm;
      }
	}

}

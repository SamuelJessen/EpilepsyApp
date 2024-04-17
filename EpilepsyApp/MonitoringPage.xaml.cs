using EpilepsyApp.ViewModel;

namespace EpilepsyApp;

public partial class MonitoringPage : ContentPage
{
	public MonitoringPage(MonitoringViewModel vm)
	{
		InitializeComponent();
      BindingContext = vm;
   }
}
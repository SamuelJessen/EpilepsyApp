using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EpilepsyApp.Services;

namespace EpilepsyApp.ViewModel
{
	public partial class MainViewModel : ObservableObject
	{
		[ObservableProperty]
		string username;

		[ObservableProperty]
		string password;

		public BLEservice BLEservice { get; set; }
		private readonly IDecoder decoder;
		public IMQTTService mqttService { get; set; }
		public IRawDataService rawDataService { get; set; }

		public MainViewModel(BLEservice ble, IDecoder decoder, IMQTTService mqttClient, IRawDataService rawDataClient)
		{
			BLEservice = ble;
			this.decoder = decoder;
			mqttService = mqttClient;
			rawDataService = rawDataClient;
		}

		[ICommand]
		async Task Login()
		{
			await Shell.Current.GoToAsync($"{nameof(MonitoringPage)}?Username={username}");
		}

	}
}

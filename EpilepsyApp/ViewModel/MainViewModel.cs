using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EpilepsyApp.Constants;
using EpilepsyApp.Services;

namespace EpilepsyApp.ViewModel
{
	public partial class MainViewModel : ObservableObject
	{
		[ObservableProperty]
		string _patientID;

		[ObservableProperty]
		string _password;

		public BLEservice BLEservice { get; set; }
		private readonly IDecoder _decoder;
		public IMQTTService _mqttService { get; set; }
		public IRawDataService _rawDataService { get; set; }
		public IAPIService _apiService { get; set; }

		public MainViewModel(BLEservice ble, IDecoder decoder, IMQTTService mqttClient, IRawDataService rawDataClient, IAPIService apiService)
		{
			BLEservice = ble;
			_decoder = decoder;
			_mqttService = mqttClient;
			_rawDataService = rawDataClient;
			_apiService = apiService;
		}

		[ICommand]
		public async Task Login()
		{
			var loginResult = await _apiService.Login(PatientID, Password);
			if (loginResult)
			{
				await Shell.Current.GoToAsync($"{nameof(MonitoringPage)}?PatientID={PatientID}");
			}
			else
			{
				string errorMessage = $"Login failed!";
				await Shell.Current.DisplayAlert("Login Failed", errorMessage, "OK");
			}
		}

	}
}

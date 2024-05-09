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
		string patientID;

		[ObservableProperty]
		string password;

		public BLEservice BLEservice { get; set; }
		private readonly IDecoder decoder;
		public IMQTTService mqttService { get; set; }
		public IRawDataService rawDataService { get; set; }
		private readonly HttpClient httpClient;

		public MainViewModel(BLEservice ble, IDecoder decoder, IMQTTService mqttClient, IRawDataService rawDataClient)
		{
			BLEservice = ble;
			this.decoder = decoder;
			mqttService = mqttClient;
			rawDataService = rawDataClient;
			httpClient = new HttpClient();
		}

		[ICommand]
		public async Task Login()
		{
			var loginRequest = new { Id = patientID, Password = password };
			var json = JsonSerializer.Serialize(loginRequest);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			HttpResponseMessage response = await httpClient.PostAsync(APIStrings.ApiString + "/patients/login", content);

			if (response.IsSuccessStatusCode)
			{
				await Shell.Current.GoToAsync($"{nameof(MonitoringPage)}?patientID= {patientID}");
			}
			else
			{
				string errorMessage = $"Login failed: {response.StatusCode} - {response.ReasonPhrase}";
				await Shell.Current.DisplayAlert("Login Failed", errorMessage, "OK");
			}
		}

	}
}

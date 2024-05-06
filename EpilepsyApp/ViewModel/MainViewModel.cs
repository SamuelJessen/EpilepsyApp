using System.Net.Http;
using System.Text.Json;
using System.Text;
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
			await Shell.Current.GoToAsync($"{nameof(MonitoringPage)}?Username={username}");

			//var loginRequest = new { Id = username, Password = password };
			//var json = JsonSerializer.Serialize(loginRequest);
			//var content = new StringContent(json, Encoding.UTF8, "application/json");

			//HttpResponseMessage response = await httpClient.PostAsync("https://localhost:7128/patients/login", content);

			//if (response.IsSuccessStatusCode)
			//{
			//	await Shell.Current.GoToAsync($"{nameof(MonitoringPage)}?Username={username}");
			//}
			//else
			//{
			//	string errorMessage = $"Login failed: {response.StatusCode} - {response.ReasonPhrase}";
			//	await Shell.Current.DisplayAlert("Login Failed", errorMessage, "OK");
			//}
		}

	}
}

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EpilepsyApp.Constants;
using EpilepsyApp.DTO;
using EpilepsyApp.Models;

namespace EpilepsyApp.Services;
public class APIService : IAPIService
{
	private readonly HttpClient _httpClient;

	public APIService(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<Patient> GetPatient(string patientId)
	{
		var patient = await _httpClient.GetFromJsonAsync<Patient>(APIStrings.ApiString + $"/patients/{patientId}");
		if (patient == null)
			return null;
		return patient;

	}

	public async Task<bool> Login(string patientId, string password)
	{
		var loginRequest = new { Id = patientId, Password = password };
		var json = JsonSerializer.Serialize(loginRequest);
		var content = new StringContent(json, Encoding.UTF8, "application/json");

		HttpResponseMessage response = await _httpClient.PostAsync(APIStrings.ApiString + "/patients/login", content);

		if (response.IsSuccessStatusCode)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	public async Task<bool> UpdateThresholds(string patientId, int csi30, int csi50, int csi100, int modcsi100)
	{
		var thresholdRequest = new ThresholdRequest(){ CSIThreshold30 = csi30, CSIThreshold50 = csi50, CSIThreshold100 = csi100, ModCSIThreshold100 = modcsi100 };
		var response = await _httpClient.PutAsJsonAsync(APIStrings.ApiString + $"/patients/{patientId}/thresholds", thresholdRequest);
		if(response.IsSuccessStatusCode)
			return true;
		else
			return false;
	}

	public async Task<bool> PostAlarm(EcgAlarm alarm)
	{
		var response = await _httpClient.PostAsJsonAsync(APIStrings.ApiString + "/alarms", alarm);

		if (response.IsSuccessStatusCode)
			return true;
		else
			return false;
	}
}

public class ThresholdRequest
{
	public int CSIThreshold30 { get; set; }
	public int CSIThreshold50 { get; set; }
	public int CSIThreshold100 { get; set; }
	public int ModCSIThreshold100 { get; set; }
}

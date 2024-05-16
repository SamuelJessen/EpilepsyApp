using EpilepsyApp.DTO;
using EpilepsyApp.Models;

namespace EpilepsyApp.Services;
public interface IAPIService
{
	Task<bool> Login(string patientId, string password);
	Task<Patient> GetPatient(string patientId);
	Task<bool> UpdateThresholds(string patientId, int csi30, int csi50, int csi100, int modcsi100);
	Task<bool> PostAlarm(EcgAlarm alarm);
}

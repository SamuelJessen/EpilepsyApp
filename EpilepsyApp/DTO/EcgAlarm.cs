namespace EpilepsyApp.DTO;
public class EcgAlarm
{
	public Guid Id { get; set; }
	public string PatientID { get; set; }
	public int CSI30 { get; set; }
	public int CSI50 { get; set; }
	public int CSI100 { get; set; }
	public int ModCSI100 { get; set; }
	public int PatientCSIThreshold30 { get; set; }
	public int PatientCSIThreshold50 { get; set; }
	public int PatientCSIThreshold100 { get; set; }
	public int PatientModCSIThreshold100 { get; set; }
	public bool CSI30Alarm { get; set; }
	public bool CSI50Alarm { get; set; }
	public bool CSI100Alarm { get; set; }
	public bool ModCSI100Alarm { get; set; }
	public DateTime AlarmTimeStamp { get; set; }
}

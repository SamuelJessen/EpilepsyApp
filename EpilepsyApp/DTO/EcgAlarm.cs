namespace EpilepsyApp.DTO;
public class EcgAlarm
{
	public Guid Id { get; set; }
	public string PatientId { get; set; }
	public float CSI30 { get; set; }
	public float CSI50 { get; set; }
	public float CSI100 { get; set; }
	public float ModCSI100 { get; set; }
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

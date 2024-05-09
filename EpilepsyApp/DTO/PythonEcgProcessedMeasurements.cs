namespace EpilepsyApp.DTO;
public class PythonEcgProcessedMeasurements
{
	public string PatientID { get; set; }
	public DateTime TimeStamp { get; set; }
	public float CSI30 { get; set; }
	public float CSI50 { get; set; }
	public float CSI100 { get; set; }
	public float ModCSI100 { get; set; }
	public List<double> ProcessedEcgChannel1 { get; set; }
	public List<double> ProcessedEcgChannel2 { get; set; }
	public List<double> ProcessedEcgChannel3 { get; set; }
}

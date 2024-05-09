namespace EpilepsyApp.Models
{
	public class ECGBatchSeriesData
	{
		public string PatientID { get; set; }

		public DateTime TimeStamp { get; set; }
		public List<sbyte[]> EcgRawBytes { get; set; }
		public int Samples { get; set; }
	}
}

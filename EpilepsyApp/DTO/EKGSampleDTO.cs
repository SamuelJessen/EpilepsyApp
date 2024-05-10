namespace EpilepsyApp.DTO
{
	//make this class an observable by inheriting from the INotifyPropertyChanged interface
	public class EKGSampleDTO
	{
		public string PatientID { get; set; }
		public DateTime TimeStamp { get; set; }
		public sbyte[] RawBytes { get; set; }
	}
}

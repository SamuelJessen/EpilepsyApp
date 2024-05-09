namespace EpilepsyApp.Models;
public class Patient
{
	public string Id { get; set; }
	public string Name { get; set; }
	public int CSIThreshold30 { get; set; }
	public int CSIThreshold50 { get; set; }
	public int CSIThreshold100 { get; set; }
	public int ModCSIThreshold100 { get; set; }
	public string Password { get; set; }
}

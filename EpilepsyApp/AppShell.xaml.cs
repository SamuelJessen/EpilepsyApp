namespace EpilepsyApp
{
	public partial class AppShell : Shell
	{
		public AppShell()
		{
			InitializeComponent();

			Routing.RegisterRoute(nameof(MonitoringPage), typeof(MonitoringPage));
		}
	}
}

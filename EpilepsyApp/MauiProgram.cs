using EpilepsyApp.Services;
using EpilepsyApp.ViewModel;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace EpilepsyApp
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
			.UseSkiaSharp(true)
			.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

#if DEBUG
			builder.Logging.AddDebug();
#endif
			builder.Services.AddSingleton<IMQTTService, MqttService>();
			builder.Services.AddSingleton<IRawDataService, RawDataService>();
			builder.Services.AddSingleton<HttpClient>();
			builder.Services.AddSingleton<IAPIService, APIService>();
			builder.Services.AddSingleton<BLEservice>();
			builder.Services.AddSingleton<MainPage>();
			builder.Services.AddSingleton<MainViewModel>();

			builder.Services.AddSingleton<MonitoringPage>();
			builder.Services.AddSingleton<MonitoringViewModel>();
			builder.Services.AddSingleton<IDecoder, DecodingByteArray>();

			return builder.Build();
		}
	}
}

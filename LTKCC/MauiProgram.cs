using Microsoft.Extensions.Logging;
using SQLitePCL;

namespace LTKCC;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		Batteries_V2.Init();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<Data.AppDb>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

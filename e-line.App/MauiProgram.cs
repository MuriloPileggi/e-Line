using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using e_line.App.ViewModels;

namespace e_line.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register the HttpClientFactory service
		builder.Services.AddHttpClient();

		// Register our MainPage and its ViewModel
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<MainViewModel>();

		return builder.Build();
	}
}

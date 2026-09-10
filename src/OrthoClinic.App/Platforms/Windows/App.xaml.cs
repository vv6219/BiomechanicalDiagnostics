using Microsoft.UI.Xaml;

namespace OrthoClinic.WinUI;

public partial class App : MauiWinUIApplication
{
	public App()
	{
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), e.ExceptionObject?.ToString());
		};

		this.UnhandledException += (s, e) =>
		{
			File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), e.Exception?.ToString() ?? e.Message);
		};

		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

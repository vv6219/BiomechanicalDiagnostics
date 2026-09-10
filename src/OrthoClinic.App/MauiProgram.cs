namespace OrthoClinic;

using OrthoClinic.Core.Data;
using OrthoClinic.Core.Engine;
using OrthoClinic.Core.Services;
using OrthoClinic.Infrastructure.Data;
using OrthoClinic.Infrastructure.Services;
using OrthoClinic.UI.ViewModels;
using OrthoClinic.UI.Views;

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

        // Путь к защищенной локальной базе данных SQLite
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "orthoclinic_vault.db");

        // Регистрация служб ядра и инфраструктуры
        builder.Services.AddSingleton<IHorizontalAssessmentEngine, HorizontalAssessmentEngine>();
        builder.Services.AddSingleton<IAdaptiveProtocolEngine, AdaptiveProtocolEngine>();
        builder.Services.AddSingleton<IPodiatricPdfExportService, PodiatricPdfExportService>();
        builder.Services.AddSingleton<IExamRepository>(sp => new SqliteExamRepository(dbPath, "OrthoClinic.MedicalVault.Key.2026!#"));

        // Регистрация ViewModel и Views
        builder.Services.AddTransient<HorizontalTestsViewModel>();
        builder.Services.AddTransient<HorizontalAssessmentView>();

        return builder.Build();
    }
}

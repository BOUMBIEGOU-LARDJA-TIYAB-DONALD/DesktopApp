using desktop_app_hybrid.Services;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace desktop_app_hybrid
{
    // MauiProgram centralise la configuration MAUI / Blazor / DI / Configuration
    // Copie ce fichier en remplaçant l'ancien MauiProgram.cs (ou utilise son contenu)
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Charger le fichier AppSettings.json depuis la sortie du projet
            // Assure-toi que AppSettings.json est 
            //  - Build Action = Content
            //  - Copy to Output Directory = Copy if newer
            builder.Configuration
                   .AddJsonFile("AppSettings.json", optional: false, reloadOnChange: true);
                   //.AddEnvironmentVariables(); // utile pour override en dev/CI

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Blazor WebView pour MAUI
            builder.Services.AddMauiBlazorWebView();

            // Services applicatifs - toujours enregistrer hors DEBUG
            builder.Services.AddScoped<AuthService>();


            // Si tu as d'autres services, les ajouter ici, par ex. :
            // builder.Services.AddSingleton<AppState>();
            // builder.Services.AddScoped<DatabaseService>();

#if DEBUG
            // Outils de développement Blazor uniquement en DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

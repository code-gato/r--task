using Blazored.LocalStorage;
using Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace rtask
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            
            // se registran los servicios de autorización
            builder.Services.AddAuthorizationCore();

            // se registran los servicios de acceso a la memoria local del navegador
            builder.Services.AddBlazoredLocalStorage();
            
            // se registran los servicios de Firebase (Authentication, Realtime Database)
            builder.Services.AddScoped<FirebaseAuthService>();
            builder.Services.AddScoped<FirebaseAuthStateProvider>();
            builder.Services.AddScoped<FirebaseRealtimeDatabaseService>();
            // se registra my AuthStateProvider modificado como proveedor de estado de autentificación por defecto
            builder.Services.AddScoped<AuthenticationStateProvider, FirebaseAuthStateProvider>();
            // se filtran los logs de autorización (para prevenir mensajes innecesarios en la consola)
            builder.Logging.AddFilter(
                "Microsoft.AspNetCore.Authorization.DefaultAuthorizationService",
                LogLevel.Warning);

            await builder.Build().RunAsync();
        }
    }
}

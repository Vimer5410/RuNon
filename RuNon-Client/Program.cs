using System.Net;
using RuNon_Client.Components;
using RuNon_Client.Hubs;
using RuNon_Client.Services;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;


Log.Logger = new LoggerConfiguration()
    
    .MinimumLevel.Information() 
    
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) 
    
    .WriteTo.Console(
       
        theme: AnsiConsoleTheme.Code, 
        // шаблон вывода сообщений 
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(); 

ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddScoped<MatchMakingService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<SimpleHub>("/simplehub");
try
{
    app.Run();
}
catch (Exception ex)
{
    // в сслучае критической ошибки
    Log.Fatal(ex, "Приложение завершило работу неожиданно!");
}
finally
{
    
    Log.CloseAndFlush();
}
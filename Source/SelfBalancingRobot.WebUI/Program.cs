using SelfBalancingRobot.WebUI;

namespace SelfBalancingRobot;

public class Program
{
    private static CancellationTokenSource cancelTokenSource = new System.Threading.CancellationTokenSource();
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        host.RunAsync(cancelTokenSource.Token).GetAwaiter().GetResult();
    }
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
              .ConfigureLogging(logging =>
              {
                  logging.ClearProviders();
                  logging.AddConsole();
              })
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.ConfigureKestrel(serverOptions =>
                  {
                  });
                  webBuilder.UseStartup<Startup>();
              });
        return host;
    }
    public static void Shutdown()
    {
        cancelTokenSource.Cancel();
    }
}

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddRazorPages();

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//app.UseAuthorization();

//app.MapRazorPages();

//app.Run();

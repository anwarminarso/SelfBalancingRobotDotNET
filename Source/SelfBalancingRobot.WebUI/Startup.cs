﻿using Iot.Device.Bno055;
using Iot.Device.SensorHub;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using SelfBalancingRobot.WebUI.Configuration;
using SelfBalancingRobot.WebUI.Hubs;
using SelfBalancingRobot.WebUI.Models;
using SelfBalancingRobot.WebUI.Resources;
using SelfBalancingRobot.WebUI.Extensions;
using System.Device.Gpio;

namespace SelfBalancingRobot.WebUI;

public class Startup
{
    public IConfiguration Configuration { get; }
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }
    public virtual void ConfigureServices(IServiceCollection services)
    {
        var appSettings = new AppSettings();
        var deviceSettings = new DeviceSettings();
        var menuSettings = new MenuSettings();
        var calibrationSettings = new CalibrationSettings();

        Configuration.Bind(Utils.ConfigName_AppSettings, appSettings);
        Configuration.Bind(Utils.ConfigName_DeviceSettings, deviceSettings);
        Configuration.Bind(Utils.ConfigName_MenuSettings, menuSettings);
        Configuration.Bind(Utils.ConfigName_CalibrationSettings, calibrationSettings);

        services.AddSingleton(appSettings);
        services.AddSingleton(deviceSettings);
        services.AddSingleton(menuSettings);
        services.AddSingleton(calibrationSettings);

        #region Compression
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = System.IO.Compression.CompressionLevel.Optimal;
        });
        services.AddResponseCompression(options =>
        {
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = new[]
            {
                    "text/html",
                    "text/css",
                    "application/javascript",
                    "application/json",
                    "text/json",
                    "application/xml",
                    "text/xml",
                    "text/plain",
                    "image/svg+xml",
                    "application/x-font-ttf"
                };
        });
        #endregion

        services.AddMemoryCache();
        services.AddControllers();
        services.AddResponseCaching();
        var mvcBuilder = services.AddRazorPages(options =>
        {
            options.RootDirectory = "/Pages";
        });
        mvcBuilder.AddNewtonsoftJson(options =>
        {
            options.UseMemberCasing();
            options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        });
        services.AddSignalR().AddJsonProtocol(x =>
        {
            x.PayloadSerializerOptions.PropertyNamingPolicy = null;
        });
        services.AddEmbeddedResources();

        services.AddSingleton<WritableConfiguration>();

        services.AddSingleton<IMUContext>();
#if NO_GPIO
        services.AddSingleton<MotorContext>();
#else
        GpioController gpio = new GpioController(PinNumberingScheme.Logical);
        var motorContext = new MotorContext(gpio, deviceSettings);
        services.AddSingleton<MotorContext>(motorContext);
#endif
        services.AddSingleton<StabilizerContext>();
        services.AddSingleton<ControlContext>();

        services.AddSingleton<IMUHub>();
        services.AddSingleton<CalibrationHub>();
        services.AddSingleton<ControlHub>();
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env, AppSettings appSettings, 
        IMUContext imuContext, MotorContext motorContext, StabilizerContext stabilizerContext)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions()
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
        });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        if (appSettings.Headers != null && appSettings.Headers.Keys.Count > 0)
        {
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    foreach (var key in appSettings.Headers.Keys)
                    {
                        context.Response.Headers.Add(key, appSettings.Headers[key]);
                    }
                    return Task.CompletedTask;
                });
                await next();
            });
        }

        app.UseStaticFiles();
        // Use HTTPS.
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseCors();
        
        //app.UseSession();

        //app.UseAuthentication();
        //app.UseAuthorization();

        // User response compression
        app.UseResponseCompression();

        // end points
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllers();
            endpoints.MapHub<Hubs.ControlHub>("/ws/control", options =>
            {
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
            });
            endpoints.MapHub<Hubs.IMUHub>("/ws/imu", options =>
            {
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
            });
            endpoints.MapHub<Hubs.CalibrationHub>("/ws/calibration", options =>
            {
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
            });
        });


        #region Init
        {
            imuContext.Init();
            imuContext.StartMonitoring();
            motorContext.Init();
        }
        #endregion
    }
}
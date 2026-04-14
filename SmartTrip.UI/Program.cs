using Microsoft.EntityFrameworkCore;
using Serilog;
using SmartTrip.Data;
using SmartTrip.Models;
using SmartTrip.Application.Interfaces;
using SmartTrip.Application.Services;
using Microsoft.AspNetCore.Identity;
using QuestPDF;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SmartTrip application...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services));

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    builder.Services.AddDbContext<SmartTripDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddIdentityApiEndpoints<User>()
        .AddEntityFrameworkStores<SmartTripDbContext>()
        .AddDefaultTokenProviders();

    // Add services to the container.
    builder.Services.AddControllersWithViews();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ITripService, TripService>();
    builder.Services.AddHttpClient<ITripGeneratorService, TripGeneratorService>();
    builder.Services.AddHttpClient<SmartTrip.Application.Interfaces.IEventDiscoveryService, SmartTrip.Application.Services.EventDiscoveryService>(client =>
    {
        client.BaseAddress = new Uri("https://app.ticketmaster.com/");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    //gallery
    builder.Services.AddScoped<IGalleryService, GalleryService>();

    //package item
    builder.Services.AddScoped<IPackingService, PackingService>();

    // profile
    builder.Services.AddScoped<IProfileService, ProfileService>();

    //email sender
    builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, SmartTrip.Infrastructure.Services.EmailSender>();

    var app = builder.Build();

    // Configure QuestPDF license
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
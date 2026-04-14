using CTest.WebAdmin.Models;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
var poiApiBaseUrl = builder.Configuration["PoiApi:BaseUrl"] ?? "http://localhost:5287/";

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<AppDataService>();
builder.Services.Configure<QrCodeOptions>(builder.Configuration.GetSection("QrCode"));
builder.Services.AddHttpClient<PoiApiClient>(client =>
{
    client.BaseAddress = new Uri(poiApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<TourApiClient>(client =>
{
    client.BaseAddress = new Uri(poiApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<AudioGuideApiClient>(client =>
{
    client.BaseAddress = new Uri(poiApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<ListeningHistoryApiClient>(client =>
{
    client.BaseAddress = new Uri(poiApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AudioGuideAdminService>();
builder.Services.AddScoped<AudioGuideValidationService>();
builder.Services.AddScoped<PoiAdminService>();
builder.Services.AddScoped<TourAdminService>();
builder.Services.AddScoped<ListeningHistoryService>();
builder.Services.AddScoped<PoiValidationService>();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseDeveloperExceptionPage();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

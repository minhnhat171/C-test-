using CTest.WebAdmin.Models;
using CTest.WebAdmin.Security;
using CTest.WebAdmin.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using VinhKhanhGuide.Core.Configuration;

var builder = WebApplication.CreateBuilder(args);
var poiApiBaseUrl = builder.Configuration["PoiApi:BaseUrl"];
var adminApiKey = builder.Configuration["AdminApi:ApiKey"];

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "CTest.WebAdmin.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(WebAdminPolicies.AdminOnly, policy =>
        policy.RequireRole(WebAdminRoles.Admin));
    options.AddPolicy(WebAdminPolicies.OwnerArea, policy =>
        policy.RequireRole(WebAdminRoles.Admin, WebAdminRoles.PoiOwner));
});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<WebAdminAuthOptions>(builder.Configuration.GetSection("WebAdminAuth"));
builder.Services.AddSingleton<IWebAdminAccountStore, WebAdminAccountStore>();
builder.Services.AddSingleton<IWebAdminAuthService, WebAdminAuthService>();
builder.Services.AddScoped<IWebAdminCurrentUser, WebAdminCurrentUser>();
builder.Services.AddSingleton<WebDisplayClock>();
builder.Services.Configure<QrCodeOptions>(builder.Configuration.GetSection("QrCode"));
builder.Services.AddHttpClient<PoiApiClient>(ConfigureSharedApiClient);
builder.Services.AddHttpClient<TourApiClient>(ConfigureSharedApiClient);
builder.Services.AddHttpClient<AudioGuideApiClient>(ConfigureSharedApiClient);
builder.Services.AddHttpClient<ListeningHistoryApiClient>(ConfigureSharedApiClient);
builder.Services.AddHttpClient<ActiveDeviceApiClient>(ConfigureSharedApiClient);
builder.Services.AddHttpClient<UserManagementApiClient>(ConfigureSharedApiClient);
builder.Services.AddHttpClient("PublicApiProxy", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AudioGuideAdminService>();
builder.Services.AddScoped<AudioGuideValidationService>();
builder.Services.AddScoped<PoiAdminService>();
builder.Services.AddScoped<TourAdminService>();
builder.Services.AddScoped<ListeningHistoryService>();
builder.Services.AddScoped<PoiValidationService>();
builder.Services.AddScoped<PoiImageStorageService>();
builder.Services.AddScoped<TtsTranslationService>();

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
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

void ConfigureSharedApiClient(HttpClient client)
{
    client.BaseAddress = PoiApiDefaults.CreateBaseUri(poiApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);

    if (client.BaseAddress.Host.EndsWith(".ngrok-free.dev", StringComparison.OrdinalIgnoreCase))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("ngrok-skip-browser-warning", "true");
    }

    if (!string.IsNullOrWhiteSpace(adminApiKey))
    {
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Admin-Api-Key", adminApiKey);
    }
}

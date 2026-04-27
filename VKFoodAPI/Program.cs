using Microsoft.AspNetCore.Authentication;
using VKFoodAPI.Security;
using VKFoodAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddAuthentication(AdminApiKeyDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, AdminApiKeyAuthenticationHandler>(
        AdminApiKeyDefaults.SchemeName,
        _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminApiKeyDefaults.PolicyName, policy =>
    {
        policy.AddAuthenticationSchemes(AdminApiKeyDefaults.SchemeName);
        policy.RequireAuthenticatedUser();
    });
});
builder.Services.AddSingleton<PoiRepository>();
builder.Services.AddSingleton<AudioGuideRepository>();
builder.Services.AddSingleton<TourRepository>();
builder.Services.AddSingleton<QrCodeRepository>();
builder.Services.AddSingleton<ListeningHistoryRepository>();
builder.Services.AddSingleton<UserManagementRepository>();
builder.Services.AddSingleton<ActiveDeviceRepository>();
builder.Services.AddSingleton<MovementLogRepository>();
builder.Services.AddSingleton<AuditLogRepository>();
builder.Services.AddHostedService<ActiveDevicePruningService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

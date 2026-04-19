using VKFoodAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<PoiRepository>();
builder.Services.AddSingleton<AudioGuideRepository>();
builder.Services.AddSingleton<TourRepository>();
builder.Services.AddSingleton<ListeningHistoryRepository>();
builder.Services.AddSingleton<UserManagementRepository>();
builder.Services.AddSingleton<ActiveDeviceRepository>();
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

app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();

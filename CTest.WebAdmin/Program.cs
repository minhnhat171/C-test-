using CTest.WebAdmin.Services;

var builder = WebApplication.CreateBuilder(args);
var poiApiBaseUrl = builder.Configuration["PoiApi:BaseUrl"] ?? "http://localhost:5287/";

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<AppDataService>();
builder.Services.AddHttpClient<PoiApiClient>(client =>
{
    client.BaseAddress = new Uri(poiApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

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

using Microsoft.AspNetCore.Mvc;

namespace CTest.WebAdmin.Controllers;

public class DownloadController : Controller
{
    private readonly IWebHostEnvironment _env;
    private const string ApkFileName = "com.companyname.vkfoodarea-Signed.apk";

    public DownloadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("/download-app")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("/download-apk")]
    public IActionResult Apk()
    {
        var apkPath = Path.Combine(_env.WebRootPath, "apk", ApkFileName);

        if (!System.IO.File.Exists(apkPath))
        {
            return NotFound("Không tìm thấy file APK.");
        }

        return PhysicalFile(
            apkPath,
            "application/vnd.android.package-archive",
            ApkFileName);
    }
}
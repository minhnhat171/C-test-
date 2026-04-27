namespace VKFoodAPI.Services;

internal static class AppDataPathResolver
{
    public static string GetDataDirectory(IHostEnvironment environment)
    {
        var projectDirectory = FindProjectDirectory(environment.ContentRootPath);
        var dataDirectory = Path.Combine(projectDirectory ?? environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        return dataDirectory;
    }

    private static string? FindProjectDirectory(string contentRootPath)
    {
        var currentDirectory = new DirectoryInfo(contentRootPath);
        while (currentDirectory is not null)
        {
            var projectFilePath = Path.Combine(currentDirectory.FullName, "VKFoodAPI.csproj");
            if (File.Exists(projectFilePath))
            {
                return currentDirectory.FullName;
            }

            var siblingProjectFilePath = Path.Combine(currentDirectory.FullName, "VKFoodAPI", "VKFoodAPI.csproj");
            if (File.Exists(siblingProjectFilePath))
            {
                return Path.GetDirectoryName(siblingProjectFilePath);
            }

            currentDirectory = currentDirectory.Parent;
        }

        return null;
    }
}

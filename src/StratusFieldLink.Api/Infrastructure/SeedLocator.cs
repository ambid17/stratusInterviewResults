namespace StratusFieldLink.Api.Infrastructure;

public static class SeedLocator
{
    public const string RelativeSeedPath = "src/Data/sketches.seed.json";

    /// <summary>
    /// Walks up from the running assembly looking for the repo's seed file.
    /// Override with the STRATUS_SEED_PATH environment variable.
    /// </summary>
    public static string Locate()
    {
        var explicitPath = Environment.GetEnvironmentVariable("STRATUS_SEED_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, RelativeSeedPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, RelativeSeedPath);
    }
}

using System.IO.Compression;

namespace Taiwu.WorkshopPublisher.Cli;

internal static class WorkshopContentPreparer
{
    public static PreparedWorkshopContent Prepare(
        string releaseAssetDirectory,
        string extractDirectory,
        string? githubOutput)
    {
        string normalizedReleaseAssetDirectory = Path.GetFullPath(releaseAssetDirectory);
        string normalizedExtractDirectory = Path.GetFullPath(extractDirectory);

        string archivePath = FindSingleZipArchive(normalizedReleaseAssetDirectory);
        EnsureEmptyDirectory(normalizedExtractDirectory);

        ZipFile.ExtractToDirectory(archivePath, normalizedExtractDirectory);

        string contentDirectory = FindSingleContentDirectory(normalizedExtractDirectory);
        PreparedWorkshopContent content = new(contentDirectory);

        if (!string.IsNullOrWhiteSpace(githubOutput))
        {
            content.WriteGitHubOutputs(githubOutput);
        }

        Console.WriteLine($"Prepared workshop content: {contentDirectory}");
        return content;
    }

    private static string FindSingleZipArchive(string releaseAssetDirectory)
    {
        if (!Directory.Exists(releaseAssetDirectory))
        {
            throw new CliException($"Release asset directory does not exist: {releaseAssetDirectory}");
        }

        string[] archives = Directory.GetFiles(releaseAssetDirectory, "*.zip", SearchOption.TopDirectoryOnly);

        return archives.Length switch
        {
            1 => archives[0],
            0 => throw new CliException($"No zip release asset was found in {releaseAssetDirectory}."),
            _ => throw new CliException($"Expected exactly one zip release asset in {releaseAssetDirectory}, found {archives.Length}."),
        };
    }

    private static void EnsureEmptyDirectory(string directory)
    {
        if (Directory.Exists(directory)
            && Directory.EnumerateFileSystemEntries(directory).Any())
        {
            throw new CliException($"Workshop content directory must be empty before extraction: {directory}");
        }

        _ = Directory.CreateDirectory(directory);
    }

    private static string FindSingleContentDirectory(string extractDirectory)
    {
        string[] configPaths =
        [
            .. Directory
            .GetFiles(extractDirectory, "Config.Lua", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal),
        ];

        return configPaths.Length switch
        {
            1 => Path.GetDirectoryName(configPaths[0])
                ?? throw new CliException($"Could not resolve content directory for {configPaths[0]}."),
            0 => throw new CliException($"Extracted release asset does not contain Config.Lua under {extractDirectory}."),
            _ => throw new CliException(
                $"Extracted release asset contains multiple Config.Lua files under {extractDirectory}; content directory is ambiguous."),
        };
    }
}

internal sealed record PreparedWorkshopContent(string ContentDirectory)
{
    public void WriteGitHubOutputs(string path)
    {
        GitHubOutputFile.Append(
            path,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["content_dir"] = ContentDirectory,
            });
    }
}

using System.Globalization;

namespace Taiwu.WorkshopPublisher.Cli;

internal sealed record PublishTarget(
    string ModId,
    ulong FileId,
    string ReleaseRepository,
    string ReleaseTag,
    string ReleaseAssetDirectory,
    string ExtractDirectory)
{
    public void PrepareArtifacts()
    {
        _ = Directory.CreateDirectory(ReleaseAssetDirectory);
        _ = Directory.CreateDirectory(ExtractDirectory);
    }

    public void WriteGitHubOutputs(string path)
    {
        GitHubOutputFile.Append(
            path,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["mod_id"] = ModId,
                ["file_id"] = FileId.ToString(CultureInfo.InvariantCulture),
                ["release_repository"] = ReleaseRepository,
                ["release_tag"] = ReleaseTag,
                ["release_asset_dir"] = ReleaseAssetDirectory,
                ["extract_dir"] = ExtractDirectory,
            });
    }
}

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Taiwu.WorkshopPublisher.Cli;

internal sealed record PublishMatrixOutput(IReadOnlyList<PublishMatrixEntry> Entries)
{
    public static PublishMatrixOutput Create(
        PublishManifest manifest,
        PublishManifest? previousManifest)
    {
        IReadOnlyList<PublishSelection> selections = previousManifest is null
            ? manifest.GetSelections()
            : PublishSelectionDiff.GetChanged(manifest, previousManifest);
        List<PublishMatrixEntry> entries = [];

        foreach (PublishSelection selection in selections)
        {
            entries.Add(new PublishMatrixEntry(selection.Id));
        }

        return new PublishMatrixOutput(entries);
    }

    public void WriteGitHubOutputs(string path)
    {
        GitHubOutputFile.Append(
            path,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["count"] = Entries.Count.ToString(CultureInfo.InvariantCulture),
                ["matrix"] = JsonSerializer.Serialize(new GitHubMatrix(Entries)),
            });
    }
}

internal sealed record GitHubMatrix([property: JsonPropertyName("include")] IReadOnlyList<PublishMatrixEntry> Include);

internal sealed record PublishMatrixEntry([property: JsonPropertyName("mod_id")] string ModId);

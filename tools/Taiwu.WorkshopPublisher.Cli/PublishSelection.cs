namespace Taiwu.WorkshopPublisher.Cli;

internal sealed record PublishSelection(
    string Id,
    ulong FileId,
    string ReleaseRepository,
    string ReleaseTag);

internal static class PublishSelectionDiff
{
    public static IReadOnlyList<PublishSelection> GetChanged(
        PublishManifest current,
        PublishManifest previous)
    {
        Dictionary<string, PublishSelection> previousSelections = previous.GetSelections()
            .ToDictionary(static selection => selection.Id, StringComparer.Ordinal);
        List<PublishSelection> changedSelections = [];

        foreach (PublishSelection currentSelection in current.GetSelections())
        {
            if (!previousSelections.TryGetValue(currentSelection.Id, out PublishSelection? previousSelection)
                || currentSelection != previousSelection)
            {
                changedSelections.Add(currentSelection);
            }
        }

        return changedSelections;
    }
}

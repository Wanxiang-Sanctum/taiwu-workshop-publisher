using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Taiwu.WorkshopPublisher.Cli;

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "YamlDotNet creates publish manifest DTOs through reflection.")]
internal sealed class PublishManifest
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithDuplicateKeyChecking()
        .Build();

    public string? Repository { get; set; }

    public Dictionary<string, PublishManifestMod>? Mods { get; set; }

    public static PublishManifest Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new CliException($"Publish manifest was not found: {path}");
        }

        try
        {
            PublishManifest manifest = Deserializer.Deserialize<PublishManifest>(File.ReadAllText(path))
                ?? throw new CliException($"Publish manifest is empty: {path}");
            manifest.Validate();
            return manifest;
        }
        catch (YamlException exception)
        {
            throw new CliException($"Failed to parse publish manifest {path}: {exception.Message}", exception);
        }
    }

    public PublishTarget ResolveTarget(
        string modId,
        string artifactsRoot)
    {
        ValidateModId(modId, "mod id");

        Dictionary<string, PublishManifestMod> mods = GetMods();
        PublishManifestMod mod = mods.GetValueOrDefault(modId)
            ?? throw new CliException(
                $"Publish manifest does not contain mod id '{modId}'. Known mods: {GetKnownMods()}");
        PublishSelection selection = CreateSelection(modId, mod);

        string releaseAssetDirectory = Path.Combine(artifactsRoot, "release-assets");
        string extractDirectory = Path.Combine(artifactsRoot, "extracted-release");

        return new PublishTarget(
            selection.Id,
            selection.FileId,
            selection.ReleaseRepository,
            selection.ReleaseTag,
            releaseAssetDirectory,
            extractDirectory);
    }

    internal IReadOnlyList<PublishSelection> GetSelections()
    {
        List<PublishSelection> selections = [];

        foreach (KeyValuePair<string, PublishManifestMod> mod in GetMods())
        {
            selections.Add(CreateSelection(mod.Key, mod.Value));
        }

        return [.. selections.OrderBy(static selection => selection.Id, StringComparer.Ordinal)];
    }

    private void Validate()
    {
        string? defaultRepository = NormalizeOptional(Repository);

        if (defaultRepository is not null)
        {
            ValidateGitHubRepository(defaultRepository, "repository");
        }

        if (Mods is null)
        {
            throw new CliException("Publish manifest must define mods as a mapping.");
        }

        foreach (KeyValuePair<string, PublishManifestMod> mod in Mods)
        {
            string modId = mod.Key;
            ValidateModId(modId, "mod id");

            PublishManifestMod modValue = mod.Value
                ?? throw new CliException($"Mod '{modId}' must be a mapping.");
            string releaseRepository = ResolveReleaseRepository(modValue)
                ?? throw new CliException($"Mod '{modId}' must define repository because no top-level repository is set.");

            ValidateGitHubRepository(releaseRepository, $"mod '{modId}' repository");
            ValidateFileId(modValue.FileId, $"mod '{modId}' fileId");
            ValidateReleaseTag(modValue.Tag, $"mod '{modId}' tag");
        }
    }

    private string GetKnownMods()
    {
        Dictionary<string, PublishManifestMod> mods = GetMods();

        if (mods.Count == 0)
        {
            return "(none)";
        }

        return string.Join(
            ", ",
            mods.Keys.OrderBy(static id => id, StringComparer.Ordinal));
    }

    private Dictionary<string, PublishManifestMod> GetMods()
    {
        return Mods ?? throw new InvalidOperationException("Validated publish manifest has no mods mapping.");
    }

    private string? ResolveReleaseRepository(PublishManifestMod mod)
    {
        return NormalizeOptional(mod.Repository) ?? NormalizeOptional(Repository);
    }

    private PublishSelection CreateSelection(string modId, PublishManifestMod mod)
    {
        return new PublishSelection(
            modId,
            mod.FileId ?? throw new InvalidOperationException("Validated publish manifest mod has no file id."),
            ResolveReleaseRepository(mod) ?? throw new InvalidOperationException(
                "Validated publish manifest mod has no release repository."),
            mod.Tag);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static void ValidateGitHubRepository(string repository, string fieldName)
    {
        ValidateSingleLine(repository, fieldName);
        string[] parts = repository.Split('/');

        if (parts.Length != 2
            || parts.Any(static part => string.IsNullOrWhiteSpace(part)))
        {
            throw new CliException($"{fieldName} must use the owner/repository form.");
        }
    }

    private static void ValidateReleaseTag(string tag, string fieldName)
    {
        ValidateSingleLine(tag, fieldName);

        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new CliException($"{fieldName} cannot be empty.");
        }
    }

    private static void ValidateFileId(ulong? fileId, string fieldName)
    {
        if (fileId is null)
        {
            throw new CliException($"{fieldName} is required.");
        }

        if (fileId == 0)
        {
            throw new CliException($"{fieldName} must be non-zero because creating new Workshop items is not supported.");
        }
    }

    private static void ValidateModId(string modId, string fieldName)
    {
        ValidateSingleLine(modId, fieldName);

        if (string.IsNullOrWhiteSpace(modId))
        {
            throw new CliException($"{fieldName} cannot be empty.");
        }
    }

    private static void ValidateSingleLine(string value, string fieldName)
    {
        if (value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal))
        {
            throw new CliException($"{fieldName} cannot contain line breaks.");
        }
    }
}

[SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "YamlDotNet creates publish manifest mod entries through reflection.")]
internal sealed class PublishManifestMod
{
    public ulong? FileId { get; set; }

    public string? Repository { get; set; }

    public string Tag { get; set; } = string.Empty;
}

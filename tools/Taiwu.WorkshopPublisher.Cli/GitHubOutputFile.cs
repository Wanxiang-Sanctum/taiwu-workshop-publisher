using System.Text;

namespace Taiwu.WorkshopPublisher.Cli;

internal static class GitHubOutputFile
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public static void Append(string path, IReadOnlyDictionary<string, string> values)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        using StreamWriter writer = new(path, append: true, Utf8NoBom);

        foreach (KeyValuePair<string, string> value in values)
        {
            WriteOutput(writer, value.Key, value.Value);
        }
    }

    private static void WriteOutput(TextWriter writer, string name, string value)
    {
        if (value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal))
        {
            throw new CliException($"GitHub output value for '{name}' cannot contain newlines.");
        }

        writer.WriteLine($"{name}={value}");
    }
}

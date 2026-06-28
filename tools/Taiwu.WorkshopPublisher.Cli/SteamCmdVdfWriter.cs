using System.Globalization;
using System.Text;

namespace Taiwu.WorkshopPublisher.Cli;

internal static class SteamCmdVdfWriter
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public static void Write(string path, WorkshopItem item)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Build(item), Utf8NoBom);
    }

    public static string Build(WorkshopItem item)
    {
        StringBuilder builder = new();
        _ = builder
            .AppendLine("\"workshopitem\"")
            .AppendLine("{");
        Append(builder, "appid", item.AppId.ToString(CultureInfo.InvariantCulture));
        Append(builder, "publishedfileid", item.PublishedFileId.ToString(CultureInfo.InvariantCulture));
        Append(builder, "contentfolder", item.ContentFolder);

        if (item.PreviewFile is not null)
        {
            Append(builder, "previewfile", item.PreviewFile);
        }

        if (item.Visibility is not null)
        {
            Append(builder, "visibility", item.Visibility.Value.ToString(CultureInfo.InvariantCulture));
        }

        Append(builder, "title", item.Title);
        Append(builder, "description", item.Description);

        if (!string.IsNullOrWhiteSpace(item.ChangeNote))
        {
            Append(builder, "changenote", item.ChangeNote);
        }

        _ = builder.AppendLine("}");
        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string key, string value)
    {
        _ = builder
            .Append("  \"")
            .Append(Escape(key))
            .Append("\" \"")
            .Append(Escape(value))
            .AppendLine("\"");
    }

    private static string Escape(string value)
    {
        StringBuilder builder = new(value.Length);

        foreach (char current in value)
        {
            _ = builder.Append(current switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ => current.ToString(),
            });
        }

        return builder.ToString();
    }
}

using System.Globalization;

namespace Taiwu.WorkshopPublisher.Cli;

internal sealed record TaiwuModConfig(
    string Title,
    ulong FileId,
    string? Description,
    string? ChangeNote,
    string? Cover,
    string? WorkshopCover,
    int? Visibility)
{
    private static readonly HashSet<string> FieldsToRead = new(StringComparer.Ordinal)
    {
        "Title",
        "FileId",
        "Description",
        "ChangeLog",
        "Cover",
        "WorkshopCover",
        "Visibility",
    };

    public static TaiwuModConfig Load(string contentDirectory)
    {
        string configPath = Path.Combine(contentDirectory, "Config.Lua");

        if (!File.Exists(configPath))
        {
            throw new CliException($"Config.Lua was not found: {configPath}");
        }

        LuaTable table = LuaConfigReader.ReadConfig(File.ReadAllText(configPath), configPath, FieldsToRead);
        string title = GetRequiredString(table, "Title");
        ulong fileId = GetRequiredUInt64(table, "FileId");

        return new TaiwuModConfig(
            title,
            fileId,
            GetOptionalString(table, "Description"),
            GetOptionalString(table, "ChangeLog"),
            GetOptionalString(table, "Cover"),
            GetOptionalString(table, "WorkshopCover"),
            GetOptionalInt32(table, "Visibility"));
    }

    private static string GetRequiredString(LuaTable table, string key)
    {
        return GetOptionalString(table, key)
            ?? throw new CliException($"Config.Lua is missing required string field: {key}");
    }

    private static ulong GetRequiredUInt64(LuaTable table, string key)
    {
        return GetOptionalUInt64(table, key)
            ?? throw new CliException($"Config.Lua is missing required integer field: {key}");
    }

    private static string? GetOptionalString(LuaTable table, string key)
    {
        if (!table.Fields.TryGetValue(key, out LuaValue? value) || value is LuaNil)
        {
            return null;
        }

        return value is LuaString luaString
            ? luaString.Value
            : throw new CliException($"Config.Lua field {key} must be a string.");
    }

    private static int? GetOptionalInt32(LuaTable table, string key)
    {
        ulong? value = GetOptionalUInt64(table, key);

        if (value is null)
        {
            return null;
        }

        if (value > int.MaxValue)
        {
            throw new CliException($"Config.Lua field {key} is too large.");
        }

        return (int)value.Value;
    }

    private static ulong? GetOptionalUInt64(LuaTable table, string key)
    {
        if (!table.Fields.TryGetValue(key, out LuaValue? value) || value is LuaNil)
        {
            return null;
        }

        return ToUInt64(value, key);
    }

    private static ulong ToUInt64(LuaValue value, string fieldName)
    {
        return value switch
        {
            LuaNumber luaNumber when luaNumber.Value >= 0 && decimal.Truncate(luaNumber.Value) == luaNumber.Value
                => decimal.ToUInt64(luaNumber.Value),
            LuaString luaString when ulong.TryParse(
                luaString.Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out ulong result)
                => result,
            _ => throw new CliException($"Config.Lua field {fieldName} must be an unsigned integer."),
        };
    }
}

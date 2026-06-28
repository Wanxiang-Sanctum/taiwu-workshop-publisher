namespace Taiwu.WorkshopPublisher.Cli;

internal static class WorkshopContentValidator
{
    public static ValidationResult Validate(PublishRequest request)
    {
        List<ValidationIssue> issues = [];
        WorkshopItem item = request.WorkshopItem;

        string settingsPath = Path.Combine(item.ContentFolder, "Settings.Lua");

        if (!request.AllowSettingsLua && File.Exists(settingsPath))
        {
            issues.Add(Error(
                "Settings.Lua is present in the content folder. "
                    + "Remove local player settings or pass --allow-settings explicitly."));
        }

        if (item.AppId <= 0)
        {
            issues.Add(Error("Steam app id must be positive."));
        }

        if (item.PublishedFileId == 0)
        {
            issues.Add(Error("Publish manifest fileId must be non-zero because creating new Workshop items is not supported."));
        }

        if (request.ConfigPublishedFileId == 0)
        {
            issues.Add(Error("Config.Lua FileId must be non-zero because creating new Workshop items is not supported."));
        }
        else if (item.PublishedFileId != 0
            && item.PublishedFileId != request.ConfigPublishedFileId)
        {
            issues.Add(Error(
                $"Publish manifest fileId {item.PublishedFileId} does not match Config.Lua FileId {request.ConfigPublishedFileId}."));
        }

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            issues.Add(Error("Workshop title cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(item.Description))
        {
            issues.Add(Warning("Workshop description is empty."));
        }

        if (item.PreviewFile is not null && !File.Exists(item.PreviewFile))
        {
            issues.Add(Error($"Preview file does not exist: {item.PreviewFile}"));
        }

        if (item.Visibility is < 0 or > 3)
        {
            issues.Add(Error("Workshop visibility must be 0, 1, 2, or 3."));
        }

        return new ValidationResult(issues);
    }

    private static ValidationIssue Error(string message)
    {
        return new ValidationIssue(ValidationSeverity.Error, message);
    }

    private static ValidationIssue Warning(string message)
    {
        return new ValidationIssue(ValidationSeverity.Warning, message);
    }
}

internal sealed record ValidationResult(IReadOnlyList<ValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(static issue => issue.Severity == ValidationSeverity.Error);
}

internal sealed record ValidationIssue(ValidationSeverity Severity, string Message);

internal enum ValidationSeverity
{
    Warning = 0,
    Error = 1,
}

namespace Taiwu.WorkshopPublisher.Cli;

internal sealed record WorkshopItem(
    int AppId,
    ulong PublishedFileId,
    string ContentFolder,
    string? PreviewFile,
    int? Visibility,
    string Title,
    string Description,
    string? ChangeNote);

internal sealed record PublishRequest(
    WorkshopItem WorkshopItem,
    ulong ConfigPublishedFileId,
    bool AllowSettingsLua);

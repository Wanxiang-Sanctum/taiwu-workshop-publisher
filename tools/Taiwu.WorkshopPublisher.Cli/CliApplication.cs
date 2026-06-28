using System.CommandLine;
using System.CommandLine.Help;
using CliWrap.Exceptions;

namespace Taiwu.WorkshopPublisher.Cli;

internal static class CliApplication
{
    private const int TaiwuSteamAppId = 838350;
    private const string DefaultManifestFile = "publishing/workshop.yml";
    private const string DefaultArtifactsRoot = "artifacts";

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            Command command = CreateCommand();
            return await command.Parse(args)
                .InvokeAsync(CreateInvocationConfiguration());
        }
        catch (OperationCanceledException)
        {
            return 130;
        }
        catch (CliException exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            return 2;
        }
        catch (Exception exception) when (ShouldReportError(exception))
        {
            await Console.Error.WriteLineAsync(exception.Message);
            return 1;
        }
    }

    private static InvocationConfiguration CreateInvocationConfiguration()
    {
        return new InvocationConfiguration
        {
            EnableDefaultExceptionHandler = false,
        };
    }

    private static bool ShouldReportError(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or CommandExecutionException;
    }

    private static Command CreateCommand()
    {
        Command command = new(
            "taiwu-workshop-publisher",
            "Publish Taiwu mod release assets to Steam Workshop.");

        command.Options.Add(new HelpOption { Recursive = true });
        command.Subcommands.Add(CreateBuildMatrixCommand());
        command.Subcommands.Add(CreateResolveTargetCommand());
        command.Subcommands.Add(CreatePrepareContentCommand());
        command.Subcommands.Add(CreateValidateCommand());
        command.Subcommands.Add(CreateVdfCommand());
        command.Subcommands.Add(CreatePublishCommand());

        return command;
    }

    private static Command CreateBuildMatrixCommand()
    {
        Option<string> manifestOption = CreateManifestOption();
        Option<string?> previousManifestOption = new("--previous-manifest")
        {
            Description = "Previous YAML publish manifest path used to include only changed manifest entries.",
            HelpName = "path",
        };
        Option<string?> githubOutputOption = new("--github-output")
        {
            Description = "Append matrix values to this GitHub Actions output file.",
            HelpName = "path",
        };
        Command command = new("build-matrix", "Build a GitHub Actions matrix from a YAML publish manifest.");

        command.Options.Add(manifestOption);
        command.Options.Add(previousManifestOption);
        command.Options.Add(githubOutputOption);
        command.SetAction(parseResult => RunBuildMatrix(
            parseResult.GetRequiredValue(manifestOption),
            parseResult.GetValue(previousManifestOption),
            parseResult.GetValue(githubOutputOption)));

        return command;
    }

    private static Command CreateResolveTargetCommand()
    {
        Option<string> manifestOption = CreateManifestOption();
        Option<string> modIdOption = new("--mod-id")
        {
            Description = "Mod id to resolve from the publish manifest.",
            HelpName = "id",
            Required = true,
        };
        Option<string> artifactsRootOption = new("--artifacts-root")
        {
            Description = "Artifacts root used by the publishing workflow.",
            HelpName = "path",
            DefaultValueFactory = _ => DefaultArtifactsRoot,
        };
        Option<string?> githubOutputOption = new("--github-output")
        {
            Description = "Append resolved values to this GitHub Actions output file.",
            HelpName = "path",
        };
        Command command = new("resolve-target", "Resolve one publish manifest target for CI.");

        command.Options.Add(manifestOption);
        command.Options.Add(modIdOption);
        command.Options.Add(artifactsRootOption);
        command.Options.Add(githubOutputOption);
        command.SetAction(parseResult => RunResolveTarget(
            parseResult.GetRequiredValue(manifestOption),
            parseResult.GetRequiredValue(modIdOption),
            parseResult.GetRequiredValue(artifactsRootOption),
            parseResult.GetValue(githubOutputOption)));

        return command;
    }

    private static Command CreatePrepareContentCommand()
    {
        Option<string> releaseAssetDirectoryOption = new("--release-asset-dir")
        {
            Description = "Directory containing the downloaded release asset.",
            HelpName = "path",
            Required = true,
        };
        Option<string> extractDirectoryOption = new("--extract-dir")
        {
            Description = "Empty directory where the release asset is extracted.",
            HelpName = "path",
            Required = true,
        };
        Option<string?> githubOutputOption = new("--github-output")
        {
            Description = "Append prepared content values to this GitHub Actions output file.",
            HelpName = "path",
        };
        Command command = new("prepare-content", "Extract a release asset and locate the packed Taiwu mod directory.");

        command.Options.Add(releaseAssetDirectoryOption);
        command.Options.Add(extractDirectoryOption);
        command.Options.Add(githubOutputOption);
        command.SetAction(parseResult => RunPrepareContent(
            parseResult.GetRequiredValue(releaseAssetDirectoryOption),
            parseResult.GetRequiredValue(extractDirectoryOption),
            parseResult.GetValue(githubOutputOption)));

        return command;
    }

    private static Option<string> CreateManifestOption()
    {
        return new Option<string>("--manifest")
        {
            Description = "YAML publish manifest path.",
            HelpName = "path",
            DefaultValueFactory = _ => DefaultManifestFile,
        };
    }

    private static Command CreateValidateCommand()
    {
        Command command = new("validate", "Validate a packed Taiwu mod directory before publishing.");
        PublishOptionSet publishOptions = AddPublishOptions(command);

        command.SetAction(parseResult => RunValidate(ReadPublishOptions(parseResult, publishOptions)));

        return command;
    }

    private static Command CreateVdfCommand()
    {
        Command command = new("vdf", "Generate a SteamCMD workshop item VDF.");
        PublishOptionSet publishOptions = AddPublishOptions(command);
        Option<string> outputOption = CreateOutputOption(required: true);

        command.Options.Add(outputOption);
        command.SetAction(parseResult => RunVdf(
            ReadPublishOptions(parseResult, publishOptions),
            parseResult.GetRequiredValue(outputOption)));

        return command;
    }

    private static Command CreatePublishCommand()
    {
        Command command = new("publish", "Call SteamCMD with an existing workshop item VDF.");
        Option<string> vdfOption = new("--vdf")
        {
            Description = "SteamCMD workshop item VDF path.",
            HelpName = "path",
            Required = true,
        };
        Option<string> steamCmdOption = new("--steamcmd")
        {
            Description = "SteamCMD executable or script.",
            HelpName = "path",
            DefaultValueFactory = _ => "steamcmd",
        };
        Option<string?> steamHomeOption = new("--steam-home")
        {
            Description = "SteamCMD HOME directory containing trusted session state.",
            HelpName = "path",
        };

        command.Options.Add(vdfOption);
        command.Options.Add(steamCmdOption);
        command.Options.Add(steamHomeOption);
        command.SetAction((parseResult, cancellationToken) => RunPublishAsync(
            parseResult.GetRequiredValue(vdfOption),
            parseResult.GetRequiredValue(steamCmdOption),
            parseResult.GetValue(steamHomeOption),
            cancellationToken));

        return command;
    }

    private static PublishOptionSet AddPublishOptions(Command command)
    {
        Option<string> contentDirectoryOption = new("--content-dir")
        {
            Description = "Packed mod directory containing Config.Lua.",
            HelpName = "path",
            Required = true,
        };
        Option<int> appIdOption = new("--app-id")
        {
            Description = "Steam app id.",
            HelpName = "number",
            DefaultValueFactory = _ => TaiwuSteamAppId,
        };
        Option<ulong> fileIdOption = new("--file-id")
        {
            Description = "Expected Steam Workshop published file id from the publish manifest.",
            HelpName = "number",
            Required = true,
        };
        Option<bool> allowSettingsOption = new("--allow-settings")
        {
            Description = "Allow Settings.Lua in the content directory.",
        };

        command.Options.Add(contentDirectoryOption);
        command.Options.Add(appIdOption);
        command.Options.Add(fileIdOption);
        command.Options.Add(allowSettingsOption);

        return new PublishOptionSet(
            contentDirectoryOption,
            appIdOption,
            fileIdOption,
            allowSettingsOption);
    }

    private static PublishOptions ReadPublishOptions(ParseResult parseResult, PublishOptionSet options)
    {
        return new PublishOptions(
            parseResult.GetRequiredValue(options.ContentDirectory),
            parseResult.GetRequiredValue(options.AppId),
            parseResult.GetRequiredValue(options.FileId),
            parseResult.GetValue(options.AllowSettings));
    }

    private static Option<string> CreateOutputOption(bool required)
    {
        return new Option<string>("--output")
        {
            Description = "VDF output path.",
            HelpName = "path",
            Required = required,
        };
    }

    private static int RunResolveTarget(
        string manifestPath,
        string modId,
        string artifactsRoot,
        string? githubOutput)
    {
        PublishManifest manifest = PublishManifest.Load(manifestPath);
        PublishTarget target = manifest.ResolveTarget(modId, artifactsRoot);

        target.PrepareArtifacts();

        if (!string.IsNullOrWhiteSpace(githubOutput))
        {
            target.WriteGitHubOutputs(githubOutput);
        }

        Console.WriteLine($"Resolved {target.ModId} from {target.ReleaseRepository}.");
        Console.WriteLine($"File id: {target.FileId}");
        Console.WriteLine($"Release tag: {target.ReleaseTag}");
        Console.WriteLine($"Release asset directory: {target.ReleaseAssetDirectory}");
        Console.WriteLine($"Extract directory: {target.ExtractDirectory}");
        return 0;
    }

    private static int RunBuildMatrix(
        string manifestPath,
        string? previousManifestPath,
        string? githubOutput)
    {
        PublishManifest manifest = PublishManifest.Load(manifestPath);
        PublishManifest? previousManifest = string.IsNullOrWhiteSpace(previousManifestPath)
            ? null
            : PublishManifest.Load(previousManifestPath);
        PublishMatrixOutput matrix = PublishMatrixOutput.Create(manifest, previousManifest);

        if (!string.IsNullOrWhiteSpace(githubOutput))
        {
            matrix.WriteGitHubOutputs(githubOutput);
        }

        Console.WriteLine($"Selected {matrix.Entries.Count} manifest entr{(matrix.Entries.Count == 1 ? "y" : "ies")} for publishing.");
        return 0;
    }

    private static int RunPrepareContent(
        string releaseAssetDirectory,
        string extractDirectory,
        string? githubOutput)
    {
        _ = WorkshopContentPreparer.Prepare(
            releaseAssetDirectory,
            extractDirectory,
            githubOutput);

        return 0;
    }

    private static int RunValidate(PublishOptions options)
    {
        PublishRequest request = CreatePublishRequest(options);
        ValidationResult validation = WorkshopContentValidator.Validate(request);
        WriteValidation(validation);
        return validation.HasErrors ? 1 : 0;
    }

    private static int RunVdf(PublishOptions options, string outputPath)
    {
        PublishRequest request = CreatePublishRequest(options);
        ValidationResult validation = WorkshopContentValidator.Validate(request);

        if (validation.HasErrors)
        {
            WriteValidation(validation);
            return 1;
        }

        SteamCmdVdfWriter.Write(outputPath, request.WorkshopItem);
        WriteValidation(validation);
        Console.WriteLine($"Wrote SteamCMD VDF: {outputPath}");
        return 0;
    }

    private static async Task<int> RunPublishAsync(
        string vdfPath,
        string steamCmdPath,
        string? steamHomePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(vdfPath))
        {
            throw new CliException($"SteamCMD VDF was not found: {vdfPath}");
        }

        string normalizedVdfPath = Path.GetFullPath(vdfPath);
        SteamAccount account = SteamAccount.FromEnvironment();
        return await SteamCmdRunner.RunAsync(steamCmdPath, account, normalizedVdfPath, steamHomePath, cancellationToken);
    }

    private static PublishRequest CreatePublishRequest(PublishOptions options)
    {
        string contentDirectory = options.ContentDirectory;
        TaiwuModConfig config = TaiwuModConfig.Load(contentDirectory);

        string title = config.Title;
        string description = config.Description ?? string.Empty;
        string? changeNote = config.ChangeNote;
        int? visibility = config.Visibility;
        string? previewFile = ResolvePreviewFile(contentDirectory, config);

        WorkshopItem item = new(
            options.AppId,
            options.FileId,
            Path.GetFullPath(contentDirectory),
            previewFile,
            visibility,
            title,
            description,
            changeNote);

        return new PublishRequest(item, config.FileId, options.AllowSettings);
    }

    private static string? ResolvePreviewFile(
        string contentDirectory,
        TaiwuModConfig config)
    {
        string? previewFile = config.WorkshopCover ?? config.Cover;

        if (string.IsNullOrWhiteSpace(previewFile))
        {
            return null;
        }

        if (Path.IsPathRooted(previewFile))
        {
            return Path.GetFullPath(previewFile);
        }

        return Path.GetFullPath(Path.Combine(contentDirectory, previewFile));
    }

    private static void WriteValidation(ValidationResult validation)
    {
        foreach (ValidationIssue issue in validation.Issues)
        {
            TextWriter writer = issue.Severity == ValidationSeverity.Error
                ? Console.Error
                : Console.Out;
            writer.WriteLine($"{issue.Severity}: {issue.Message}");
        }

    }
}

internal sealed record PublishOptions(
    string ContentDirectory,
    int AppId,
    ulong FileId,
    bool AllowSettings);

internal sealed record PublishOptionSet(
    Option<string> ContentDirectory,
    Option<int> AppId,
    Option<ulong> FileId,
    Option<bool> AllowSettings);

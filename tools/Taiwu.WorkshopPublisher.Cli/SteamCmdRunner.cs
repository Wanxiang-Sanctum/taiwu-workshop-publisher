using CliWrap;

namespace Taiwu.WorkshopPublisher.Cli;

internal static class SteamCmdRunner
{
    public static async Task<int> RunAsync(
        string steamCmdPath,
        SteamAccount account,
        string vdfPath,
        string? steamHomePath,
        CancellationToken cancellationToken)
    {
        Command command = global::CliWrap.Cli.Wrap(steamCmdPath)
            .WithArguments(arguments =>
            {
                _ = arguments
                    .Add("+@NoPromptForPassword")
                    .Add("1")
                    .Add("+login")
                    .Add(account.Username);

                _ = arguments
                    .Add("+workshop_build_item")
                    .Add(vdfPath)
                    .Add("+quit");
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line => Console.Out.WriteLineAsync(line)))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line => Console.Error.WriteLineAsync(line)))
            .WithValidation(CommandResultValidation.None);

        if (!string.IsNullOrWhiteSpace(steamHomePath))
        {
            command = command.WithEnvironmentVariables(variables =>
                variables.Set("HOME", Path.GetFullPath(steamHomePath)));
        }

        CommandResult result = await command.ExecuteAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            await Console.Error.WriteLineAsync($"SteamCMD exited with code {result.ExitCode}.");
        }

        return result.ExitCode;
    }
}

internal sealed record SteamAccount(string Username)
{
    public static SteamAccount FromEnvironment()
    {
        string? username = Environment.GetEnvironmentVariable("STEAM_USERNAME");

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new CliException("STEAM_USERNAME is required for publish.");
        }

        return new SteamAccount(username);
    }
}

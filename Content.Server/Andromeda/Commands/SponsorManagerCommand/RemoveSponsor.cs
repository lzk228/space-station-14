using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Andromeda.AndromedaSponsorService;

namespace Content.Server.Andromeda.Commands.SponsorManagerCommand;

[AdminCommand(AdminFlags.Host)]
public sealed class RemoveSponsorCommand : IConsoleCommand
{
    [Dependency] private readonly AndromedaSponsorManager _sponsorManager = default!;

    public string Command => "removesponsor";
    public string Description => "Removes a sponsor from the list.";
    public string Help => $"Usage: {Command} <user_ID>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!Guid.TryParse(args[0], out var userId))
        {
            shell.WriteLine($"Invalid user ID: {args[0]}");
            return;
        }

        if (!_sponsorManager.IsSponsor(userId))
        {
            shell.WriteLine($"User {userId} is not a sponsor.");
            return;
        }

        _sponsorManager.RemoveSponsor(userId);
        shell.WriteLine($"Sponsor with user ID {userId} removed successfully.");
    }
}
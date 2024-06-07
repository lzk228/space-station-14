using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Server.Andromeda.AndromedaSponsorService;

namespace Content.Server.Andromeda.Commands.SponsorManagerCommand;

[AdminCommand(AdminFlags.Host)]
public sealed class AddSponsorCommand : IConsoleCommand
{
    [Dependency] private readonly AndromedaSponsorManager _sponsorManager = default!;

    public string Command => "addsponsor";
    public string Description => "Adds a sponsor by their user ID.";
    public string Help => $"Usage: {Command} <user ID> [allowedAntag] [OOC color]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!Guid.TryParse(args[0], out var userId))
        {
            shell.WriteLine($"Invalid user ID: {args[0]}");
            return;
        }

        bool allowedAntag = false;
        if (args.Length > 1)
        {
            if (!bool.TryParse(args[1], out allowedAntag))
            {
                shell.WriteLine($"Invalid allowedAntag value: {args[1]}");
                return;
            }
        }

        string color = "#FF0000";
        if (args.Length > 2)
        {
            color = args[2];

            if (!_sponsorManager.IsValidColor(args[2]))
            {
                shell.WriteLine($"Invalid color: {args[2]}");
                return;
            }
        }

        if (_sponsorManager.IsSponsor(userId))
        {
            _sponsorManager.SaveSponsors(userId, allowedAntag, color);
            shell.WriteLine($"Sponsor data for user {userId} updated successfully.");
        }
        else
        {
            _sponsorManager.AddSponsor(userId, allowedAntag, color);
            shell.WriteLine($"User {userId} added as a sponsor.");
        }
    }
}
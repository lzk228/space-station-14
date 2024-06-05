using Robust.Shared.Console;
using Content.Server.Andromeda.AndromedaSponsorService;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Network;
using Robust.Server.Player;

namespace Content.Server.Andromeda.Commands.SponsorManagerCommand;

[AdminCommand(AdminFlags.Admin)]
public sealed class ListActiveSponsorsCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AndromedaSponsorManager _sponsorManager = default!;

    public string Command => "listsponsors";
    public string Description => "Lists all active sponsors.";
    public string Help => $"Usage: {Command}";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var activeSponsors = new List<Guid>();

        foreach (var session in _playerManager.Sessions)
        {
            if (_sponsorManager.IsSponsor(session.UserId))
            {
                activeSponsors.Add(session.UserId);
            }
        }

        shell.WriteLine("Active sponsors:");
        foreach (var sponsor in activeSponsors)
        {
            var session = _playerManager.GetSessionById(new NetUserId(sponsor));
            shell.WriteLine($"- {session?.Name}");
        }
    }
}
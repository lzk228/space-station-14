using Content.Server.Chat.Managers;
//using Content.Server.Singularity.Events;
using Content.Shared.Mobs;
//using Content.Shared.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Server.Administration;
using Content.Server.Administration.Managers;

namespace Content.Server.Administration.Systems;

public sealed class AdminNotifySystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        //SubscribeLocalEvent<ContainmentFieldGeneratorComponent, EventHorizonConsumedEntityEvent>(OnSingularityConsumedEntity);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!TryComp(ev.Target, out ActorComponent? actorComponent))
            return;

        if (actorComponent.PlayerSession.AttachedEntity == null || ev.NewMobState == MobState.Alive)
            return;

        string message;
        if (ev.Origin == null)
        {
            message = Loc.GetString("notify-admin-mob-state-changed",
                ("target", ToPrettyString(ev.Target)),
                ("state", ev.NewMobState.ToString()));
        }
        else
        {
            message = Loc.GetString("notify-admin-mob-state-changed-by",
                ("target", ToPrettyString(ev.Target)),
                ("state", ev.NewMobState.ToString()),
                ("origin", ToPrettyString(ev.Origin.Value)));
        }

        _chatManager.SendAdminAlert(message);
        _audioSystem.PlayGlobal(new SoundPathSpecifier("/Audio/Andromeda/Effects/newplayerping.ogg"),
            Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), false,
            audioParams: new AudioParams { Volume = -5f });

    }

    //private void OnSingularityConsumedEntity(EntityUid uid, ContainmentFieldGeneratorComponent component, EventHorizonConsumedEntityEvent ev)
    //{
    //    _chatManager.SendAdminAlert(Loc.GetString("notify-admin-singulatity-breach", ("target", ToPrettyString(ev.Entity))));
    //}
}

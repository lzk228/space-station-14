using Content.Shared.Explosion.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Andromeda.Voomra.C4;

/// <summary>
///     Detonation control system C4 when trying to detach it
/// </summary>
/// /// <seealso cref="T:Content.Shared.Andromeda.Voomra.C4.C4DetonationByUnstickComponent"/>
public sealed class C4DetonationByUnstickSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<C4DetonationByUnstickComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<C4DetonationByUnstickComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<C4DetonationByUnstickComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnComponentInit(EntityUid uid, C4DetonationByUnstickComponent component, ComponentInit args)
    {
        component.Owner2 = uid;
    }

    private void OnComponentRemove(EntityUid uid, C4DetonationByUnstickComponent component, ComponentRemove args)
    {
        component.Owner2 = EntityUid.Invalid;
    }

    private void OnGetAltVerbs(EntityUid uid, C4DetonationByUnstickComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (HasComp<ActiveTimerTriggerComponent>(uid))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("verb-c4-detonation-by-unstick", ("status", component.Detonation
                ? Loc.GetString("verb-c4-detonation-by-unstick-status-on")
                : Loc.GetString("verb-c4-detonation-by-unstick-status-off"))),
            Act = () => DoAltVerbs(component)
        });
    }

    private void DoAltVerbs(C4DetonationByUnstickComponent component)
    {
        component.Detonation = !component.Detonation;
    }
}

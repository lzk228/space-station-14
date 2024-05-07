using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Verbs;

namespace Content.Shared.Andromeda.Voomra.C4;

/// <summary>
///     Система управления детонированием C4 при попытке открепить её
/// </summary>
/// /// <seealso cref="T:Content.Shared.Andromeda.Voomra.C4.C4DetonationByUnstickComponent"/>
public sealed class C4DetonationByUnstickSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    // [Dependency] private readonly ILogManager _log = default!;
    // private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        // _sawmill = _log.GetSawmill(GetType().Name);

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
        args.Verbs.Add(new AlternativeVerb
        {
            Text = $"Детонация при попытке снятия: {(component.Detonation ? "ВКЛ" : "ВЫКЛ")}",
            Act = () => DoAltVerbs(component, args.User)
        });
    }

    private void DoAltVerbs(C4DetonationByUnstickComponent component, EntityUid user)
    {
        component.Detonation = !component.Detonation;
        // _adminLogger.Add(LogType.Verb, verb.Impact, $"{ToPrettyString(user):user} {executionText} the [{verbText:verb}] verb targeting {ToPrettyString(target):target}");
        _adminLogger.Add(LogType.Verb, $"{ToPrettyString(user):user} {(component.Detonation ? "ВКЛЮЧИЛ" : "ВЫКЛЮЧИЛ")} детонацию С4 при её снятии");
    }
}

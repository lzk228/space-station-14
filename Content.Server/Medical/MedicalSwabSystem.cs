using System.Threading;
using Content.Server.Botany;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Disease.Components;
using Content.Server.DoAfter;
using Content.Server.Medical.Components;
using Content.Server.Popups;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;

namespace Content.Server.Medical;

public sealed class MedicalSwabSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly ReactiveSystem _reactiveSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MedicalSwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MedicalSwabComponent, SolutionChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<MedicalSwabComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TargetSwabSuccessfulEvent>(OnTargetSwabSuccessful);
        SubscribeLocalEvent<SwabCancelledEvent>(OnSwabCancelled);
    }

    private void OnAfterInteract(EntityUid uid, MedicalSwabComponent component, AfterInteractEvent args)
    {
        if (!component.IsSolutionAdded)
            return;

        if (!TryComp<SolutionContainerManagerComponent>(uid, out var solutionContainerManagerComponent))
            return;

        if (!solutionContainerManagerComponent.Solutions.TryGetValue("swab", out var solution))
            return;

        if (solution.Volume == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-swab-empty"), args.User, args.User);
            return;
        }

        component.CancelToken = new CancellationTokenSource();
        _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, component.SwabDelay, component.CancelToken.Token, target: args.Target)
        {
            BroadcastFinishedEvent = new TargetSwabSuccessfulEvent(args.User, args.Target, component, solution),
            BroadcastCancelledEvent = new SwabCancelledEvent(component),
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnStun = true,
            NeedHand = true
        });
    }

    private void OnSolutionChanged(EntityUid uid, MedicalSwabComponent component, SolutionChangedEvent args)
    {
        if (component.IsSolutionAdded)
            return;

        if (TryComp<BotanySwabComponent>(uid, out var botanySwabComponent))
        {
            RemComp(uid, botanySwabComponent);
        }

        if (TryComp<DiseaseSwabComponent>(uid, out var diseaseSwabComponent))
        {
            RemComp(uid, diseaseSwabComponent);
        }

        component.IsSolutionAdded = true;
    }

    private void OnExamined(EntityUid uid, MedicalSwabComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && component.IsSolutionAdded)
        {
            args.PushMarkup(Loc.GetString("medical-swab-used"));
        }
    }

    private void OnTargetSwabSuccessful(TargetSwabSuccessfulEvent ev)
    {
        if (ev.Target == null)
            return;

        _reactiveSystem.DoEntityReaction(ev.Target.Value, ev.SwabSolution, ReactionMethod.Touch);
        _solutionContainerSystem.RemoveAllSolution(ev.Swab.Owner, ev.SwabSolution);
        _popupSystem.PopupEntity(Loc.GetString("medical-swab-swabbed", ("target", Identity.Entity(ev.Target.Value, EntityManager))), ev.Target.Value, ev.User);
    }

    private void OnSwabCancelled(SwabCancelledEvent ev)
    {
        ev.Swab.CancelToken = null;
    }

    private sealed class SwabCancelledEvent : EntityEventArgs
    {
        public readonly MedicalSwabComponent Swab;
        public SwabCancelledEvent(MedicalSwabComponent swab)
        {
            Swab = swab;
        }
    }

    private sealed class TargetSwabSuccessfulEvent : EntityEventArgs
    {
        public EntityUid User { get; }
        public EntityUid? Target { get; }
        public MedicalSwabComponent Swab { get; }
        public Solution SwabSolution { get; }

        public TargetSwabSuccessfulEvent(EntityUid user, EntityUid? target, MedicalSwabComponent swab, Solution solution)
        {
            User = user;
            Target = target;
            Swab = swab;
            SwabSolution = solution;
        }
    }
}

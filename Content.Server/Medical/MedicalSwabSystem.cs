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
using Content.Shared.DoAfter;
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
        SubscribeLocalEvent<MedicalSwabComponent, DoAfterEvent<MedicalSwabData>>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, MedicalSwabComponent component, AfterInteractEvent args)
    {
        if (!component.IsSolutionAdded)
            return;

        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;

        if (solution.Volume == 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("medical-swab-empty"), args.User, args.User);
            return;
        }

        var swabData = new MedicalSwabData(solution);

        var doAfterEventArgs = new DoAfterEventArgs(args.User, component.SwabDelay, target: args.Target, used: uid)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnStun = true,
            NeedHand = true
        };

        _doAfterSystem.DoAfter(doAfterEventArgs, swabData);
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

    private void OnDoAfter(EntityUid uid, MedicalSwabComponent component, DoAfterEvent<MedicalSwabData> args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null || component.Deleted)
            return;

        _reactiveSystem.DoEntityReaction(args.Args.Target.Value, args.AdditionalData.SwabSolution, ReactionMethod.Touch);
        _solutionContainerSystem.RemoveAllSolution(uid, args.AdditionalData.SwabSolution);
        _popupSystem.PopupEntity(Loc.GetString("medical-swab-swabbed",
            ("target", Identity.Entity(args.Args.Target.Value, EntityManager))), args.Args.Target.Value, args.Args.User);
        args.Handled = true;
    }

    private record struct MedicalSwabData(Solution SwabSolution)
    {
        public readonly Solution SwabSolution = SwabSolution;
    }
}

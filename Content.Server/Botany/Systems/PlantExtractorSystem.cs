using Content.Server.Botany.Components;
using Content.Server.Construction;
using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Botany;
using Content.Shared.Chemistry.Components;
using System.Linq;
using Content.Server.Kitchen.Components;
using Robust.Shared.Audio;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Systems
{
    /// <summary>
    /// Contains all the server-side logic for PlantExtractors.
    /// <seealso cref="PlantExtractorComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class PlantExtractorSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PlantExtractorComponent, ComponentStartup>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<PlantExtractorComponent, SolutionChangedEvent>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<PlantExtractorComponent, BoundUIOpenedEvent>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<PlantExtractorComponent, EntInsertedIntoContainerMessage>((_, comp, _) => UpdateUiState(comp));
            SubscribeLocalEvent<PlantExtractorComponent, EntRemovedFromContainerMessage>((_, comp, _) => UpdateUiState(comp));

            SubscribeLocalEvent<PlantExtractorComponent, RefreshPartsEvent>(OnRefreshParts);
            SubscribeLocalEvent<PlantExtractorComponent, UpgradeExamineEvent>(OnUpgradeExamine);
            SubscribeLocalEvent<PlantExtractorComponent, InteractUsingEvent>(OnInteractUsing);

            SubscribeLocalEvent<PlantExtractorComponent, PlantExtractorSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<PlantExtractorComponent, PlantExtractorReagentAmountButtonMessage>(OnReagentButtonMessage);

            SubscribeLocalEvent<PlantExtractorComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);
            SubscribeLocalEvent<PlantExtractorComponent, PlantExtractorStartMessage>(OnStartMessage);
            SubscribeLocalEvent<PlantExtractorComponent, PlantExtractorEjectChamberAllMessage>(OnEjectChamberAllMessage);
            SubscribeLocalEvent<PlantExtractorComponent, PlantExtractorEjectChamberContentMessage>(OnEjectChamberContentMessage);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (activeComponent, component) in EntityQuery<ActivePlantExtractorComponent, PlantExtractorComponent>())
            {
                var uid = component.Owner;

                if (activeComponent.EndTime > _timing.CurTime)
                    continue;

                component.AudioStream?.Stop();
                RemCompDeferred<ActivePlantExtractorComponent>(uid);

                var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedPlantExtractor.InputContainerId);

                if (!_solutionContainerSystem.TryGetSolution(uid, SharedPlantExtractor.BufferId, out var bufferSolution))
                    continue;

                foreach (var item in inputContainer.ContainedEntities.ToList())
                {
                    var solution = GetExtractSolution(item);

                    if (solution is null)
                        continue;

                    if (TryComp<StackComponent>(item, out var stack))
                    {
                        solution.ScaleSolution(stack.Count);
                    }
                    QueueDel(item);

                    // _solutionContainerSystem.TryAddSolution(uid, bufferSolution, solution);
                    bufferSolution.AddSolution(solution, _prototypeManager);
                }

                UpdateUiState(component);
                _userInterfaceSystem.TrySendUiMessage(uid, PlantExtractorUiKey.Key, new PlantExtractorWorkCompletedMessage());
            }
        }

        #region UI
        private void UpdateUiState(PlantExtractorComponent component)
        {
            if (!_solutionContainerSystem.TryGetSolution(component.Owner, SharedPlantExtractor.BufferId, out var bufferSolution))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(component.Owner, SharedPlantExtractor.InputContainerId);
            var beakerContainer = _itemSlotsSystem.GetItemOrNull(component.Owner, SharedPlantExtractor.BeakerContainerId);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            var isBusy = HasComp<ActivePlantExtractorComponent>(component.Owner);
            var canExtract = false;

            if (inputContainer.ContainedEntities.Count > 0)
                canExtract = inputContainer.ContainedEntities.All(CanExtract);

            var state = new PlantExtractorBoundUserInterfaceState(
                component.Mode,
                BuildBeakerContainerInfo(beakerContainer),
                bufferReagents,
                bufferCurrentVolume,
                isBusy,
                canExtract,
                inputContainer.ContainedEntities.Select(item => item).ToArray());

            _userInterfaceSystem.TrySetUiState(component.Owner, PlantExtractorUiKey.Key, state);
        }
        #endregion

        #region Actions
        private void OnRefreshParts(EntityUid uid, PlantExtractorComponent component, RefreshPartsEvent args)
        {
            var ratingWorkTime = args.PartRatings[component.MachinePartWorkTime];
            var ratingStorage = args.PartRatings[component.MachinePartStorageMax];

            component.WorkTimeMultiplier = MathF.Pow(component.PartRatingWorkTimerMulitplier, ratingWorkTime - 1);
            component.StorageMaxEntities = component.BaseStorageMaxEntities + (int) (component.StoragePerPartRating * ratingStorage);
        }

        private void OnUpgradeExamine(EntityUid uid, PlantExtractorComponent component, UpgradeExamineEvent args)
        {
            args.AddPercentageUpgrade("plant-extractor-component-upgrade-work-time", component.WorkTimeMultiplier);
            args.AddNumberUpgrade("plant-extractor-component-upgrade-storage", component.StorageMaxEntities - component.BaseStorageMaxEntities);
        }

        private void OnSetModeMessage(EntityUid uid, PlantExtractorComponent component, PlantExtractorSetModeMessage message)
        {
            if (!Enum.IsDefined(typeof(PlantExtractorMode), message.PlantExtractorMode))
                return;

            component.Mode = message.PlantExtractorMode;
            UpdateUiState(component);
            ClickSound(component);
        }

        private void OnReagentButtonMessage(EntityUid uid, PlantExtractorComponent component, PlantExtractorReagentAmountButtonMessage message)
        {
            if (!Enum.IsDefined(typeof(PlantExtractorReagentAmount), message.Amount))
                return;

            switch (component.Mode)
            {
                case PlantExtractorMode.Transfer:
                    TransferReagents(component, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                case PlantExtractorMode.Discard:
                    DiscardReagents(component, message.ReagentId, message.Amount.GetFixedPoint(), message.FromBuffer);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(component);
        }

        private void TransferReagents(PlantExtractorComponent component, string reagentId, FixedPoint2 amount, bool fromBuffer)
        {
            var container = _itemSlotsSystem.GetItemOrNull(component.Owner, SharedPlantExtractor.BeakerContainerId);
            if (container is null ||
                !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution) ||
                !_solutionContainerSystem.TryGetSolution(component.Owner, SharedPlantExtractor.BufferId, out var bufferSolution))
            {
                return;
            }

            if (fromBuffer) // Buffer to container
            {
                amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
                amount = bufferSolution.RemoveReagent(reagentId, amount);
                _solutionContainerSystem.TryAddReagent(container.Value, containerSolution, reagentId, amount, out var _);
            }
            else // Container to buffer
            {
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(reagentId));
                _solutionContainerSystem.TryRemoveReagent(container.Value, containerSolution, reagentId, amount);
                bufferSolution.AddReagent(reagentId, amount);
            }

            UpdateUiState(component);
        }

        private void DiscardReagents(PlantExtractorComponent chemMaster, string reagentId, FixedPoint2 amount, bool fromBuffer)
        {

            if (fromBuffer)
            {
                if (_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedPlantExtractor.BufferId, out var bufferSolution))
                    bufferSolution.RemoveReagent(reagentId, amount);
                else
                    return;
            }
            else
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster.Owner, SharedPlantExtractor.BeakerContainerId);
                if (container is not null &&
                    _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution))
                {
                    _solutionContainerSystem.TryRemoveReagent(container.Value, containerSolution, reagentId, amount);
                }
                else
                    return;
            }

            UpdateUiState(chemMaster);
        }

        private void OnInteractUsing(EntityUid uid, PlantExtractorComponent component, InteractUsingEvent args)
        {
            var heldEnt = args.Used;
            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedPlantExtractor.InputContainerId);

            if (!HasComp<ExtractableComponent>(heldEnt) || !HasComp<ProduceComponent>(heldEnt))
            {
                if (!HasComp<FitsInDispenserComponent>(heldEnt))
                {
                    _popupSystem.PopupEntity(Loc.GetString("plant-extractor-component-cannot-put-entity-message"), uid, args.User);
                }

                return;
            }

            if (args.Handled)
                return;

            if (inputContainer.ContainedEntities.Count >= component.StorageMaxEntities)
                return;

            if (!inputContainer.Insert(heldEnt, EntityManager))
                return;

            args.Handled = true;
        }

        private void OnEntRemoveAttempt(EntityUid uid, PlantExtractorComponent component, ContainerIsRemovingAttemptEvent args)
        {
            if (HasComp<ActivePlantExtractorComponent>(uid))
                args.Cancel();
        }

        private void OnStartMessage(EntityUid uid, PlantExtractorComponent component, PlantExtractorStartMessage message)
        {
            if (!this.IsPowered(uid, EntityManager) || HasComp<ActivePlantExtractorComponent>(uid))
                return;

            DoWork(uid, component);
        }

        private void OnEjectChamberAllMessage(EntityUid uid, PlantExtractorComponent component, PlantExtractorEjectChamberAllMessage message)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedPlantExtractor.InputContainerId);

            if (HasComp<ActivePlantExtractorComponent>(uid) || inputContainer.ContainedEntities.Count <= 0)
                return;

            ClickSound(component);
            foreach (var entity in inputContainer.ContainedEntities.ToList())
            {
                inputContainer.Remove(entity);
                entity.RandomOffset(0.4f);
            }

            UpdateUiState(component);
        }

        private void OnEjectChamberContentMessage(EntityUid uid, PlantExtractorComponent component, PlantExtractorEjectChamberContentMessage message)
        {
            if (HasComp<ActivePlantExtractorComponent>(uid))
                return;

            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedPlantExtractor.InputContainerId);

            if (inputContainer.Remove(message.EntityId))
            {
                message.EntityId.RandomOffset(0.4f);
                ClickSound(component);
                UpdateUiState(component);
            }
        }

        private void DoWork(EntityUid uid, PlantExtractorComponent component)
        {
            var inputContainer = _containerSystem.EnsureContainer<Container>(uid, SharedPlantExtractor.InputContainerId);

            if (inputContainer.ContainedEntities.Count <= 0)
                return;

            var active = AddComp<ActivePlantExtractorComponent>(uid);
            active.EndTime = _timing.CurTime + component.WorkTime * component.WorkTimeMultiplier;

            component.AudioStream = _audioSystem.PlayPvs(component.ExtractSound, uid, AudioParams.Default.WithPitchScale(1 / component.WorkTimeMultiplier));
            _userInterfaceSystem.TrySendUiMessage(uid, PlantExtractorUiKey.Key, new PlantExtractorWorkStartedMessage());
        }
        #endregion

        #region Helpers
        private ContainerInfo? BuildBeakerContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            var reagents = solution.Contents
                .Select(reagent => (reagent.ReagentId, reagent.Quantity)).ToList();

            return new ContainerInfo(name, true, solution.Volume, solution.MaxVolume, reagents);
        }

        private bool CanExtract(EntityUid uid)
        {
            var solutionName = CompOrNull<ExtractableComponent>(uid)?.GrindableSolution;

            return solutionName is not null && _solutionContainerSystem.TryGetSolution(uid, solutionName, out _);
        }

        private Solution? GetExtractSolution(EntityUid uid)
        {
            if (TryComp<ExtractableComponent>(uid, out var extractable)
                && extractable.GrindableSolution is not null
                && _solutionContainerSystem.TryGetSolution(uid, extractable.GrindableSolution, out var solution))
            {
                return solution;
            }
            else
                return null;
        }

        private void ClickSound(PlantExtractorComponent component)
        {
            _audioSystem.PlayPvs(component.ClickSound, component.Owner, AudioParams.Default.WithVolume(-2f));
        }
        #endregion
    }
}

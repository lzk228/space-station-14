using Content.Server.Chemistry.Components;
using Content.Server.Coordinates.Helpers;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems
{
    public sealed class ChemicalReactionSystem : SharedChemicalReactionSystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        protected override void OnReaction(Solution solution, ReactionPrototype reaction, ReagentPrototype randomReagent, EntityUid owner, FixedPoint2 unitReactions)
        {
            base.OnReaction(solution, reaction,  randomReagent, owner, unitReactions);

            var coordinates = Transform(owner).Coordinates;

            AdminLogger.Add(LogType.ChemicalReaction, reaction.Impact,
                $"Chemical reaction {reaction.ID:reaction} occurred with strength {unitReactions:strength} on entity {ToPrettyString(owner):metabolizer} at {coordinates}");

            _audio.PlayPvs(reaction.Sound, owner);
        }

        public override void BoilOutSolution(Solution solution, EntityUid owner)
        {
            var smokeSolution = solution.BoilOutSolution(_prototypeManager);

            if (smokeSolution == null)
                return;

            var spreadAmount = (int) Math.Max(0, Math.Ceiling((smokeSolution.Volume / 2.5).Float()));
            var duration = 10;
            var transform = EntityManager.GetComponent<TransformComponent>(owner);
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryFindGridAt(transform.MapPosition, out var grid) ||
                !grid.TryGetTileRef(transform.Coordinates, out var tileRef) ||
                tileRef.Tile.IsSpace())
            {
                return;
            }

            var coords = grid.MapToGrid(transform.MapPosition);

            var smokeEntityPrototype = "Smoke";
            var ent = EntityManager.SpawnEntity(smokeEntityPrototype, coords.SnapToGrid());

            if (!EntityManager.TryGetComponent<SmokeComponent>(ent, out var smokeComponent))
            {
                Logger.Error("Couldn't get SmokeComponent from " + smokeEntityPrototype);
                EntityManager.QueueDeleteEntity(ent);
                return;
            }

            var smoke = EntityManager.System<SmokeSystem>();
            smokeComponent.SpreadAmount = spreadAmount;
            smoke.Start(ent, smokeComponent, smokeSolution, duration);

            _audio.PlayPvs("/Audio/Effects/smoke.ogg", owner, AudioHelpers.WithVariation(0.125f));

            if (solution.Volume <= 0)
            {
                var ev = new BoilOutEvent(owner);
                RaiseLocalEvent(owner, ref ev);
            }

            AdminLogger.Add(LogType.ChemicalReaction, LogImpact.High,
                $"Solution {smokeSolution} boiled out with strength {spreadAmount} on entity {ToPrettyString(owner)} at {coords}");
        }
    }

    [ByRefEvent]
    public record struct BoilOutEvent(EntityUid Entity)
    {

    }
}

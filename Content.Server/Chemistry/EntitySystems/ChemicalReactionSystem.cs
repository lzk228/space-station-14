using Content.Server.Chemistry.Components;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Audio;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
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

            var amount = (int) Math.Max(1, Math.Round(Math.Sqrt(smokeSolution.Volume.Float())));
            amount = Math.Min(amount, 15);
            var duration = Math.Min(amount, 5);

            var coords = Transform(owner).Coordinates;

            var smokeEntityPrototype = "Smoke";
            var ent = EntityManager.SpawnEntity(smokeEntityPrototype, coords.SnapToGrid());

            var areaEffectComponent = EntityManager.GetComponentOrNull<SmokeSolutionAreaEffectComponent>(ent);

            if (areaEffectComponent == null)
            {
                Logger.Error("Couldn't get AreaEffectComponent from " + smokeEntityPrototype);
                EntityManager.QueueDeleteEntity(ent);
                return;
            }

            areaEffectComponent.TryAddSolution(smokeSolution);
            areaEffectComponent.Start(amount, duration, 0.5f, 1.0f);

            _audio.PlayPvs("/Audio/Effects/smoke.ogg", owner, AudioHelpers.WithVariation(0.125f));

            if (solution.Volume <= 0)
            {
                var ev = new BoilOutEvent(owner);
                RaiseLocalEvent(owner, ref ev);
            }

            AdminLogger.Add(LogType.ChemicalReaction, LogImpact.High,
                $"Solution {smokeSolution} boiled out with strength {amount} on entity {ToPrettyString(owner)} at {coords}");

        }
    }

    [ByRefEvent]
    public record struct BoilOutEvent(EntityUid Entity)
    {

    }
}

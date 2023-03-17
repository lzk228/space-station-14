using Content.Server.Ghost.Roles.Components;
using Content.Server.Salvage;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects;

[UsedImplicitly]
public sealed class AdaptSpaceMob : ReagentEffect
{
    public override void Effect(ReagentEffectArgs args)
    {
        if (args.EntityManager.HasComponent<SalvageMobRestrictionsComponent>(args.SolutionEntity))
        {
            args.EntityManager.RemoveComponent<SalvageMobRestrictionsComponent>(args.SolutionEntity);
            args.EntityManager.RemoveComponent<GhostTakeoverAvailableComponent>(args.SolutionEntity);
        }
    }
}

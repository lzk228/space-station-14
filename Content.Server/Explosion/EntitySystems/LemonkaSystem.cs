using Content.Server.Botany.Components;
using Content.Server.Explosion.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;

namespace Content.Server.Explosion.EntitySystems;

[UsedImplicitly]
public sealed class LemonkaSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LemonkaComponent, ThrowDoHitEvent>(OnCreamPieHit);
        SubscribeLocalEvent<LemonkaComponent, LandEvent>(OnCreamPieLand);
        SubscribeLocalEvent<LemonkaComponent, ThrowHitByEvent>(OnCreamPiedHitBy);
    }

    private void OnCreamPieLand(EntityUid uid, LemonkaComponent component, ref LandEvent args)
    {
        Explode(uid);
    }

    private void OnCreamPieHit(EntityUid uid, LemonkaComponent component, ThrowDoHitEvent args)
    {
        Explode(uid);
    }

    private void OnCreamPiedHitBy(EntityUid uid, LemonkaComponent component, ThrowHitByEvent args)
    {
        if (!EntityManager.EntityExists(args.Thrown) || !EntityManager.TryGetComponent(args.Thrown, out LemonkaComponent? creamPie))
            return;

        Explode(uid);
    }

    private void Explode(EntityUid uid)
    {
        if (!EntityManager.TryGetComponent(uid, out ProduceComponent? produceComponent))
            return;

        var potency = produceComponent.Seed?.Potency ?? 1;
        EntitySystem.Get<ExplosionSystem>().QueueExplosion(
            uid, "Default", potency * 3, 1, 120);
    }
}

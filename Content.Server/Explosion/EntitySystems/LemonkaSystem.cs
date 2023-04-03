using Content.Server.Botany.Components;
using Content.Server.Explosion.Components;
using Content.Shared.Throwing;
using JetBrains.Annotations;

namespace Content.Server.Explosion.EntitySystems;

[UsedImplicitly]
public sealed class LemonkaSystem : EntitySystem
{
    [Dependency] private ExplosionSystem _explosionSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LemonkaComponent, ThrowDoHitEvent>(OnHit);
        SubscribeLocalEvent<LemonkaComponent, LandEvent>(OnLand);
        SubscribeLocalEvent<LemonkaComponent, ThrowHitByEvent>(OnHitBy);
    }

    private void OnLand(EntityUid uid, LemonkaComponent component, ref LandEvent args)
    {
        Explode(uid);
    }

    private void OnHit(EntityUid uid, LemonkaComponent component, ThrowDoHitEvent args)
    {
        Explode(uid);
    }

    private void OnHitBy(EntityUid uid, LemonkaComponent component, ThrowHitByEvent args)
    {
        if (!EntityManager.EntityExists(args.Thrown) || !EntityManager.TryGetComponent(args.Thrown, out LemonkaComponent? creamPie))
            return;

        Explode(uid);
    }

    private void Explode(EntityUid uid)
    {
        if (!EntityManager.TryGetComponent(uid, out ProduceComponent? produceComponent))
            return;

        var potency = produceComponent.Seed?.Potency ?? 5;
        var totalIntensity = MathF.Sqrt(potency) * 9;
        _explosionSystem.QueueExplosion(uid, "Default", totalIntensity, 1.5f, 120, canCreateVacuum:false);
    }
}

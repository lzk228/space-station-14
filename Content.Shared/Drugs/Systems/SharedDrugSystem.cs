using Content.Shared.Eye.Blinding;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Drugs.Systems;

public sealed class SharedDrugSystem : EntitySystem
{
    [Dependency] private readonly SharedBlindingSystem _blindingSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BerserkDrugComponent, ComponentInit>(OnBerserkInit);
        SubscribeLocalEvent<BerserkDrugComponent, ComponentRemove>(OnBerserkRemoval);
    }

    #region BerserkDrug

    private void OnBerserkInit(EntityUid uid, BerserkDrugComponent component, ComponentInit args)
    {
        if (!TryComp(uid, out MobThresholdsComponent? mobThresholdsComponent))
            return;

        mobThresholdsComponent.IgnoreCritical = true;
        Dirty(mobThresholdsComponent);

        if (!TryComp<BlindableComponent>(uid, out var blindComp))
            return;

        _blindingSystem.AdjustEyeDamage(uid, 7, blindComp);
    }

    private void OnBerserkRemoval(EntityUid uid, BerserkDrugComponent component, ComponentRemove args)
    {
        if (!TryComp(uid, out MobThresholdsComponent? mobThresholdsComponent))
            return;

        mobThresholdsComponent.IgnoreCritical = false;
        Dirty(mobThresholdsComponent);

        if (!TryComp<BlindableComponent>(uid, out var blindComp))
            return;

        _blindingSystem.AdjustEyeDamage(uid, -7, blindComp);
    }

    #endregion
}

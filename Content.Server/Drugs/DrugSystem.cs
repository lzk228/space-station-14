using Content.Server.Drugs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Drugs;

public sealed class DrugSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ZoomiumComponent, ComponentRemove>(OnZoomiumRemoval);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var zoomiumComponent in EntityQuery<ZoomiumComponent>())
        {
            UpdateZoomium(zoomiumComponent.Owner, zoomiumComponent);
        }
    }

    private void OnZoomiumRemoval(EntityUid uid, ZoomiumComponent component, ComponentRemove args)
    {
        var eye = EnsureComp<EyeComponent>(uid);
        eye.Zoom = Vector2.One;
        Dirty(eye);
    }

    private void UpdateZoomium(EntityUid uid, ZoomiumComponent component)
    {
        if (component.NextSmallUpdate > _gameTiming.CurTime)
            return;

        component.NextSmallUpdate = _gameTiming.CurTime + component.SmallUpdateDelay;

        var eye = EnsureComp<EyeComponent>(uid);
        component.CurrentZoomLevel = (component.CurrentZoomLevel + component.NextZoomLevel) / 2;
        eye.Zoom = Vector2.One * component.CurrentZoomLevel;
        Dirty(eye);

        if (component.NextUpdate > _gameTiming.CurTime)
            return;

        component.NextZoomLevel = _random.NextFloat();
        component.NextUpdate = _gameTiming.CurTime + component.UpdateDelay / component.Severity;
    }
}

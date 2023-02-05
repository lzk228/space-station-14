using Content.Server.Popups;
using Content.Shared.Interaction.Events;
using Content.Shared.Paper;
using JetBrains.Annotations;

namespace Content.Server.Paper;

[UsedImplicitly]
public sealed class StampStateHandlerSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StampStateHandlerComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(EntityUid uid, StampStateHandlerComponent? component, UseInHandEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (args.Handled)
            return;

        if (EntityManager.TryGetComponent(uid, out StampComponent? stampComponent))
        {
            var lenght = Math.Min(component.StampStateCollection.Length, component.StampNameCollection.Length);
            component.CurrentStateIndex = (component.CurrentStateIndex + 1) % lenght;
            stampComponent.StampState = component.StampStateCollection[component.CurrentStateIndex];
            stampComponent.StampedName = component.StampNameCollection[component.CurrentStateIndex];

            var sign = Loc.GetString(stampComponent.StampedName);
            var stampChangeMessage = Loc.GetString("stamp-state-handler-component-state-change", ("name", sign));
            _popupSystem.PopupEntity(stampChangeMessage, args.User, args.User);
        }
    }
}

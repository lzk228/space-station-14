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
            stampComponent.StampedColor = GetStampColor(stampComponent.StampState);

            var sign = Loc.GetString(stampComponent.StampedName);
            var stampChangeMessage = Loc.GetString("stamp-state-handler-component-state-change", ("name", sign));
            _popupSystem.PopupEntity(stampChangeMessage, args.User, args.User);
        }
    }

    private Color GetStampColor(string stampState)
    {
        switch (stampState)
        {
            case "paper_stamp-deny":
                return Color.FromHex("#a23e3e");
            case "paper_stamp-approve":
                return Color.FromHex("#00be00");
            case "paper_stamp-syndicate":
                return Color.FromHex("#850000");
            case "paper_stamp-cap":
                return Color.FromHex("#3681bb");
            case "paper_stamp-chaplain":
                return Color.FromHex("#d70601");
            case "paper_stamp-clown":
                return Color.FromHex("#ff33cc");
            case "paper_stamp-ce":
                return Color.FromHex("#c69b17");
            case "paper_stamp-cmo":
                return Color.FromHex("#33ccff");
            case "paper_stamp-hop":
                return Color.FromHex("#6ec0ea");
            case "paper_stamp-hos":
                return Color.FromHex("#cc0000");
            case "paper_stamp-mime":
                return Color.FromHex("#777777");
            case "paper_stamp-qm":
                return Color.FromHex("#a23e3e");
            case "paper_stamp-rd":
                return Color.FromHex("#1f66a0");
            case "paper_stamp-warden":
                return Color.FromHex("#5b0000");
            case "paper_stamp-trader":
                return Color.FromHex("#000000");
            default:
                return Color.White;
        }
    }
}
using Content.Server.Body.Components; //A-13 Dragon fix full
using Content.Shared.Damage.Components; //A-13 Dragon fix full
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components; //A-13 Dragon fix full

namespace Content.Server.Devour;

public sealed class DevourSystem : SharedDevourSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DevourerComponent, DevourDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<DevourerComponent, BeingGibbedEvent>(OnBeingGibbed); //A-13 Dragon fix full
        SubscribeLocalEvent<DevourerComponent, ComponentRemove>(OnRemoved); //A-13 Dragon fix full
    }

    private void OnDoAfter(EntityUid uid, DevourerComponent component, DevourDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        //A-13 Dragon fix full start
        if (component.ShouldStoreDevoured && HasComp<MobStateComponent>(args.Args.Target))
        {
            ContainerSystem.Insert(args.Args.Target.Value, component.Stomach);

            EnsureComp<DevouredComponent>(args.Args.Target.Value);

            var ev = new DevouredEvent(uid);
            RaiseLocalEvent(args.Args.Target.Value, ref ev);
        }

        if (component.FoodPreference == FoodPreference.All || component.FoodPreference == FoodPreference.Humanoid && HasComp<HumanoidAppearanceComponent>(args.Args.Target))
        {
            if (TryComp<PassiveDamageComponent>(uid, out var passiveHealing))
            {
                if (component.PassiveDevourHealing != null)
                {
                    passiveHealing.Damage += component.PassiveDevourHealing;
                }
            }
        }
        //A-13 Dragon fix full end

        //TODO: Figure out a better way of removing structures via devour that still entails standing still and waiting for a DoAfter. Somehow.
        //If it does not have a mobState, it must be a structure
        else if (args.Args.Target != null)
        {
            QueueDel(args.Args.Target.Value);
        }

        AudioSystem.PlayPvs(component.SoundDevour, uid); //A-13 Dragon fix full
    }
    //A-13 Dragon fix full start
    public void EmptyStomach(EntityUid uid, DevourerComponent component)
    {
        foreach (var entity in component.Stomach.ContainedEntities)
        {
            RemComp<DevouredComponent>(entity);
        }
        ContainerSystem.EmptyContainer(component.Stomach);
    }

    private void OnBeingGibbed(EntityUid uid, DevourerComponent component, BeingGibbedEvent args)
    {
        EmptyStomach(uid, component);
    }
    private void OnRemoved(EntityUid uid, DevourerComponent component, ComponentRemove args)
    {
        EmptyStomach(uid, component);
    }
    //A-13 Dragon fix full end
}
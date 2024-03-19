using Content.Shared.Damage; //A-13 Dragon fix full
using Content.Shared.Mobs; //A-13 Dragon fix full
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Devour.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDevourSystem))]
public sealed partial class DevourerComponent : Component
{
    [DataField] //A-13 Dragon fix full
    public ProtoId<EntityPrototype> DevourAction = "ActionDevour"; //A-13 Dragon fix full

    [DataField] //A-13 Dragon fix full
    public EntityUid? DevourActionEntity;

    [ViewVariables(VVAccess.ReadWrite), DataField] //A-13 Dragon fix full
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    [ViewVariables(VVAccess.ReadWrite), DataField] //A-13 Dragon fix full
    public float DevourTime = 3f;

    /// <summary>
    /// The amount of time it takes to devour something
    /// <remarks>
    /// NOTE: original intended design was to increase this proportionally with damage thresholds, but those proved quite difficult to get consistently. right now it devours the structure at a fixed timer.
    /// </remarks>
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField] //A-13 Dragon fix full
    public float StructureDevourTime = 10f;

    [ViewVariables(VVAccess.ReadWrite), DataField] //A-13 Dragon fix full
    public SoundSpecifier? SoundStructureDevour = new SoundPathSpecifier("/Audio/Machines/airlock_creaking.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };

    /// <summary>
    /// Where the entities go when it devours them, empties when it is butchered.
    /// </summary>
    public Container Stomach = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField] //A-13 Dragon fix full
    public bool ShouldStoreDevoured = true;

    [ViewVariables(VVAccess.ReadWrite), DataField] //A-13 Dragon fix full
    public EntityWhitelist? Whitelist = new()
    {
        Components = new[]
        {
            "MobState",
        }
    };

    /// <summary>
    /// The favorite food not only feeds you, but also increases your passive healing.
    /// </summary>
    [DataField] //A-13 Dragon fix full
    public FoodPreference FoodPreference = FoodPreference.All; //A-13 Dragon fix full

    /// <summary>
    /// Passive healing added for each devoured favourite food.
    /// </summary>
    [DataField] //A-13 Dragon fix full
    public DamageSpecifier? PassiveDevourHealing = new(); //A-13 Dragon fix full

    /// <summary>
    /// The passive damage done to devoured entities.
    /// </summary>
    [DataField] //A-13 Dragon fix full
    public DamageSpecifier? StomachDamage = new(); //A-13 Dragon fix full

    /// <summary>
    /// The MobStates the stomach is allowed to deal damage on.
    /// </summary>
    [DataField] //A-13 Dragon fix full
    public List<MobState> DigestibleStates = new(); //A-13 Dragon fix full
}

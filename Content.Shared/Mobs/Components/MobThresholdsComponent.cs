using Content.Shared.Drugs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem), typeof(SharedDrugSystem))]
public sealed class MobThresholdsComponent : Component
{
    [DataField("thresholds", required:true), AutoNetworkedField(true)]
    public SortedDictionary<FixedPoint2, MobState> Thresholds = new();

    [DataField("triggersAlerts"), AutoNetworkedField]
    public bool TriggersAlerts = true;

    [DataField("currentThresholdState"), AutoNetworkedField]
    private bool _ignoreCritical; // Tehnox's Krokodil System Start

    [DataField("ignoreCritical")]
    public bool IgnoreCritical
    {
        get => _ignoreCritical;
        set
        {
            if (!value)
                IsNeededToVerifyState = true;
            _ignoreCritical = value;
        }
    }

    public bool IsNeededToVerifyState = false; // Tehnox's Krokodil System End

    public MobState CurrentThresholdState;

    /// <summary>
    /// Whether or not this entity can be revived out of a dead state.
    /// </summary>
    [DataField("allowRevives"), AutoNetworkedField]
    public bool AllowRevives;
[Serializable, NetSerializable] // Tehnox's Krokodil System Start
public sealed class MobThresholdComponentState : ComponentState
{
    public Dictionary<FixedPoint2, MobState> Thresholds;
    public MobState CurrentThresholdState;
    public bool IgnoreCritical;
    public bool IsNeededToVerifyState;
    public MobThresholdComponentState(MobState currentThresholdState,
        Dictionary<FixedPoint2, MobState> thresholds, bool ignoreCritical, bool isNeededToVerifyState)
    {
        CurrentThresholdState = currentThresholdState;
        Thresholds = thresholds;
        IgnoreCritical = ignoreCritical;
        IsNeededToVerifyState = isNeededToVerifyState;
    }                          // Tehnox's Krokodil System End

}

using Content.Shared.Drugs.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(MobThresholdSystem), typeof(SharedDrugSystem))]
public sealed class MobThresholdsComponent : Component
{
    [DataField("thresholds", required:true)]public SortedDictionary<FixedPoint2, MobState> Thresholds = new();

    [DataField("triggersAlerts")] public bool TriggersAlerts = true;

    private bool _ignoreCritical;

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

    public bool IsNeededToVerifyState = false;

    public MobState CurrentThresholdState;
}

[Serializable, NetSerializable]
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
    }

}

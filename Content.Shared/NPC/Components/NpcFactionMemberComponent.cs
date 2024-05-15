using System.Linq; //A-13 NpcFactionMember edit
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.NPC.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(NpcFactionSystem))]
public sealed partial class NpcFactionMemberComponent : Component
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; //A-13 NpcFactionMember edit

    /// <summary>
    /// Factions this entity is a part of.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();

    //A-13 NpcFactionMember edit start
    /// <summary>
    /// Строковой адаптер для редактирования фракций.
    /// </summary>
    /// <remarks>
    /// Фракции перечисляются через запятую.<br/>
    /// Если указанной фракции не существует, все изменения будут отменены.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public string FactionsAdapter
    {
        get => GetterFaction(Factions);
        set => SetterFactions(ref Factions, value);
    }
    //A-13 NpcFactionMember edit end

    /// <summary>
    /// Cached friendly factions.
    /// </summary>
    [ViewVariables]
    public HashSet<ProtoId<NpcFactionPrototype>> FriendlyFactions = new(); //A-13 NpcFactionMember edit

    //A-13 NpcFactionMember edit start
    /// <summary>
    /// Строковой адаптер для редактирования дружественных фракций.
    /// </summary>
    /// <remarks>
    /// Фракции перечисляются через запятую.<br/>
    /// Если указанной фракции не существует, все изменения будут отменены.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public string FriendlyFactionsAdapter
    {
        get => GetterFaction(FriendlyFactions);
        set => SetterFactions(ref FriendlyFactions, value);
    }
    //A-13 NpcFactionMember edit end

    /// <summary>
    /// Cached hostile factions.
    /// </summary>
    [ViewVariables]
    public HashSet<ProtoId<NpcFactionPrototype>> HostileFactions = new(); //A-13 NpcFactionMember edit

    //A-13 NpcFactionMember edit start
    /// <summary>
    /// Строковой адаптер для редактирования враждебных фракций.
    /// </summary>
    /// <remarks>
    /// Фракции перечисляются через запятую.<br/>
    /// Если указанной фракции не существует, все изменения будут отменены.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite)]
    public string HostileFactionsAdapter
    {
        get => GetterFaction(HostileFactions);
        set => SetterFactions(ref HostileFactions, value);
    }
    //A-13 NpcFactionMember edit end

    //A-13 NpcFactionMember edit start
    private string GetterFaction(HashSet<ProtoId<NpcFactionPrototype>> factions)
    {
        return factions.Count == 0
            ? ""
            : string.Join(", ", factions);
    }

    private void SetterFactions(ref HashSet<ProtoId<NpcFactionPrototype>> factions, string value)
    {
        if (value.Length == 0)
        {
            factions.Clear();
            return;
        }

        HashSet<ProtoId<NpcFactionPrototype>> newFactions = [];
        var factionsStr = value.Replace(", ", ",").Split(",");
        var prototypes = _prototypeManager.EnumeratePrototypes<NpcFactionPrototype>().ToList();

        foreach (var factionStr in factionsStr)
        {
            if (prototypes.Any(prototype => prototype.ID == factionStr))
                newFactions.Add(factionStr);
            else
                return;
        }

        factions = newFactions;
    }
    //A-13 NpcFactionMember edit end
}

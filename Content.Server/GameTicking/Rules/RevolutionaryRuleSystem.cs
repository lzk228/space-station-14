using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Flash;
using Content.Server.Mind.Components;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.Players;
using Content.Server.Popups;
using Content.Server.Revolutionary.Components;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Implants.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Stunnable;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.GameTicking.Rules;

public sealed class RevolutionaryRuleSystem : GameRuleSystem
{
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationJobsSystem _stationJobsSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly FactionSystem _factionSystem = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly UplinkSystem _uplinkSystem = default!;

    private ISawmill _sawmill = default!;

    private enum WinType
    {
        Neutral,
        Crew,
        Revs
    }

    private WinType _winType = WinType.Neutral;

    private WinType RuleWinType
    {
        get => _winType;
        set
        {
            _winType = value;

            switch (value)
            {
                case WinType.Crew:
                    DeconvertAllRevs();
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("revolutionary-crew-win-announcement"));
                    break;
                case WinType.Revs:
                    _chatManager.DispatchServerAnnouncement(Loc.GetString("revolutionary-revs-win-announcement"));
                    _roundEndSystem.EndRound();
                    break;
                case WinType.Neutral:
                default:
                    break;
            }
        }
    }

    private Dictionary<string, IPlayerSession> _headRevsPlayers = new();
    private Dictionary<string, IPlayerSession> _revsPlayers = new();
    private Dictionary<string, IPlayerSession> _headPlayers = new();
    private readonly string[] _headRolePrototypeIds =
    {
        "Captain",
        "HeadOfPersonnel",
        "ChiefEngineer",
        "ChiefMedicalOfficer",
        "ResearchDirector",
        "HeadOfSecurity",
        "Quartermaster"
    };
    private readonly string[] _immuneRolePrototypeIds =
    {
        "Captain",
        "HeadOfPersonnel",
        "ChiefEngineer",
        "ChiefMedicalOfficer",
        "ResearchDirector",
        "HeadOfSecurity",
        "Quartermaster",
        "SecurityOfficer"
    };

    private readonly Dictionary<string, float> _flashRangeDictionary = new Dictionary<string, float>()
    {
        { "Flash", 4f },
        { "GrenadeFlashBang", 15f},
        { "RevolutionaryFlash", 2f}
    };

    private const int MinRevs = 3;
    private const int MaxRevs = 5;
    private const int MinPlayers = 15;
    private const string RevolutionaryHeadPrototypeId = "RevolutionaryHead";
    private const string RevolutionaryPrototypeId = "Revolutionary";
    private const string UplinkPresetId = "StorePresetUplinkRevolutionary";
    private const int UplinkBalance = 20;
    private const float MaxConvertRange = 4f;

    private DamageSpecifier _convertHeal = default!;
    public override string Prototype => "Revolutionary";

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("preset");
        _convertHeal = new DamageSpecifier(_prototypeManager.Index<DamageGroupPrototype>("Brute"), -40f);

        SubscribeLocalEvent<RoundStartAttemptEvent>(OnStartAttempt);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<RevolutionaryComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobAssigned);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
        SubscribeLocalEvent<FlashAttemptEvent>(OnFlashAttempt);
        SubscribeLocalEvent<RevolutionaryComponent, EntInsertedIntoContainerMessage>(OnLoyaltyImplantInserted);
    }

    public override void Started()
    {
    }

    public override void Ended()
    {
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var revComponent in EntityQuery<RevolutionaryComponent>())
        {
            UpdateLoyaltyImplant(revComponent);
        }
    }

    public void MakeHeadRev(IPlayerSession player)
    {
        var mind = player.Data.ContentData()?.Mind;
        if (mind == null)
        {
            _sawmill.Error("Failed getting mind for picked head revolutionary.");
            return;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            _sawmill.Error("Mind picked for head revolutionary did not have an attached entity.");
            return;
        }

        // Ugly hack :( idk how to do it

        var revComponent = _componentFactory.GetComponent<RevolutionaryComponent>();
        revComponent.Owner = entity;
        revComponent.Head = true;
        EntityManager.AddComponent(entity, revComponent, true);

        _uplinkSystem.AddUplink(entity, UplinkBalance, UplinkPresetId);
    }

    public void MakeRev(IPlayerSession player)
    {
        var mind = player.Data.ContentData()?.Mind;
        if (mind is null)
        {
            _sawmill.Error("Failed getting mind for picked revolutionary.");
            return;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            _sawmill.Error("Mind picked for revolutionary did not have an attached entity.");
            return;
        }

        EntityManager.EnsureComponent<RevolutionaryComponent>(entity);
    }

    private void OnStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!RuleAdded)
            return;

        if (!ev.Forced && ev.Players.Length < MinPlayers)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-not-enough-ready-players", ("readyPlayersCount", ev.Players.Length), ("minimumPlayers", MinPlayers)));
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("traitor-no-one-ready"));
            ev.Cancel();
        }
    }

    private void OnComponentInit(EntityUid uid, RevolutionaryComponent component, ComponentInit args)
    {
        if (!TryComp<MindComponent>(uid, out var mindComponent) || !RuleAdded)
            return;

        var mind = mindComponent.Mind;
        if (mind is null)
        {
            _sawmill.Error($"Failed getting mind for picked revolutionary ({ToPrettyString(uid)}).");
            return;
        }

        var session = mindComponent.Mind?.Session;
        var name = MetaData(uid).EntityName;
        if (session is null)
            return;

        AntagPrototype antagPrototype;
        switch (component.Head)
        {
            case true when !_headRevsPlayers.ContainsKey(name):
                _headRevsPlayers.Add(name, session);
                antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryHeadPrototypeId);
                break;
            case false when !_revsPlayers.ContainsKey(name):
                _revsPlayers.Add(name, session);
                antagPrototype = _prototypeManager.Index<AntagPrototype>(RevolutionaryPrototypeId);
                break;
            default:
                return;
        }

        var traitorRole = new TraitorRole(mind, antagPrototype);
        mind.AddRole(traitorRole);

        _factionSystem.RemoveFaction(uid, "NanoTrasen", false);
        _factionSystem.AddFaction(uid, "Syndicate");

        _popupSystem.PopupEntity(Loc.GetString("revolutionary-convert-to-rev"),
            uid, uid, PopupType.LargeCaution);
    }

    private void OnComponentRemove(EntityUid uid, RevolutionaryComponent component, ComponentRemove args)
    {
        if (!RuleAdded || component.Head)
            return;

        var name = MetaData(uid).EntityName;
        _revsPlayers.Remove(name);

        if (!TryComp<MindComponent>(uid, out var mindComponent))
            return;

        var mind = mindComponent.Mind;
        if (mind is null)
        {
            _sawmill.Error($"Failed getting mind for picked revolutionary ({ToPrettyString(uid)}).");
            return;
        }

        var role = mind.AllRoles
            .Where(x => x is TraitorRole).FirstOrDefault(x => (x as TraitorRole)?.Prototype.ID == RevolutionaryPrototypeId);
        if (role != null)
            mind.RemoveRole(role);

        if (!mind.AllRoles.Any(x => x.Antagonist))
        {
            _factionSystem.RemoveFaction(uid, "Syndicate", false);
            _factionSystem.AddFaction(uid, "NanoTrasen");
        }

        _popupSystem.PopupEntity(Loc.GetString("revolutionary-convert-from-rev"),
            uid, uid, PopupType.LargeCaution);

        CheckWinConditions();
    }

    private void OnJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        if (!RuleAdded)
            return;

        _headRevsPlayers = new Dictionary<string, IPlayerSession>();
        _headPlayers = new Dictionary<string, IPlayerSession>();
        _revsPlayers = new Dictionary<string, IPlayerSession>();

        var everyone = new List<IPlayerSession>();
        var prefList = new List<IPlayerSession>();

        foreach (var player in ev.Players)
        {
            if (!ev.Profiles.ContainsKey(player.UserId))
                continue;

            everyone.Add(player);

            if (_headRolePrototypeIds.Contains(player.Data.ContentData()?.Mind?.CurrentJob?.Prototype.ID))
            {
                AddHead(player);
                continue;
            }

            if (ev.Profiles[player.UserId].AntagPreferences.Contains(RevolutionaryHeadPrototypeId))
            {
                prefList.Add(player);
            }
        }

        if (_headPlayers.Count == 0)
        {
            _sawmill.Error("No heads on station. Aborting Revolutionary gameRule.");
            // TODO: abort game rule
            return;
        }

        if (prefList.Count == 0)
        {
            _sawmill.Info("Insufficient preferred revolutionary, picking at random.");
            prefList = everyone.Where(x =>
                !_immuneRolePrototypeIds.Contains(x.Data.ContentData()?.Mind?.CurrentJob?.Prototype.ID)).ToList();

            if (prefList.Count == 0)
            {
                _sawmill.Error("Tried to start Revolutionary mode without any candidates.");
                // TODO: abort game rule
                return;
            }
        }

        var numRevs = (int) Math.Round((everyone.Count + 63) / 26f);
        numRevs = MathHelper.Clamp(numRevs, MinRevs, MaxRevs);

        var selectedRevs = SelectHeadRevs(prefList, numRevs);

        foreach (var selectedRev in selectedRevs)
        {
            MakeHeadRev(selectedRev);
        }

        LimitHeadJobs();
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        if (!RuleAdded)
            return;

        switch (RuleWinType)
        {
            case WinType.Neutral:
                ev.AddLine(Loc.GetString("revolutionary-neutral-win"));
                break;
            case WinType.Crew:
                ev.AddLine(Loc.GetString("revolutionary-crew-win"));
                break;
            case WinType.Revs:
                ev.AddLine(Loc.GetString("revolutionary-revs-win"));
                break;
        }
        ev.AddLine("⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯");

        ev.AddLine(Loc.GetString("revolutionary-head-rev-title"));
        foreach (var (name, session) in _headRevsPlayers)
        {
            var status = CheckHeadRevStatus(session);
            var color = status ? "green" : "red";
            var job = session.Data.ContentData()?.Mind?.CurrentJob?.Name ?? "";
            ev.AddLine(Loc.GetString("revolutionary-head-rev-status",
                ("job", job),
                ("markupColor", color),
                ("name", name),
                ("username", session.Name)));
        }
        ev.AddLine("⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯");

        ev.AddLine(Loc.GetString("revolutionary-head-title"));
        foreach (var (name, session) in _headPlayers)
        {
            var status = CheckHeadStatus(session);
            var color = status ? "green" : "red";
            var job = session.Data.ContentData()?.Mind?.CurrentJob?.Name ?? "";
            ev.AddLine(Loc.GetString("revolutionary-head-status",
                ("job", job),
                ("markupColor", color),
                ("name", name),
                ("username", session.Name)));
        }
        ev.AddLine("⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯");

        if (_revsPlayers.Count == 0)
            return;

        ev.AddLine(Loc.GetString("revolutionary-rev-title"));
        foreach (var (name, session) in _revsPlayers)
        {
            var status = CheckHeadRevStatus(session);
            var color = status ? "green" : "red";
            var job = session.Data.ContentData()?.Mind?.CurrentJob?.Name ?? "";
            ev.AddLine(Loc.GetString("revolutionary-rev-status",
                ("job", job),
                ("markupColor", color),
                ("name", name),
                ("username", session.Name)));
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!RuleAdded || RuleWinType is WinType.Crew or WinType.Revs)
            return;

        if (ev.NewMobState is MobState.Alive)
            return;

        if (ev.NewMobState is MobState.Critical && ev.Origin is not null)
            ConvertEntityOnCritical(ev.Target, ev.Origin.Value);

        CheckWinConditions();
    }

    private void OnFlashAttempt(FlashAttemptEvent ev)
    {
        if (!RuleAdded || ev.Cancelled)
            return;

        var target = ev.Target;

        if (ev.User is null || ev.Used is null)
            return;

        var user = ev.User.Value;
        var flash = ev.Used.Value;

        if (!_flashRangeDictionary.Keys.Contains(MetaData(flash).EntityPrototype?.ID))
            return;

        var flashId = MetaData(flash).EntityPrototype?.ID!;

        if (!TryComp<RevolutionaryComponent>(user, out var revComponent) || HasComp<RevolutionaryComponent>(target))
            return;

        if (!revComponent.Head && flashId != "RevolutionaryFlash")
            return;

        if (!TryComp<MindComponent>(target, out var mindComponent))
            return;

        if (_immuneRolePrototypeIds.Contains(mindComponent.Mind?.CurrentJob?.Prototype.ID))
            return;

        var targetSession = _playerManager.ServerSessions.FirstOrDefault(x => x.AttachedEntity == target);
        if (targetSession is null)
            return;

        if (Transform(target).Coordinates.TryDistance(EntityManager, Transform(user).Coordinates, out var distance) &&
            distance > _flashRangeDictionary[flashId])
            return;

        if (HasLoyaltyImplant(target))
        {
            if (flashId == "RevolutionaryFlash")
                DamageLoyaltyImplant(target);
            return;
        }

        if (!TryComp<FactionComponent>(target, out var factionComponent))
            return;

        var factions = factionComponent.Factions;
        if (factions.Contains("NanoTrasen"))
        {
            _stunSystem.TryKnockdown(target, TimeSpan.FromSeconds(5), false);
            MakeRev(targetSession);

            var originSession = _playerManager.ServerSessions.FirstOrDefault(x => x.AttachedEntity == user);
            if (originSession is null)
                return;
            var message = Loc.GetString("revolutionary-convert-user", ("name", ToPrettyString(target).Name ?? "??"));
            _chatManager.ChatMessageToOne(ChatChannel.Server, message, message, source: EntityUid.Invalid, hideChat: false, client: originSession.ConnectedClient, colorOverride: Color.Red);
        }
    }

    private void OnLoyaltyImplantInserted(EntityUid uid, RevolutionaryComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!RuleAdded)
            return;

        if (!HasComp<LoyaltyImplantComponent>(args.Entity))
            return;

        if (!component.Head)
            return;

        var name = MetaData(uid).EntityName;
        _popupSystem.PopupEntity(Loc.GetString("revolutionary-implant-resist", ("name", name)), uid, PopupType.Large);
        EntityManager.QueueDeleteEntity(args.Entity);
    }

    private void ConvertEntityOnCritical(EntityUid target, EntityUid origin)
    {
        if (HasComp<RevolutionaryComponent>(origin) && !HasComp<RevolutionaryComponent>(target))
        {
            if (!TryComp<MindComponent>(target, out var mindComponent))
                return;

            if (_immuneRolePrototypeIds.Contains(mindComponent.Mind?.CurrentJob?.Prototype.ID))
                return;

            var targetSession = _playerManager.ServerSessions.FirstOrDefault(x => x.AttachedEntity == target);
            if (targetSession is null)
                return;

            if (HasLoyaltyImplant(target))
                return;

            if (Transform(target).Coordinates.TryDistance(EntityManager, Transform(origin).Coordinates, out var distance) && distance > MaxConvertRange)
                return;

            if (!TryComp<FactionComponent>(target, out var factionComponent))
                return;

            var factions = factionComponent.Factions;
            if (factions.Contains("NanoTrasen"))
            {
                MakeRev(targetSession);
                _damageableSystem.TryChangeDamage(target, _convertHeal, origin: origin);

                var originSession = _playerManager.ServerSessions.FirstOrDefault(x => x.AttachedEntity == origin);
                if (originSession is null)
                    return;
                var message = Loc.GetString("revolutionary-convert-user", ("name", ToPrettyString(target).Name ?? "??"));
                _chatManager.ChatMessageToOne(ChatChannel.Server, message, message, source: EntityUid.Invalid, hideChat: false, client: originSession.ConnectedClient, colorOverride: Color.Red);
            }
        }
        else if (!HasComp<RevolutionaryComponent>(origin) && TryComp<RevolutionaryComponent>(target, out var revComponent))
        {
            if (revComponent.Head)
                return;

            if (TryComp<LoyaltyImplantComponent>(origin, out var implantComponent))
            {
                RemCompDeferred<RevolutionaryComponent>(target);
                _damageableSystem.TryChangeDamage(target, implantComponent.Heal, origin: origin);
                return;
            }

            if (!TryComp<FactionComponent>(origin, out var factionComponent))
                return;

            var factions = factionComponent.Factions;
            if (factions.Contains("NanoTrasen"))
            {
                RemCompDeferred<RevolutionaryComponent>(target);
                _damageableSystem.TryChangeDamage(target, _convertHeal, origin: origin);
            }
        }
    }

    #region Utility

    private void UpdateLoyaltyImplant(RevolutionaryComponent component)
    {
        if (!_containerSystem.TryGetContainer(component.Owner, ImplanterComponent.ImplantSlotId,
                out var implantContainer))
            return;

        var implantCompQuery = GetEntityQuery<LoyaltyImplantComponent>();

        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (!implantCompQuery.TryGetComponent(implant, out var implantComp))
                return;

            if (implantComp.NextImpact > _gameTiming.CurTime)
                return;

            if (!_mobStateSystem.IsAlive(component.Owner))
                return;

            implantComp.NextImpact = _gameTiming.CurTime + implantComp.ImpactDelay;
            _damageableSystem.TryChangeDamage(component.Owner, implantComp.Damage, origin: implant);
        }
    }

    private bool HasLoyaltyImplant(EntityUid target)
    {
        if (!_containerSystem.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return false;

        var implantCompQuery = GetEntityQuery<LoyaltyImplantComponent>();
        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (implantCompQuery.HasComponent(implant))
                return true;
        }

        return false;
    }

    private void DamageLoyaltyImplant(EntityUid target)
    {
        if (!_containerSystem.TryGetContainer(target, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        var implantCompQuery = GetEntityQuery<LoyaltyImplantComponent>();
        foreach (var implant in implantContainer.ContainedEntities)
        {
            if (!implantCompQuery.TryGetComponent(implant, out var implantComponent))
                continue;

            implantComponent.Durability--;
            if (implantComponent.Durability <= 0)
                EntityManager.QueueDeleteEntity(implant);
        }
    }

    private void DeconvertAllRevs()
    {
        foreach (var revolutionaryComponent in EntityQuery<RevolutionaryComponent>())
        {
            RemCompDeferred<RevolutionaryComponent>(revolutionaryComponent.Owner);
        }
    }

        private List<IPlayerSession> SelectHeadRevs(List<IPlayerSession> candidates, int numRevs)
    {
        numRevs = Math.Min(numRevs, candidates.Count);
        var results = new List<IPlayerSession>(numRevs);
        for (var i = 0; i < numRevs; i++)
        {
            results.Add(_random.PickAndTake(candidates));
        }
        return results;
    }

    private void AddHead(IPlayerSession player)
    {
        var mind = player.Data.ContentData()?.Mind;
        if (mind is null)
        {
            _sawmill.Error("Failed getting mind for picked CR.");
            return;
        }

        if (mind.OwnedEntity is not { } entity)
        {
            _sawmill.Error("Mind picked for CR did not have an attached entity.");
            return;
        }

        var name = MetaData(entity).EntityName;
        _headPlayers.Add(name, player);
    }

    private void LimitHeadJobs()
    {
        foreach (var station in _stationSystem.Stations)
        {
            foreach (var headRolePrototypeId in _headRolePrototypeIds)
            {
                _stationJobsSystem.TrySetJobSlot(station, headRolePrototypeId, 0);
            }
        }
    }

    private void CheckWinConditions()
    {
        if (RuleWinType is WinType.Crew or WinType.Revs)
            return;

        var anyHeadsAlive = _headPlayers.Any(x => CheckHeadStatus(x.Value));

        var anyHeadRevsAlive = false;
        foreach (var (revolutionaryComponent, stateComponent) in EntityQuery<RevolutionaryComponent, MobStateComponent>())
        {
            if (revolutionaryComponent.Head && stateComponent.CurrentState is MobState.Alive or MobState.Critical)
            {
                anyHeadRevsAlive = true;
                break;
            }
        }

        if (anyHeadsAlive && anyHeadRevsAlive)
            RuleWinType = WinType.Neutral;
        else if (anyHeadsAlive)
            RuleWinType = WinType.Crew;
        else
            RuleWinType = WinType.Revs;
    }

    private bool CheckHeadStatus(IPlayerSession session)
    {
        var entity = session.AttachedEntity;
        if (entity is null)
            return false;

        if (!TryComp<MobStateComponent>(entity, out var mobStateComponent))
            return false;

        if (mobStateComponent.CurrentState is MobState.Dead or MobState.Invalid)
            return false;

        if (!_headRolePrototypeIds.Contains(session.Data.ContentData()?.Mind?.CurrentJob?.Prototype.ID))
            return false;

        return true;
    }

    private bool CheckHeadRevStatus(IPlayerSession session)
    {
        var entity = session.AttachedEntity;
        if (entity is null)
            return false;

        if (!HasComp<RevolutionaryComponent>(entity))
            return false;

        if (!TryComp<MobStateComponent>(entity, out var mobStateComponent))
            return false;

        if (mobStateComponent.CurrentState is MobState.Dead or MobState.Invalid)
            return false;

        return true;
    }

    #endregion
}

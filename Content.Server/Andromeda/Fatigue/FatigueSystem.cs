using Content.Shared.Bed.Sleep;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Andromeda.Lemird.Fatigue;
using Content.Server.Guardian;
using Content.Shared.Alert;
using Content.Shared.Andromeda.Lemird.Nearsighted;

namespace Content.Server.Andromeda.Fatigue;
public sealed class FatigueSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly FatigueMovementSpeedSystem _fatigueMovementSpeedSystem = default!;
    [Dependency] private readonly FatigueSystem _fatigueSystem = default!;
    private const float DecreaseIntervalMinutes = 1f; //Исправить на 5
    private const float RecoverIntervalSeconds = 30f;
    private const float FatiguePopupIntervalMinutes = 1f; //Исправить на 10

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FatigueComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SleepingComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    public override void Update(float frameTime)
    {
        foreach (var fatigueComponent in EntityManager.EntityQuery<FatigueComponent>())
        {
            if (fatigueComponent.IsSleeping)
            {
                if ((_gameTiming.CurTime - fatigueComponent.LastRecoverTime).TotalSeconds >= RecoverIntervalSeconds)
                {
                    RecoverFatigue(fatigueComponent.Owner);
                    _fatigueMovementSpeedSystem.UpdateMovementSpeed(fatigueComponent.Owner, fatigueComponent);
                    _fatigueSystem.SetStaminaAlert(fatigueComponent.Owner, fatigueComponent);
                    _fatigueSystem.CheckAndUpdateNearsighted(fatigueComponent.Owner, fatigueComponent);
                    fatigueComponent.LastRecoverTime = _gameTiming.CurTime;
                }
            }
            else
            {
                if ((_gameTiming.CurTime - fatigueComponent.LastDecreaseTime).TotalMinutes >= DecreaseIntervalMinutes)
                {
                    DecreaseFatigue(fatigueComponent.Owner);
                    _fatigueMovementSpeedSystem.UpdateMovementSpeed(fatigueComponent.Owner, fatigueComponent);
                    _fatigueSystem.SetStaminaAlert(fatigueComponent.Owner, fatigueComponent);
                    _fatigueSystem.CheckAndUpdateNearsighted(fatigueComponent.Owner, fatigueComponent);
                    fatigueComponent.LastDecreaseTime = _gameTiming.CurTime;
                }
            }
            if ((_gameTiming.CurTime - fatigueComponent.LastPopupTime).TotalMinutes >= FatiguePopupIntervalMinutes)
            {
                if (fatigueComponent.CurrentFatigue <= 60 && fatigueComponent.CurrentFatigue > 45)
                {
                    var message = _random.Pick(fatigueComponent.FatigueMessagesTypeOne);
                    _popupSystem.PopupCursor(Loc.GetString(message), fatigueComponent.Owner, PopupType.Small);
                    fatigueComponent.LastPopupTime = _gameTiming.CurTime;
                    Log.Info($"Для {fatigueComponent.Owner} показано сообщение первого типа: {message}");
                }
                else if (fatigueComponent.CurrentFatigue <= 45 && fatigueComponent.CurrentFatigue > 20)
                {
                    var message = _random.Pick(fatigueComponent.FatigueMessagesTypeTwo);
                    _popupSystem.PopupCursor(Loc.GetString(message), fatigueComponent.Owner, PopupType.Medium);
                    fatigueComponent.LastPopupTime = _gameTiming.CurTime;
                    Log.Info($"Для {fatigueComponent.Owner} показано сообщение второго типа: {message}");
                }
                else if (fatigueComponent.CurrentFatigue <= 20 && fatigueComponent.CurrentFatigue > 0)
                {
                    var message = _random.Pick(fatigueComponent.FatigueMessagesTypeThree);
                    _popupSystem.PopupCursor(Loc.GetString(message), fatigueComponent.Owner, PopupType.Large);
                    fatigueComponent.LastPopupTime = _gameTiming.CurTime;
                    Log.Info($"Для {fatigueComponent.Owner} показано сообщение третьего типа: {message}");
                }
            }
        }
    }

    private void OnComponentStartup(EntityUid uid, FatigueComponent component, ComponentStartup args)
    {
        if (HasComp<CanHostGuardianComponent>(uid))
        {
            var minFatigue = 15;
            component.CurrentFatigue = _random.Next(minFatigue, component.MaxFatigue + 1);
            Log.Info($"Для {uid} было выбрано значение усталости: {component.CurrentFatigue}.");
            component.LastDecreaseTime = _gameTiming.CurTime;
            _fatigueSystem.SetStaminaAlert(uid, component);
            Timer.Spawn(10000, () => CheckerBaseSystem(uid));
        }
        else
        {
            RemComp<FatigueComponent>(uid);
            Log.Warning($"У сущности {uid} отсутствует компонент CanHostGuardianComponent, поэтому компонент усталости были удалены.");
        }
    }

    private void SetStaminaAlert(EntityUid uid, FatigueComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Deleted)
        {
            Log.Info($"Для {uid} были удалены все типы усталости потому что компонент был удалён.");
            _alerts.ClearAlertCategory(uid, AlertCategory.Fatigue);
            return;
        }

        var fatigueLevel = component.CurrentFatigue;
        AlertType alertType;

        if (fatigueLevel >= 60)
            alertType = AlertType.Fatigue1;
        else if (fatigueLevel >= 45)
            alertType = AlertType.Fatigue2;
        else if (fatigueLevel >= 20)
            alertType = AlertType.Fatigue3;
        else if (fatigueLevel >= 1)
            alertType = AlertType.Fatigue4;
        else
            alertType = AlertType.Fatigue5;

        _alerts.ShowAlert(uid, alertType);
    }

    private void CheckerBaseSystem(EntityUid uid)
    {
        if (!Deleted(uid) && TryComp<FatigueComponent>(uid, out var fatigueComp))
        {
            _fatigueMovementSpeedSystem.UpdateMovementSpeed(uid, fatigueComp);
            Log.Info($"Для игрока {uid} была вызвана проверка метода UpdateMovementSpeed.");

            if (HasComp<NearsightedComponent>(uid))
            {
                fatigueComp.HasNearsightedComponent = true;
                Log.Info($"Для игрока {uid} было назначено HasNearsightedComponent true.");
            }
            else
            {
                Log.Info($"Игрок {uid} не имеет компонента NearsightedComponent.");
            }

            _fatigueSystem.CheckAndUpdateNearsighted(uid, fatigueComp);
        }
    }

    private void CheckAndUpdateNearsighted(EntityUid uid, FatigueComponent fatigueComponent)
    {
        if (fatigueComponent.HasNearsightedComponent)
        {
            Log.Info($"Игрок {uid} уже имеет компонент затемнения зрения от игры, выходим из метода добавления компонента усталости.");
            return;
        }

        if (fatigueComponent.CurrentFatigue <= 45 && !fatigueComponent.NearsightedAddedBySystem && !fatigueComponent.HasNearsightedComponent)
        {
            if (!EntityManager.HasComponent<NearsightedComponent>(uid))
            {
                AddComp<NearsightedComponent>(uid);
                fatigueComponent.NearsightedAddedBySystem = true;
                Log.Info($"Для {uid} был добавлен компонент затемнения зрения потому что он имеет очков усталости меньше 45");
            }
        }
        else if (fatigueComponent.CurrentFatigue > 45 && fatigueComponent.NearsightedAddedBySystem && !fatigueComponent.HasNearsightedComponent)
        {
            if (EntityManager.HasComponent<NearsightedComponent>(uid))
            {
                RemComp<NearsightedComponent>(uid);
                fatigueComponent.NearsightedAddedBySystem = false;
                Log.Info($"Для {uid} был удалён компонент затемнения зрения потому что он имеет очков усталости больше 45");
            }
        }
    }

    private void OnSleepStateChanged(EntityUid uid, SleepingComponent component, SleepStateChangedEvent args)
    {
        if (!TryComp<FatigueComponent>(uid, out var fatigueComp))
            return;

        if (args.FellAsleep)
        {
            fatigueComp.IsSleeping = true;
            Log.Info($"{uid} имеет IsSleeping со значением true.");
            fatigueComp.LastRecoverTime = _gameTiming.CurTime;
        }
        else
        {
            fatigueComp.IsSleeping = false;
            Log.Info($"{uid} имеет IsSleeping со значением false.");
            fatigueComp.LastDecreaseTime = _gameTiming.CurTime;
        }
    }
    private void DecreaseFatigue(EntityUid uid)
    {
        if (Deleted(uid))
            return;

        if (TryComp<FatigueComponent>(uid, out var fatigueComp))
        {
            var oldFatigue = fatigueComp.CurrentFatigue;
            fatigueComp.CurrentFatigue -= 3;

            if (fatigueComp.CurrentFatigue < 0)
            {
                Log.Warning($"Усталость для {uid} вышла за пределы 0, новое значение возвращено к 0.");
                fatigueComp.CurrentFatigue = 0;
            }
            else
            {
                Log.Info($"Усталость для {uid} уменьшена с {oldFatigue} до {fatigueComp.CurrentFatigue}.");
            }
        }
        else
        {
            Log.Error($"Попытка уменьшить утомляемость была совершена для {uid}, но FatigueComponent не найден.");
        }
    }

    private void RecoverFatigue(EntityUid uid)
    {
        if (Deleted(uid))
            return;

        if (TryComp<FatigueComponent>(uid, out var fatigueComp))
        {
            var oldFatigue = fatigueComp.CurrentFatigue;
            fatigueComp.CurrentFatigue += 1;

            if (fatigueComp.CurrentFatigue > 100)
            {
                Log.Warning($"Усталость для {uid} вышла за пределы 100, новое значение возвращено к 100.");
                fatigueComp.CurrentFatigue = 100;
            }
            else
            {
                Log.Info($"Усталость для {uid} восстановлена с {oldFatigue} до {fatigueComp.CurrentFatigue}.");
            }
        }
        else
        {
            Log.Error($"Попытка восстановить усталость была совершена для {uid}, но FatigueComponent не найден.");
        }
    }
}
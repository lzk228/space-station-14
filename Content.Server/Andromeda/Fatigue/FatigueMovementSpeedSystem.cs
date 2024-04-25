using Content.Server.Andromeda.Fatigue;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Andromeda.Lemird.Fatigue;

public sealed class FatigueMovementSpeedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

    public void UpdateMovementSpeed(EntityUid uid, FatigueComponent fatigueComp, MovementSpeedModifierComponent? moveMod = null)
    {
        if (!Resolve(uid, ref moveMod))
            return;

        var fatigueLevel = fatigueComp.CurrentFatigue;
        if (fatigueLevel <= 45 && fatigueLevel > 0)
        {
            if (!fatigueComp.SpeedReduced)
            {
                fatigueComp.OriginalWalkSpeed = moveMod.BaseWalkSpeed;
                fatigueComp.OriginalSprintSpeed = moveMod.BaseSprintSpeed;

                var newWalkSpeed = fatigueComp.OriginalWalkSpeed * 0.6f;
                var newSprintSpeed = fatigueComp.OriginalSprintSpeed * 0.6f;

                _movementSpeedModifierSystem.ChangeBaseSpeed(uid, newWalkSpeed, newSprintSpeed, moveMod.Acceleration);

                fatigueComp.SpeedReduced = true;
            }
        }
        else if (fatigueLevel > 45 && fatigueComp.SpeedReduced)
        {
            _movementSpeedModifierSystem.ChangeBaseSpeed(uid, fatigueComp.OriginalWalkSpeed, fatigueComp.OriginalSprintSpeed, moveMod.Acceleration);

            fatigueComp.SpeedReduced = false;
        }
    }
}
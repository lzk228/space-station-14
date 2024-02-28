using Content.Shared.Input;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.GameStates;
using Content.Shared.CombatMode.Pacification; // A-13

namespace Content.Client.Shuttles.Systems
{
    public sealed class ShuttleConsoleSystem : SharedShuttleConsoleSystem
    {
        [Dependency] private readonly IInputManager _input = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PilotComponent, ComponentHandleState>(OnHandleState);
            var shuttle = _input.Contexts.New("shuttle", "common");
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeUp);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeDown);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeLeft);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleStrafeRight);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleRotateLeft);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleRotateRight);
            shuttle.AddFunction(ContentKeyFunctions.ShuttleBrake);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _input.Contexts.Remove("shuttle");
        }

        protected override void HandlePilotShutdown(EntityUid uid, PilotComponent component, ComponentShutdown args)
        {
            base.HandlePilotShutdown(uid, component, args);
            if (_playerManager.LocalEntity != uid) return;

            _input.Contexts.SetActiveContext("human");
        }

        private void OnHandleState(EntityUid uid, PilotComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not PilotComponentState state) return;

            // A-13 WIP EblanComponent
            if (HasComp<EblanComponent>(uid))
                //var data = await _locator.LookupIdByNameOrIdAsync(args[0]);

                // (Обнаружена подозрительная активность. Код 001. Если вы считаете, что получили бан по ошибке, напишите обжалование в нашем Discord-канале)
                //shell.ExecuteCommand($"ban {data.Username} Code-001 1 high ");
                return;
            // A-13 WIP EblanComponent

            var console = EnsureEntity<PilotComponent>(state.Console, uid);

            if (console == null)
            {
                component.Console = null;
                _input.Contexts.SetActiveContext("human");
                return;
            }

            if (!HasComp<ShuttleConsoleComponent>(console))
            {
                Log.Warning($"Unable to set Helmsman console to {console}");
                return;
            }

            component.Console = console;
            ActionBlockerSystem.UpdateCanMove(uid);
            _input.Contexts.SetActiveContext("shuttle");
        }
    }
}

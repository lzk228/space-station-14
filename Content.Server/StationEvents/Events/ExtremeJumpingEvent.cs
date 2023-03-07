using System.Linq;
using Content.Server.Plants.Components;
using Content.Server.Storage.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Construction.Components;
using Content.Shared.VendingMachines;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.StationEvents.Events
{
    public sealed class ExtremeJumpingEvent : StationEventSystem
    {
        private struct EntityData
        {
            public EntityUid Uid;
            public BodyType BodyType;
            public BodyStatus BodyStatus;
            public List<float> Restitutions;
        }

        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly FixtureSystem _fixtures = default!;
        public override string Prototype => "ExtremeJumpingEvent";

        private readonly List<EntityData> _affectedEntities = new();
        private readonly List<EntityData> _selectedEntities = new();

        private readonly int _maxNumberOfAffectedEntities = 20;
        private readonly float _updateRate = 2.0f;

        private float _frameTimeAccumulator = 0.0f;
        private float _endAfter = 0.0f;

        public override void Added()
        {
            base.Added();
            _endAfter = RobustRandom.Next(150, 180);
        }

        public override void Started()
        {
            var entities = new List<Component>();
            entities.AddRange(EntityQuery<EntityStorageComponent, AnchorableComponent>(true).Select(x => x.Item1).ToList());
            entities.AddRange(EntityQuery<StrapComponent>(true));
            entities.AddRange(EntityQuery<PottedPlantHideComponent>(true));
            entities.AddRange(EntityQuery<VendingMachineComponent>(true));
            var numberOfEntities = Math.Min(_maxNumberOfAffectedEntities, entities.Count);

            for (var i = 0; i < numberOfEntities; i++)
            {
                var esComponent = RobustRandom.Pick(entities);
                var target = esComponent.Owner;

                if (EntityManager.TryGetComponent<PhysicsComponent>(target, out var physicsComponent))
                {
                    var entityData = new EntityData
                    {
                        Uid = target,
                        BodyType = physicsComponent.BodyType,
                        BodyStatus = physicsComponent.BodyStatus,
                        Restitutions = new List<float>()
                    };

                    _selectedEntities.Add(entityData);
                }
            }

            base.Started();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!RuleStarted)
                return;

            if (Elapsed > _endAfter)
            {
                ForceEndSelf();
            }

            var updates = 0;
            _frameTimeAccumulator += frameTime;
            if (_frameTimeAccumulator > _updateRate)
            {
                updates = (int) (_frameTimeAccumulator / _updateRate);
                _frameTimeAccumulator -= _updateRate * updates;
            }

            for (var i = 0; i < updates; i++)
            {
                if (_selectedEntities.Count == 0)
                    break;

                var selected = _selectedEntities.Pop();
                if (EntityManager.Deleted(selected.Uid))
                    continue;

                if (EntityManager.TryGetComponent<PhysicsComponent>(selected.Uid, out var physicsComponent))
                {
                    var xform = Transform(selected.Uid);
                    var fixtures = Comp<FixturesComponent>(selected.Uid);
                    xform.Anchored = false;

                    _physics.SetBodyType(selected.Uid, BodyType.Dynamic, manager: fixtures, body: physicsComponent);
                    _physics.SetBodyStatus(physicsComponent, BodyStatus.InAir);
                    _physics.WakeBody(selected.Uid, manager: fixtures, body: physicsComponent);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        selected.Restitutions.Add(fixture.Restitution);
                        var restitution = 1.1f / (physicsComponent.Mass / 10f);
                        _physics.SetRestitution(selected.Uid, fixture, restitution, false, fixtures);
                    }

                    _fixtures.FixtureUpdate(selected.Uid, manager: fixtures, body: physicsComponent);

                    _physics.SetLinearVelocity(selected.Uid, RobustRandom.NextVector2(3.5f, 3.5f), manager: fixtures, body: physicsComponent);
                    _physics.SetAngularVelocity(selected.Uid, MathF.PI * 12, manager: fixtures, body: physicsComponent);
                    _physics.SetLinearDamping(physicsComponent, 0f);
                    _physics.SetAngularDamping(physicsComponent, 0f);

                    _affectedEntities.Add(selected);
                }
            }
        }

        public override void Ended()
        {
            foreach (var entityData in _affectedEntities)
            {
                if (EntityManager.Deleted(entityData.Uid))
                    continue;

                if (EntityManager.TryGetComponent<PhysicsComponent>(entityData.Uid, out var physicsComponent))
                {
                    var fixtures = Comp<FixturesComponent>(entityData.Uid);
                    _physics.SetBodyType(entityData.Uid, entityData.BodyType, manager: fixtures, body: physicsComponent);
                    _physics.SetBodyStatus(physicsComponent, entityData.BodyStatus);
                    entityData.Restitutions.Reverse();

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        _physics.SetRestitution(entityData.Uid, fixture, entityData.Restitutions.Pop(), false, fixtures);
                    }

                    _fixtures.FixtureUpdate(entityData.Uid, manager: fixtures, body: physicsComponent);
                }
            }

            _affectedEntities.Clear();
            _selectedEntities.Clear();

            base.Ended();
        }
    }
}

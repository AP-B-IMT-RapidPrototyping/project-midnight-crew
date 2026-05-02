using Godot;
using System;

public partial class NPC : CharacterBody3D
{
    [Export] public float Speed = 4.0f;
    [Export] public float WanderRange = 50.0f;
    [Export] public float MinWanderDistance = 4.0f; // 🔥 nieuw
    [Export] public float RotationSpeed = 6.0f;

    private NavigationAgent3D _agent;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private float _gravity;

    private Vector3 _lastPosition;
    private float _stuckTimer = 0f;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        _agent.TargetDesiredDistance = 0.8f;

        _rng.Randomize();
        _lastPosition = GlobalPosition;

        GetTree().CreateTimer(0.1f).Timeout += PickNewTarget;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Zwaartekracht
        if (!IsOnFloor())
            velocity.Y -= _gravity * (float)delta;
        else
            velocity.Y = 0;

        // STUCK DETECTION
        float movedDistance = GlobalPosition.DistanceTo(_lastPosition);

        if (movedDistance < 0.05f)
            _stuckTimer += (float)delta;
        else
            _stuckTimer = 0f;

        _lastPosition = GlobalPosition;

        if (_stuckTimer > 2.0f)
        {
            PickNewTarget();
        }

        // NIEUW TARGET ALS BIJNA AANGEKOMEN
        float distanceToTarget = GlobalPosition.DistanceTo(_agent.TargetPosition);

        if (distanceToTarget < 1.0f)
        {
            PickNewTarget();
        }

        // Beweging
        Vector3 nextPos = _agent.GetNextPathPosition();
        Vector3 direction = (nextPos - GlobalPosition).Normalized();

        velocity.X = direction.X * Speed;
        velocity.Z = direction.Z * Speed;

        // ROTATIE (alleen Y)
        Vector3 flatDirection = nextPos - GlobalPosition;
        flatDirection.Y = 0;

        if (flatDirection.Length() > 0.1f)
        {
            flatDirection = flatDirection.Normalized();

            float targetAngle = Mathf.Atan2(flatDirection.X, flatDirection.Z);

            // 👉 uncomment als model achteruit kijkt
            // targetAngle += Mathf.Pi;

            Rotation = new Vector3(
                Rotation.X,
                Mathf.LerpAngle(Rotation.Y, targetAngle, RotationSpeed * (float)delta),
                Rotation.Z
            );
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void PickNewTarget()
    {
        _stuckTimer = 0f;

        var map = _agent.GetNavigationMap();

        for (int i = 0; i < 15; i++) // iets meer pogingen
        {
            Vector2 random2D = new Vector2(
                _rng.RandfRange(-1f, 1f),
                _rng.RandfRange(-1f, 1f)
            );

            if (random2D.Length() < 0.1f)
                continue;

            random2D = random2D.Normalized() * _rng.RandfRange(MinWanderDistance, WanderRange);

            Vector3 candidate = GlobalPosition + new Vector3(random2D.X, 0, random2D.Y);

            Vector3 navPoint = NavigationServer3D.MapGetClosestPoint(map, candidate);

            float distance = GlobalPosition.DistanceTo(navPoint);

            // 🔥 BELANGRIJK: minimum afstand check
            if (candidate.DistanceTo(navPoint) < 2.0f && distance > MinWanderDistance)
            {
                _agent.TargetPosition = navPoint;
                return;
            }
        }

        // fallback (ver weg proberen)
        Vector3 fallback = GlobalPosition + new Vector3(
            _rng.RandfRange(-WanderRange, WanderRange),
            0,
            _rng.RandfRange(-WanderRange, WanderRange)
        );

        _agent.TargetPosition = NavigationServer3D.MapGetClosestPoint(map, fallback);
    }
}
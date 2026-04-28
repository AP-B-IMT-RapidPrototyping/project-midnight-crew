using Godot;
using System;

public partial class NPC : CharacterBody3D
{
    [ExportGroup("Beweging")]
    [Export] public float MaxSpeed = 5.0f; // Iets lager gezet voor realisme
    [Export] public float RotationSpeed = 10.0f;
    [Export] public float WanderRange = 10.0f;
    [Export] public float SlowingDistance = 1.5f;

    [ExportGroup("Rust")]
    [Export] public float MinWait = 0f;
    [Export] public float MaxWait = 0.01f;

    private NavigationAgent3D _navAgent;
    private bool _isWaiting = false;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        // SIGNALEN
        _navAgent.VelocityComputed += OnVelocityComputed;

        // Zorg dat de agent niet te kieskeurig is over de exacte eindpositie
        _navAgent.TargetDesiredDistance = 0.5f;
        _navAgent.PathDesiredDistance = 0.5f;

        _rng.Randomize();

        // Start na een kleine vertraging om de navigatie-map de tijd te geven om te laden
        GetTree().CreateTimer(0.1f).Timeout += () => PickNewTarget();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 currentVelocity = Velocity;

        // 1. Zwaartekracht
        if (!IsOnFloor())
        {
            currentVelocity.Y -= _gravity * (float)delta;
        }
        else
        {
            currentVelocity.Y = 0;
        }

        // 2. Navigatie check
        if (!_isWaiting)
        {
            if (_navAgent.IsNavigationFinished())
            {
                StartWaiting();
                return;
            }

            Vector3 nextPathPos = _navAgent.GetNextPathPosition();
            Vector3 direction = (nextPathPos - GlobalPosition).Normalized();
            float distance = GlobalPosition.DistanceTo(_navAgent.TargetPosition);

            // Rotatie
            Vector3 lookDirection = new Vector3(direction.X, 0, direction.Z);
            if (lookDirection.Length() > 0.01f)
            {
                Basis targetBasis = Basis.LookingAt(-lookDirection);
                Transform = Transform.Orthonormalized();
                Transform3D newTransform = Transform;
                newTransform.Basis = Transform.Basis.Slerp(targetBasis, (float)delta * RotationSpeed);
                Transform = newTransform;
            }

            // Snelheid berekenen
            float currentSpeed = MaxSpeed;
            if (distance < SlowingDistance)
            {
                currentSpeed = Mathf.Lerp(0.1f, MaxSpeed, distance / SlowingDistance);
            }

            Vector3 desiredVelocity = direction * currentSpeed;

            // Alleen doorgeven als we nog niet klaar zijn
            _navAgent.Velocity = desiredVelocity;

            // Behoud Y velocity voor gravity
            Velocity = new Vector3(Velocity.X, currentVelocity.Y, Velocity.Z);
        }
        else
        {
            // Stilstand tijdens wachten
            Velocity = new Vector3(0, currentVelocity.Y, 0);
            MoveAndSlide();
        }
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        if (_isWaiting) return;

        Velocity = new Vector3(safeVelocity.X, Velocity.Y, safeVelocity.Z);
        MoveAndSlide();
    }

    private void PickNewTarget()
    {
        _isWaiting = false;

        // Kies een punt in de buurt
        Vector3 randomPos = GlobalPosition + new Vector3(
            _rng.RandfRange(-WanderRange, WanderRange),
            0,
            _rng.RandfRange(-WanderRange, WanderRange)
        );

        // BELANGRIJK: Projecteer het punt op de navigatie-vloer
        // Dit voorkomt dat de NPC naar plekken wil waar hij niet kan komen
        _navAgent.TargetPosition = NavigationServer3D.MapGetClosestPoint(_navAgent.GetNavigationMap(), randomPos);
    }

    private async void StartWaiting()
    {
        if (_isWaiting) return;
        _isWaiting = true;

        float waitTime = _rng.RandfRange(MinWait, MaxWait);

        // Gebruik een timer maar check of de NPC nog bestaat na afloop
        await ToSignal(GetTree().CreateTimer(waitTime), SceneTreeTimer.SignalName.Timeout);

        if (IsInsideTree())
        {
            PickNewTarget();
        }
    }
}
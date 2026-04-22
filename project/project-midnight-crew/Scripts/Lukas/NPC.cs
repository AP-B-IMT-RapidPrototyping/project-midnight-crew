using Godot;
using System;

public partial class NPC : CharacterBody3D
{
    [ExportGroup("Beweging")]
    [Export] public float MaxSpeed = 20.0f;
    [Export] public float RotationSpeed = 15.0f;
    [Export] public float WanderRange = 10.0f;
    [Export] public float SlowingDistance = 1.5f;

    [ExportGroup("Rust")]
    [Export] public float MinWait = 0.0f;
    [Export] public float MaxWait = 0.0f;

    private NavigationAgent3D _navAgent;
    private bool _isWaiting = false;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    public override void _Ready()
    {
        // Zoek de NavigationAgent3D node
        _navAgent = GetNode<NavigationAgent3D>("NavigationAgent3D");

        // Koppeling van het signaal (gecorrigeerd voor C#)
        _navAgent.VelocityComputed += (safeVelocity) => OnVelocityComputed(safeVelocity);

        // Start het proces
        Callable.From(PickNewTarget).CallDeferred();
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

        // 2. Beweging en Rotatie
        if (!_isWaiting && !_navAgent.IsNavigationFinished())
        {
            Vector3 nextPathPos = _navAgent.GetNextPathPosition();
            Vector3 direction = (nextPathPos - GlobalPosition).Normalized();
            float distance = GlobalPosition.DistanceTo(_navAgent.TargetPosition);

            // --- ROTATIE MET FIX ---
            Vector3 lookDirection = new Vector3(direction.X, 0, direction.Z);
            float rotationFactor = 0.2f;

            if (lookDirection.Length() > 0.01f)
            {
                // Orthonormalized() voorkomt de "Quaternion not normalized" error
                Basis cleanBasis = Transform.Basis.Orthonormalized();
                Basis targetBasis = Basis.LookingAt(-lookDirection);

                Basis newBasis = cleanBasis.Slerp(targetBasis, (float)delta * RotationSpeed);

                Transform3D newTransform = Transform;
                newTransform.Basis = newBasis;
                Transform = newTransform;

                // Bereken hoe goed de NPC in de juiste richting kijkt
                rotationFactor = Mathf.Max(0.2f, -newBasis.Z.Dot(lookDirection));
            }

            // --- VLOEIENDE VERTRAGING ---
            float currentSpeed = MaxSpeed;
            if (distance < SlowingDistance)
            {
                currentSpeed = Mathf.Lerp(0.1f, MaxSpeed, distance / SlowingDistance);
            }

            // Combineer alles tot de gewenste snelheid
            Vector3 desiredVelocity = direction * currentSpeed * rotationFactor;

            // Geef door aan de agent voor avoidance
            _navAgent.Velocity = desiredVelocity;

            // Update alleen de Y-as in de directe Velocity (voor MoveAndSlide in de callback)
            Velocity = new Vector3(Velocity.X, currentVelocity.Y, Velocity.Z);
        }
        else
        {
            // Stop langzaam als er niet gelopen wordt
            currentVelocity.X = Mathf.MoveToward(Velocity.X, 0, MaxSpeed * (float)delta);
            currentVelocity.Z = Mathf.MoveToward(Velocity.Z, 0, MaxSpeed * (float)delta);
            Velocity = currentVelocity;
            MoveAndSlide();
        }
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        // Belangrijk: combineer de 'veilige' snelheid met onze zwaartekracht
        float currentY = Velocity.Y;
        Vector3 finalVelocity = safeVelocity;
        finalVelocity.Y = currentY;

        Velocity = finalVelocity;
        MoveAndSlide();

        // Check of we het doel bereikt hebben
        if (_navAgent.IsNavigationFinished() && !_isWaiting)
        {
            StartWaiting();
        }
    }

    private void PickNewTarget()
    {
        _isWaiting = false;

        Vector3 randomPos = new Vector3(
            _rng.RandfRange(-WanderRange, WanderRange),
            0,
            _rng.RandfRange(-WanderRange, WanderRange)
        );

        _navAgent.TargetPosition = GlobalPosition + randomPos;
    }

    private async void StartWaiting()
    {
        _isWaiting = true;

        float waitTime = _rng.RandfRange(MinWait, MaxWait);
        await ToSignal(GetTree().CreateTimer(waitTime), SceneTreeTimer.SignalName.Timeout);

        if (IsInsideTree())
        {
            PickNewTarget();
        }
    }
}
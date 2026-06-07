using Godot;
using System;

public partial class NPCEnd : CharacterBody3D
{
    [Export] public float Speed = 4.0f;
    [Export] public float RotationSpeed = 6.0f;

    // 🔥 Sleep hier in de editor de Node3D naartoe waar de NPC naartoe moet lopen
    [Export] public Node3D DoelTargetNode;

    private NavigationAgent3D _agent;
    private float _gravity;

    private Vector3 _lastPosition;
    private float _stuckTimer = 0f;
    private bool _doelBereikt = false;

    // 🔥 Houdt de veilige snelheid bij die Godot berekent om om anderen heen te lopen
    private Vector3 _berekendeVelocity = Vector3.Zero;

    // 🔥 NIEUW: Zorgt ervoor dat we pas gaan bewegen als de route écht klopt
    private bool _magBewegen = false;
    private int _safeFramesCount = 0;

    public override void _Ready()
    {
        _agent = GetNode<NavigationAgent3D>("NavigationAgent3D");
        _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        _agent.TargetDesiredDistance = 0.8f;
        _lastPosition = GlobalPosition;

        // Connect het avoidance signaal
        _agent.VelocityComputed += OnVelocityComputed;

        // 🔥 OPLOSSING: Bereken de route NIET direct, maar wacht tot het allereerste physics frame klaar is.
        // Dit geeft de NavigationServer3D de tijd om te synchroniseren.
        Callable.From(ActorReady).CallDeferred();
    }

    private void ActorReady()
    {
        // Nu is de map 100% zeker gesynchroniseerd en veilig te bevragen!
        UpdateRouteNaarDoel();
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Zwaartekracht werkt altijd, zodat ze niet gaan zweven tijdens het laden
        if (!IsOnFloor())
            velocity.Y -= _gravity * (float)delta;
        else
            velocity.Y = 0;

        // 🔥 HARD RESET TIJDENS DE EERSTE 5 FRAMES 🔥
        // Dit negeert de foute beginkoers naar (0,0,0) van de NavigationAgent3D volledig
        if (!_magBewegen)
        {
            _safeFramesCount++;
            if (_safeFramesCount > 5)
            {
                UpdateRouteNaarDoel(); // Forceer nog één keer de échte route
                _magBewegen = true;
            }

            // Houd de NPC volledig stil op de X- en Z-as
            velocity.X = 0;
            velocity.Z = 0;
            Velocity = velocity;
            MoveAndSlide();
            return;
        }

        // Als we geen doel hebben, of we zijn er al, hoeven we niet te lopen
        if (DoelTargetNode == null || _doelBereikt)
        {
            velocity.X = 0;
            velocity.Z = 0;
            Velocity = velocity;
            MoveAndSlide();
            return;
        }

        // STUCK DETECTION (Als hij klem staat tegen een muurtje)
        float movedDistance = GlobalPosition.DistanceTo(_lastPosition);
        if (movedDistance < 0.05f)
            _stuckTimer += (float)delta;
        else
            _stuckTimer = 0f;

        _lastPosition = GlobalPosition;

        if (_stuckTimer > 2.0f)
        {
            UpdateRouteNaarDoel();
        }

        // CHECK OF WE ER ZIJN
        float distanceToTarget = GlobalPosition.DistanceTo(_agent.TargetPosition);
        if (distanceToTarget < 1.0f)
        {
            _doelBereikt = true;
            GD.Print($"{Name} heeft het eindpunt bereikt!");
            velocity.X = 0;
            velocity.Z = 0;
            Velocity = velocity;
            MoveAndSlide();
            return;
        }

        // Beweging richting het volgende punt op de NavMesh
        Vector3 nextPos = _agent.GetNextPathPosition();
        Vector3 direction = (nextPos - GlobalPosition).Normalized();

        // Dit is de snelheid die we *willen* hebben (de gewenste snelheid)
        Vector3 gewensteVelocity = direction * Speed;

        // 🔥 VERTEL DE NAVIGATIONAGENT WAT ONZE WENS IS 🔥
        if (_agent.AvoidanceEnabled)
        {
            _agent.Velocity = gewensteVelocity;
        }
        else
        {
            _berekendeVelocity = gewensteVelocity;
        }

        // Pas de veilige, berekende X en Z toe
        velocity.X = _berekendeVelocity.X;
        velocity.Z = _berekendeVelocity.Z;

        // ROTATIE (Alleen draaien als we ook echt mogen bewegen en vaart hebben)
        Vector3 flatDirection = velocity;
        flatDirection.Y = 0;

        if (flatDirection.Length() > 0.1f)
        {
            flatDirection = flatDirection.Normalized();
            float targetAngle = Mathf.Atan2(flatDirection.X, flatDirection.Z);

            Rotation = new Vector3(
                Rotation.X,
                Mathf.LerpAngle(Rotation.Y, targetAngle, RotationSpeed * (float)delta),
                Rotation.Z
            );
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void OnVelocityComputed(Vector3 safeVelocity)
    {
        // Alleen de berekende snelheid aannemen als de opstartfase voorbij is
        if (_magBewegen)
        {
            _berekendeVelocity = safeVelocity;
        }
    }

    private void UpdateRouteNaarDoel()
    {
        if (DoelTargetNode != null && IsInsideTree())
        {
            _stuckTimer = 0f;
            _doelBereikt = false;

            var map = _agent.GetNavigationMap();
            Vector3 navPoint = NavigationServer3D.MapGetClosestPoint(map, DoelTargetNode.GlobalPosition);

            _agent.TargetPosition = navPoint;
        }
    }

    public void VeranderDoelPunt(Node3D nieuwDoel)
    {
        DoelTargetNode = nieuwDoel;
        UpdateRouteNaarDoel();
    }
}
using Godot;
using System;

public partial class SniperShot : Node3D
{
    [Export] public float Range = 200.0f;
    [Export] public float ShootCooldown = 1.0f;

    [ExportGroup("Recoil Instellingen")]
    [Export] public float RecoilAmount = 0.4f;
    [Export] public float RecoilTime = 0.12f;

    [ExportGroup("Debug Visueel")]
    [Export] public bool ShowRaycast = true;
    [Export] public Color RayColor = new Color(1, 0, 0, 0.5f);
    [Export] public float RayDuration = 0.1f;
    [Export] public float RayThickness = 0.01f;

    private Node3D _barrelLoc;
    private AudioStreamPlayer3D _shootSound;
    private AudioStreamPlayer3D _chamberSound; // NIEUW
    private bool _canShoot = true;

    public override void _Ready()
    {
        _barrelLoc = GetNode<Node3D>("BarrelLoc");
        _shootSound = GetNode<AudioStreamPlayer3D>("ShootSound");
        _chamberSound = GetNode<AudioStreamPlayer3D>("ChamberBullet"); // NIEUW
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("shoot") && _canShoot)
        {
            Shoot();
        }
    }

    private async void Shoot()
    {
        _canShoot = false;

        if (_shootSound != null) _shootSound.Play();

        ApplyVisualRecoil();

        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null)
        {
            _canShoot = true;
            return;
        }

        var spaceState = GetWorld3D().DirectSpaceState;
        Vector3 camStart = camera.GlobalPosition;
        Vector3 camDirection = -camera.GlobalTransform.Basis.Z;
        Vector3 camEnd = camStart + camDirection * Range;

        var camQuery = PhysicsRayQueryParameters3D.Create(camStart, camEnd);
        camQuery.CollisionMask = 1;

        Node n = GetParent();
        while (n != null && !(n is CollisionObject3D)) n = n.GetParent();
        if (n is CollisionObject3D playerCollision)
        {
            camQuery.Exclude = new Godot.Collections.Array<Rid> { playerCollision.GetRid() };
        }

        var result = spaceState.IntersectRay(camQuery);

        Vector3 barrelStart = _barrelLoc.GlobalPosition;
        Vector3 hitPoint = result.Count > 0 ? (Vector3)result["position"] : camEnd;

        if (ShowRaycast)
        {
            DebugDrawLine(barrelStart, hitPoint);
        }

        if (result.Count > 0)
        {
            Node hitObject = (Node)result["collider"];
            if (hitObject.IsInGroup("NPC"))
            {
                GD.Print($"Target geëlimineerd: {hitObject.Name}");
                hitObject.QueueFree();
            }
        }

        // --- NIEUW: CHAMBER SOUND ---
        // We wachten een klein beetje (bijv. 0.3s) na het schot voordat we de grendel horen
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        if (_chamberSound != null) _chamberSound.Play();

        // --- COOLDOWN WACHTEN ---
        // We trekken de 0.3s die we hierboven al gewacht hebben af van de totale cooldown
        float remainingCooldown = Mathf.Max(0.1f, ShootCooldown - 0.5f);
        await ToSignal(GetTree().CreateTimer(remainingCooldown), SceneTreeTimer.SignalName.Timeout);

        _canShoot = true;
    }

    // ... rest van de functies (ApplyVisualRecoil en DebugDrawLine) blijven hetzelfde ...
    private void ApplyVisualRecoil()
    {
        Node n = GetParent();
        while (n != null && !(n is PlayerScript)) n = n.GetParent();
        if (n is PlayerScript player)
        {
            player.ApplyRecoil(RecoilAmount, RecoilTime);
        }
    }

    private void DebugDrawLine(Vector3 start, Vector3 end)
    {
        MeshInstance3D meshInstance = new MeshInstance3D();
        BoxMesh boxMesh = new BoxMesh();
        float distance = start.DistanceTo(end);
        boxMesh.Size = new Vector3(RayThickness, RayThickness, distance);
        meshInstance.Mesh = boxMesh;
        StandardMaterial3D material = new StandardMaterial3D();
        material.AlbedoColor = RayColor;
        material.EmissionEnabled = true;
        material.Emission = RayColor;
        material.Transparency = StandardMaterial3D.TransparencyEnum.Alpha;
        material.NoDepthTest = true;
        material.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
        meshInstance.MaterialOverride = material;
        GetTree().Root.AddChild(meshInstance);
        meshInstance.LookAtFromPosition(start, end);
        meshInstance.TranslateObjectLocal(new Vector3(0, 0, -distance / 2.0f));
        GetTree().CreateTimer(RayDuration).Timeout += () => meshInstance.QueueFree();
    }
}
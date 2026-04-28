using Godot;
using System;

public partial class SniperShot : Node3D
{
    [Export] public float Range = 200.0f;

    [ExportGroup("Recoil Instellingen")]
    [Export] public float RecoilAmount = 2.0f; // Sterkte in graden
    [Export] public float RecoilTime = 0.08f;  // Snelheid van de kick

    [ExportGroup("Debug Visueel")]
    [Export] public bool ShowRaycast = true;
    [Export] public Color RayColor = new Color(1, 0, 0, 0.5f);
    [Export] public float RayDuration = 0.1f;
    [Export] public float RayThickness = 0.01f;

    private Node3D _barrelLoc;
    private AudioStreamPlayer3D _shootSound;

    public override void _Ready()
    {
        _barrelLoc = GetNode<Node3D>("BarrelLoc");

        // Zoek de ShootSound node op (zorg dat deze als kind onder de Sniper staat)
        _shootSound = GetNode<AudioStreamPlayer3D>("ShootSound");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("shoot"))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        // Speel het geluid direct af
        if (_shootSound != null)
        {
            _shootSound.Play();
        }

        ApplyVisualRecoil();

        // --- 1. HIT DETECTION VIA CAMERA ---
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        var spaceState = GetWorld3D().DirectSpaceState;

        Vector3 camStart = camera.GlobalPosition;
        Vector3 camDirection = -camera.GlobalTransform.Basis.Z;
        Vector3 camEnd = camStart + camDirection * Range;

        var camQuery = PhysicsRayQueryParameters3D.Create(camStart, camEnd);

        // VEILIGE EXCLUDE: We zoeken de player op om te voorkomen dat de sniper zichzelf raakt
        Node n = GetParent();
        while (n != null && !(n is CollisionObject3D))
        {
            n = n.GetParent();
        }
        if (n is CollisionObject3D playerCollision)
        {
            camQuery.Exclude = new Godot.Collections.Array<Rid> { playerCollision.GetRid() };
        }

        var result = spaceState.IntersectRay(camQuery);

        // --- 2. VISUEEL EFFECT VIA BARREL ---
        Vector3 barrelStart = _barrelLoc.GlobalPosition;
        Vector3 hitPoint = result.Count > 0 ? (Vector3)result["position"] : camEnd;

        if (ShowRaycast)
        {
            DebugDrawLine(barrelStart, hitPoint);
        }

        // --- 3. DAMAGE AFHANDELING ---
        if (result.Count > 0)
        {
            Node hitObject = (Node)result["collider"];

            if (hitObject.IsInGroup("NPC"))
            {
                GD.Print($"Target geëlimineerd: {hitObject.Name}");
                hitObject.QueueFree();
            }
        }
    }

    private void ApplyVisualRecoil()
    {
        // Zoek de PlayerScript node in de ouders
        Node n = GetParent();
        while (n != null && !(n is PlayerScript))
        {
            n = n.GetParent();
        }

        if (n is PlayerScript player)
        {
            player.ApplyRecoil(RecoilAmount, RecoilTime);
        }
    }

    private void DebugDrawLine(Vector3 start, Vector3 end)
    {
        MeshInstance3D meshInstance = new MeshInstance3D();
        BoxMesh boxMesh = new BoxMesh();
        boxMesh.Size = new Vector3(RayThickness, RayThickness, start.DistanceTo(end));
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

        meshInstance.GlobalPosition = start.Lerp(end, 0.5f);
        meshInstance.LookAt(end);

        GetTree().CreateTimer(RayDuration).Timeout += () => meshInstance.QueueFree();
    }
}
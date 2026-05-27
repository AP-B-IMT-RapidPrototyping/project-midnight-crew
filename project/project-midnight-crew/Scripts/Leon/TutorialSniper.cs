using Godot;
using System;

public partial class TutorialSniper : Node3D
{
    [Export] public float Range = 200.0f;
    [Export] public float ShootCooldown = 1.0f;

    [ExportGroup("Recoil Instellingen")]
    [Export] public float RecoilAmount = 0.4f;
    [Export] public float RecoilTime = 0.12f;
    [Export] private Label labelSlow;
    [Export] private Camera3D Camera;
    [Export] private ProgressBar slowbar;
    [Export] private Label Target;


    // UI Nodes
    private Label _failLabel;    // We noemen deze even FailLabel voor de duidelijkheid
    private Label _successLabel; // Het label voor winst
    private ColorRect _blackScreen;

    private Node3D _barrelLoc;
    private AudioStreamPlayer3D _shootSound;
    private AudioStreamPlayer3D _chamberSound;
    private bool _canShoot = true;
    private bool _isGameOver = false;

    public override void _Ready()
    {
        _barrelLoc = GetNode<Node3D>("BarrelLoc");
        _shootSound = GetNode<AudioStreamPlayer3D>("ShootSound");
        _chamberSound = GetNode<AudioStreamPlayer3D>("ChamberBullet");

        // UI opzoeken
        _failLabel = GetTree().Root.FindChild("FailLabel", true, false) as Label;
        _successLabel = GetTree().Root.FindChild("SuccesLabel", true, false) as Label;
        _blackScreen = GetTree().Root.FindChild("BlackScreen", true, false) as ColorRect;
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

        MaakMuzzleFlash();

        ApplyVisualRecoil();

        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) { _canShoot = true; return; }

        var spaceState = GetWorld3D().DirectSpaceState;
        var camDirection = -camera.GlobalTransform.Basis.Z;
        Vector3 targetEndPosition = camera.GlobalPosition + camDirection * Range; // Standaard eindpunt als we niks raken

        var camQuery = PhysicsRayQueryParameters3D.Create(camera.GlobalPosition, targetEndPosition);
        camQuery.CollisionMask = 1;

        var result = spaceState.IntersectRay(camQuery);

        if (result.Count > 0)
        {
            targetEndPosition = (Vector3)result["position"];
        }

        // MAAK DE BULLET TRAIL AAN
        MaakBulletTrail(_barrelLoc.GlobalPosition, targetEndPosition);

        if (result.Count > 0)
        {
            Node hitObject = (Node)result["collider"];

            // --- DE JUISTE TARGET GERAAKT ---
            if (hitObject.IsInGroup("Target"))
            {
                GD.Print("VOLTREFFER! Missie geslaagd.");
                TriggerMissionSuccess(); // Roep de nieuwe win-functie aan
                hitObject.QueueFree();
                return;
            }
            // --- EEN BURGER GERAAKT ---
            else if (hitObject.IsInGroup("Hittable"))
            {
                TriggerMissionFailed();
                hitObject.QueueFree();
                return;
            }
        }

        // Normale herlaad cyclus
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        if (!_isGameOver && _chamberSound != null) _chamberSound.Play();

        float remainingCooldown = Mathf.Max(0.1f, ShootCooldown - 0.5f);
        await ToSignal(GetTree().CreateTimer(remainingCooldown), SceneTreeTimer.SignalName.Timeout);
        _canShoot = true;
    }

    private void MaakMuzzleFlash()
    {
        if (_barrelLoc == null) return;

        // 1. Maak een dynamic light aan die de omgeving heel even oplicht
        OmniLight3D flashLight = new OmniLight3D();
        flashLight.LightColor = new Color(1.0f, 0.6f, 0.2f); // Oranje/gele gloed
        flashLight.LightEnergy = 8.0f;                       // Lekkere felle flits
        flashLight.OmniRange = 10.0f;                        // Bereik van het licht
        _barrelLoc.AddChild(flashLight);                     // Hang hem direct aan de loop

        // 2. Maak een klein 3D bolletje/vlammetje aan de loop
        MeshInstance3D flashMesh = new MeshInstance3D();
        SphereMesh sphere = new SphereMesh();
        sphere.Radius = 0.08f;
        sphere.Height = 0.16f; // Iets langer dan breed voor een vlamvorm
        flashMesh.Mesh = sphere;

        // Geef de vlam een gloeiend onbelicht materiaal
        StandardMaterial3D flashMat = new StandardMaterial3D();
        flashMat.AlbedoColor = new Color(1.0f, 0.8f, 0.3f);
        flashMat.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
        flashMesh.MaterialOverride = flashMat;
        _barrelLoc.AddChild(flashMesh);

        // Pas de positie aan zodat hij net iets vóór de loop zweeft
        flashMesh.Position = new Vector3(0, 0, -0.2f);

        // 3. Laat alles binnen 0.05 seconden supersnel verdwijnen via een Tween
        Tween flashTween = GetTree().CreateTween();
        flashTween.SetParallel(true); // Laat de animaties tegelijkertijd lopen

        // Dim het licht en krimp het vlammetje
        flashTween.TweenProperty(flashLight, "light_energy", 0.0f, 0.06f);
        flashTween.TweenProperty(flashMesh, "scale", Vector3.Zero, 0.06f);

        // Ruim de nodes netjes op zodra de flits voorbij is
        flashTween.Chain().TweenCallback(Callable.From(() =>
        {
            flashLight.QueueFree();
            flashMesh.QueueFree();
        }));
    }

    private void MaakBulletTrail(Vector3 start, Vector3 einde)
    {
        MeshInstance3D trail = new MeshInstance3D();
        BoxMesh boxMesh = new BoxMesh();
        boxMesh.Size = new Vector3(0.03f, 0.03f, start.DistanceTo(einde));
        trail.Mesh = boxMesh;

        StandardMaterial3D mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(1f, 0.9f, 0.4f);
        mat.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
        trail.MaterialOverride = mat;

        GetTree().Root.AddChild(trail);

        trail.GlobalPosition = (start + einde) / 2.0f;
        trail.LookAtFromPosition(trail.GlobalPosition, einde, Vector3.Up);

        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(trail, "scale", new Vector3(0f, 0f, 1f), 0.15f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);

        tween.Finished += () => trail.QueueFree();
    }

    // --- NIEUWE WIN-FUNCTIE ---
    private async void TriggerMissionSuccess()
    {
        _isGameOver = true;

        if (_successLabel != null)
        {
            _successLabel.Text = "MISSION SUCCESS";
            _successLabel.Visible = true;
        }

        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
        GD.Print("Level herladen voor een schone start...");
        Input.MouseMode = Input.MouseModeEnum.Visible;

        GetTree().ReloadCurrentScene();
    }

    // --- BESTAANDE VERLIES-FUNCTIE ---
    private void TriggerMissionFailed()
    {
        _isGameOver = true;

        Engine.TimeScale = 1.0f;
        var audioNodes = GetTree().GetNodesInGroup("audio");
        foreach (Node node in audioNodes)
        {
            if (IsInstanceValid(node)) node.Set("pitch_scale", 1.0f);
        }

        Tween slowTween = GetTree().CreateTween();
        slowTween.SetProcessMode((Tween.TweenProcessMode)3);

        slowTween.TweenMethod(Callable.From<float>(UpdateGlobalSpeed), 1.0f, 0.01f, 2.0f)
                 .SetTrans(Tween.TransitionType.Quad)
                 .SetEase(Tween.EaseType.Out);

        if (_blackScreen != null)
        {
            Tween fadeTween = GetTree().CreateTween();
            fadeTween.SetProcessMode((Tween.TweenProcessMode)3);
            fadeTween.TweenProperty(_blackScreen, "color", new Color(0, 0, 0, 1), 2.0f);

            fadeTween.Finished += async () =>
            {
                if (_failLabel != null)
                {
                    _failLabel.Text = "MISSION FAILED";
                    _failLabel.Visible = true;
                }

                GetTree().Paused = true;
                await ToSignal(GetTree().CreateTimer(2.5f, true), SceneTreeTimer.SignalName.Timeout);

                GetTree().Paused = false;
                Engine.TimeScale = 1.0f;
                GetTree().ReloadCurrentScene();
            };
        }
    }

    private void UpdateGlobalSpeed(float value)
    {
        var audioNodes = GetTree().GetNodesInGroup("audio");
        foreach (Node node in audioNodes)
        {
            if (IsInstanceValid(node)) node.Set("pitch_scale", value);
        }
    }

    private void ApplyVisualRecoil()
    {
        Node n = GetParent();
        while (n != null && !(n is PlayerScript)) n = n.GetParent();
        if (n is PlayerScript player) player.ApplyRecoil(RecoilAmount, RecoilTime);
    }
}

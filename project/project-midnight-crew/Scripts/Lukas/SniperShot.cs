using Godot;
using System;

public partial class SniperShot : Node3D
{
    [Export] public float Range = 200.0f;
    [Export] public float ShootCooldown = 1.0f;

    [ExportGroup("Recoil Instellingen")]
    [Export] public float RecoilAmount = 0.4f;
    [Export] public float RecoilTime = 0.12f;
    [Export] private Label labelSlow;
    [Export] private Node3D startMain;
    [Export] private Camera3D settingmainCamera;
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
        if(SettingMain.IsGepauzeerd || !GetNode<Node3D>("/root/Main/SettingMain").Visible)
        {
            return;
        }
        if (_isGameOver) return;

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
        if (camera == null) { _canShoot = true; return; }

        var spaceState = GetWorld3D().DirectSpaceState;
        var camDirection = -camera.GlobalTransform.Basis.Z;
        var camQuery = PhysicsRayQueryParameters3D.Create(camera.GlobalPosition, camera.GlobalPosition + camDirection * Range);
        camQuery.CollisionMask = 1;

        var result = spaceState.IntersectRay(camQuery);

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
            else if (hitObject.IsInGroup("NPC"))
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

    // --- NIEUWE WIN-FUNCTIE ---
    private async void TriggerMissionSuccess()
    {
        _isGameOver = true; // Zorgt dat de speler niet meer kan schieten

        var globalData = GetNode<GlobalData>("/root/GlobalData");
        int actiefLevel = 1;

        if(globalData != null)
        {
            actiefLevel = globalData.HuidigSpeelLevel;
            int volgendLevel = actiefLevel + 1;

            if (globalData.MaxVrijgespeeldLevel < volgendLevel)
            {
                globalData.MaxVrijgespeeldLevel = volgendLevel;
            }
        }

        if (_successLabel != null)
        {
            _successLabel.Text = "MISSION SUCCESS";
            _successLabel.Visible = true;
        }

        // Wacht 3 seconden
        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);
        GD.Print("Level herladen voor een schone start...");
        Input.MouseMode = Input.MouseModeEnum.Visible;


        /*if (_successLabel != null) _successLabel.Visible = false; // Verberg de succes tekst
        if (labelSlow != null) labelSlow.Visible = false;        // Verberg 'Target' tekst
        if (slowbar != null) slowbar.Visible = false;            // Verberg slowmobalk
        if (Target != null) Target.Visible = false;

        settingmainCamera.Current = false;
        this.Visible = false;
        startMain.Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;*/

        GetTree().ReloadCurrentScene();
    }

    // --- BESTAANDE VERLIES-FUNCTIE ---
    private void TriggerMissionFailed()
    {
        _isGameOver = true;

        // --- FIX: Reset de tijd en audio naar normaal voordat de animatie begint ---
        Engine.TimeScale = 1.0f;
        var audioNodes = GetTree().GetNodesInGroup("audio");
        foreach (Node node in audioNodes)
        {
            if (IsInstanceValid(node)) node.Set("pitch_scale", 1.0f);
        }
        // -----------------------------------------------------------------------

        Tween slowTween = GetTree().CreateTween();
        // Gebruik ProcessMode 3 (Always) zodat de tween doorgaat als de game pauzeert
        slowTween.SetProcessMode((Tween.TweenProcessMode)3);

        // De animatie die de wereld langzaam vertraagt na de fout
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
                // Wacht op een timer die ook doorloopt tijdens pauze
                await ToSignal(GetTree().CreateTimer(2.5f, true), SceneTreeTimer.SignalName.Timeout);

                GetTree().Paused = false;
                Engine.TimeScale = 1.0f;
                GetTree().ReloadCurrentScene();
            };
        }
    }

    private void UpdateGlobalSpeed(float value)
    {
        //Engine.TimeScale = value;
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
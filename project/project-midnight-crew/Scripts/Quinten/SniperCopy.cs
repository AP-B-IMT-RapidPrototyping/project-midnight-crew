using Godot;
using System;

public partial class SniperCopy : Node3D
{
	[Export] public float Range = 200.0f;
    [Export] public float ShootCooldown = 1.0f;

    [ExportGroup("Recoil Instellingen")]
    [Export] public float RecoilAmount = 0.4f;
    [Export] public float RecoilTime = 0.12f;

    // UI Nodes
    private Label _failLabel;    // We noemen deze even FailLabel voor de duidelijkheid
    private Label _successLabel; // Het label voor winst
	private Label _BulletLabel;
    private ColorRect _blackScreen;

    private Node3D _barrelLoc;
    private AudioStreamPlayer3D _shootSound;
    private AudioStreamPlayer3D _chamberSound;
    private bool _canShoot = true;
    private bool _isGameOver = false;
	private int bulletAmount = 1;
	private int ReloadAmount = 2;


    public override void _Ready()
    {
        _barrelLoc = GetNode<Node3D>("BarrelLoc");
        _shootSound = GetNode<AudioStreamPlayer3D>("ShootSound");
        _chamberSound = GetNode<AudioStreamPlayer3D>("ChamberBullet");

        // UI opzoeken
        _failLabel = GetTree().Root.FindChild("FailLabel", true, false) as Label;
        _successLabel = GetTree().Root.FindChild("SuccesLabel", true, false) as Label;
        _blackScreen = GetTree().Root.FindChild("BlackScreen", true, false) as ColorRect;
		_BulletLabel = GetTree().Root.FindChild("BulletLabel", true, false) as Label;
    }
    public override void _Process(double delta)
    {
        _BulletLabel.Text = $"{bulletAmount}/{ReloadAmount}";
    }


    public async override void _Input(InputEvent @event)
    {
        if (_isGameOver) return;

        if (@event.IsActionPressed("shoot") && _canShoot)
        {
            Shoot();
        }
        //reload
		if(@event.IsActionPressed("Reload") && ReloadAmount > 0) 
		{
			
			await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
			if (!_isGameOver && _chamberSound != null) _chamberSound.Play();
            bulletAmount = 1;
			if (ReloadAmount > 0) ReloadAmount--;
			
		}
        //reload
		else if(bulletAmount <= 0 && ReloadAmount <= 0)
		{
			TriggerMissionFailed();
		}
    }

    private async void Shoot()
    {
		if(bulletAmount > 0)//reload
		{
			bulletAmount--;
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
			/*await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
			if (!_isGameOver && _chamberSound != null) _chamberSound.Play();*/

			float remainingCooldown = Mathf.Max(0.1f, ShootCooldown - 0.5f);
			await ToSignal(GetTree().CreateTimer(remainingCooldown), SceneTreeTimer.SignalName.Timeout);
			_canShoot = true;
		}
        
    }

    // --- NIEUWE WIN-FUNCTIE ---
    private async void TriggerMissionSuccess()
    {
        _isGameOver = true; // Zorgt dat de speler niet meer kan schieten

        if (_successLabel != null)
        {
            _successLabel.Text = "MISSION SUCCESS";
            _successLabel.Visible = true;
        }
        // Wacht 3 seconden
        await ToSignal(GetTree().CreateTimer(3.0f), SceneTreeTimer.SignalName.Timeout);

        // Switch naar het startscherm
        GD.Print("Switching naar startscherm...");
        GetTree().ChangeSceneToFile("res://Scenes/Leon/StartScreen.tscn");
    }

    // --- BESTAANDE VERLIES-FUNCTIE ---
    private void TriggerMissionFailed()
    {
        _isGameOver = true;

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

using Godot;
using System;

public partial class TutorialPlayer : CharacterBody3D
{
    [ExportGroup("Beweging")]
    [Export] public float Speed = 5.0f;
    [Export] public float SprintMultiplier = 1.6f; // NIEUW: Hoeveel sneller je gaat tijdens sprinten
    [Export] public float GravityMultiplier = 2.0f;
    [Export] public float JumpVelocity = 4.5f;
    [Export] public float MouseSensitivity = 0.002f;

    [ExportGroup("Aim / Zoom")]
    [Export] public float ZoomFov = 20.0f;
    [Export] public float DefaultFov = 75.0f;
    [Export] public float AimSensitivity = 0.0005f;
    [Export] public float AimLerpSpeed = 0.15f;

    [ExportGroup("Camera Bobbing")]
    [Export] public float BobFreq = 2.4f;
    [Export] public float BobAmp = 0.08f;
    private float _tBob = 0.0f;

    [ExportGroup("Zaklamp")]
    [Export] public float FlashlightEnergy = 1.5f;

    [ExportGroup("UI Koppelingen")]
    [Export] private Panel _scopeUI;
    [Export] private Label _Target;
    [Export] private TextureRect _TargetBack;

    private Camera3D _camera;
    private SpotLight3D _flashlight;
    private Node3D _sniperModel;
    private Vector3 _baseCameraPos;

    private AudioStreamPlayer3D _aimSound;
    private bool _wasAiming = false;

    public float RotationX { get; set; } = 0f;
    
    public override void _Ready()
    {
		_Target.Visible = false;
		_TargetBack.Visible = false;
		_scopeUI.Visible = false;

        _camera = GetNode<Camera3D>("Camera3D");
		_baseCameraPos = _camera.Position;

		// GetNodeOrNull zorgt ervoor dat de game niet crasht als de node ontbreekt
		_flashlight = _camera.GetNodeOrNull<SpotLight3D>("Flashlight");
		
		if (_flashlight != null)
		{
			_flashlight.LightEnergy = 0.0f;
		}
		else
		{
			// Dit print een duidelijke waarschuwing in je console in plaats van te crashen!
			GD.PrintErr("Waarschuwing: Kan de node 'Flashlight' niet vinden onder Camera3D!");
		}

		// Omdat de code hierboven niet meer crasht, wordt je muis nu ALTIJD netjes gevangen:
		Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        //Blokkeer input wnr game is gepauzeerd
        //if (SettingMain.IsGepauzeerd || !GetNode<Node3D>("/root/Main/SettingMain").Visible) return;

        if (@event.IsActionPressed("flashLight"))
        {
            _flashlight.LightEnergy = _flashlight.LightEnergy > 0 ? 0.0f : FlashlightEnergy;
        }

        // RONDDRAAIEN MET DE MUIS
        if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
        {
            float currentSens = Input.IsActionPressed("aim") ? AimSensitivity : MouseSensitivity;
            
            // Draai het hele personage (lichaam) naar links en rechts
            RotateY(-mouseMotion.Relative.X * currentSens);
            
            // Draai alleen de camera omhoog en omlaag
            RotationX -= mouseMotion.Relative.Y * currentSens;
            
            // Zorg dat je niet je eigen nek breekt (beperk omhoog/omlaag kijken)
            RotationX = Mathf.Clamp(RotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        //Blokkeer bewegen wnr game is gepauzeerd
        /*if (SettingMain.IsGepauzeerd || !GetNode<Node3D>("/root/Main/SettingMain").Visible)
        {/*
            // Zorg dat de speler direct stilstaat en niet wegglijdt tijdens het pauzeren
            Velocity = Vector3.Zero;
            MoveAndSlide();

            // Forceer de camera en UI uit de 'aim' stand, anders blijft de scope hangen!
            _camera.Fov = DefaultFov;
            if (_scopeUI != null) _scopeUI.Visible = false;
            if (_Target != null) _Target.Visible = true;
            if (_TargetBack != null) _TargetBack.Visible = true;
            //if (_sniperModel != null) _sniperModel.Visible = true;
            return; 
        }*/

        bool isCurrentlyAiming = Input.IsActionPressed("aim");
        bool isSprinting = Input.IsActionPressed("sprint") && !isCurrentlyAiming;

        // --- BEWEGING & SPRINGEN ---
        Vector3 velocity = Velocity;

        // Zwaartekracht toepassen als je niet op de grond staat
        if (!IsOnFloor())
        {
            velocity += GetGravity() * GravityMultiplier * (float)delta;
        }

        // Springen
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        // Richting bepalen op basis van WASD / ZQSD
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        float currentMaxSpeed = isSprinting ? Speed * SprintMultiplier : Speed;

        // Snelheid toepassen
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * currentMaxSpeed;
            velocity.Z = direction.Z * currentMaxSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, currentMaxSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, currentMaxSpeed);
        }

        Velocity = velocity;
        MoveAndSlide();

        // --- HEAD BOBBING ---
        float bobMult = isCurrentlyAiming ? 0.1f : (isSprinting ? 1.5f : 1.0f);
        Vector3 targetBobPos = _baseCameraPos;

        if (IsOnFloor() && velocity.Length() > 0.1f)
        {
            _tBob += (float)delta * velocity.Length();
            targetBobPos.Y += Mathf.Sin(_tBob * BobFreq) * BobAmp * bobMult;
            targetBobPos.X += Mathf.Cos(_tBob * BobFreq * 0.5f) * BobAmp * bobMult;
        }

        _camera.Position = _camera.Position.Lerp(targetBobPos, (float)delta * 10.0f);
        
        // Pas de camera rotatie toe (omhoog en omlaag kijken)
        _camera.Rotation = new Vector3(RotationX, 0, 0);


        // --- ZOOM & UI LOGICA ---

        // 1. Bepaal wat de doel-FOV moet zijn
        float targetFov = isCurrentlyAiming ? ZoomFov : DefaultFov;

        // 2. Pas de FOV van de camera soepel aan (Lerp)
        _camera.Fov = Mathf.Lerp(_camera.Fov, targetFov, (float)delta * AimLerpSpeed);

        // 3. Toon de scope UI en verberg de normale crosshair tijdens het richten
        if (_scopeUI != null) _scopeUI.Visible = isCurrentlyAiming;
        if (_Target != null) _Target.Visible = !isCurrentlyAiming;
        if (_TargetBack != null) _TargetBack.Visible = !isCurrentlyAiming;
    }

    public void ApplyRecoil(float strength, float time)
    {
        //Geen terugslag animatie tijdens pauze
        // if (SettingMain.IsGepauzeerd) return;

        Tween tween = GetTree().CreateTween();
        float recoilInRad = Mathf.DegToRad(strength * 5f);
        float targetRot = RotationX + recoilInRad;
        float originalRot = RotationX;

        tween.TweenProperty(this, nameof(RotationX), targetRot, time).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, nameof(RotationX), originalRot, time * 2.5f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    }
}
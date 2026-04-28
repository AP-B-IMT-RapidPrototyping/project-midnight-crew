using Godot;
using System;

public partial class PlayerScript : CharacterBody3D
{
    [ExportGroup("Beweging")]
    [Export] public float Speed = 5.0f;
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

    private Camera3D _camera;
    private SpotLight3D _flashlight;
    private TextureRect _scopeUI;
    private Node3D _sniperModel;
    private Vector3 _baseCameraPos;

    // Audio variabele
    private AudioStreamPlayer3D _aimSound;
    private bool _wasAiming = false;

    // Property voor de Tween en Camera rotatie
    public float RotationX { get; set; } = 0f;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _baseCameraPos = _camera.Position;

        _flashlight = _camera.GetNode<SpotLight3D>("Flashlight");
        _flashlight.LightEnergy = 0.0f;

        _sniperModel = _camera.GetNode<Node3D>("Sniper");

        // Zoek de AimSound node op
        _aimSound = GetNode<AudioStreamPlayer3D>("AimSound");

        _scopeUI = GetTree().Root.FindChild("Scope", true, false) as TextureRect;
        if (_scopeUI != null) _scopeUI.Visible = false;

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }

        if (@event.IsActionPressed("flashLight"))
        {
            _flashlight.LightEnergy = _flashlight.LightEnergy > 0 ? 0.0f : FlashlightEnergy;
        }

        if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
        {
            float currentSens = Input.IsActionPressed("aim") ? AimSensitivity : MouseSensitivity;

            RotateY(-mouseMotion.Relative.X * currentSens);

            RotationX -= mouseMotion.Relative.Y * currentSens;
            RotationX = Mathf.Clamp(RotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        bool isCurrentlyAiming = Input.IsActionPressed("aim");

        // --- AIM GELUID LOGICA ---
        if (isCurrentlyAiming && !_wasAiming)
        {
            // Speelt af zodra je de knop indrukt
            if (_aimSound != null) _aimSound.Play();
        }
        _wasAiming = isCurrentlyAiming;

        // --- AIM & ZICHTBAARHEID ---
        if (isCurrentlyAiming)
        {
            _camera.Fov = Mathf.Lerp(_camera.Fov, ZoomFov, AimLerpSpeed);
            if (_scopeUI != null) _scopeUI.Visible = true;
            if (_sniperModel != null) _sniperModel.Visible = false;
        }
        else
        {
            _camera.Fov = Mathf.Lerp(_camera.Fov, DefaultFov, AimLerpSpeed);
            if (_scopeUI != null) _scopeUI.Visible = false;
            if (_sniperModel != null) _sniperModel.Visible = true;
        }

        // --- BEWEGING ---
        Vector3 velocity = Velocity;
        if (!IsOnFloor()) velocity += GetGravity() * (float)delta;

        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * Speed;
            velocity.Z = direction.Z * Speed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
        }

        Velocity = velocity;
        MoveAndSlide();

        // --- HEAD BOBBING ---
        float bobMult = isCurrentlyAiming ? 0.1f : 1.0f;
        _tBob += (float)delta * velocity.Length() * (IsOnFloor() ? 1.0f : 0.0f);
        Vector3 pos = _camera.Position;
        pos.Y = _baseCameraPos.Y + Mathf.Sin(_tBob * BobFreq) * BobAmp * bobMult;
        pos.X = _baseCameraPos.X + Mathf.Cos(_tBob * BobFreq * 0.5f) * BobAmp * bobMult;

        // --- CAMERA UPDATE ---
        _camera.Position = pos;
        _camera.Rotation = new Vector3(RotationX, 0, 0);
    }

    public void ApplyRecoil(float strength, float time)
    {
        Tween tween = GetTree().CreateTween();

        float recoilInRad = Mathf.DegToRad(strength * 5f);
        float targetRot = RotationX + recoilInRad;
        float originalRot = RotationX;

        tween.TweenProperty(this, nameof(RotationX), targetRot, time)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);

        tween.TweenProperty(this, nameof(RotationX), originalRot, time * 2.5f)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
    }
}
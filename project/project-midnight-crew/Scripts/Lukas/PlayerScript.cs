using Godot;
using System;

public partial class PlayerScript : CharacterBody3D
{
    [ExportGroup("Beweging")]
    [Export] public float Speed = 5.0f;
    [Export] public float MouseSensitivity = 0.002f;

    [ExportGroup("Camera Bobbing")]
    [Export] public float BobFreq = 2.4f;
    [Export] public float BobAmp = 0.08f;
    private float _tBob = 0.0f;

    [ExportGroup("Zaklamp")]
    [Export] public float FlashlightEnergy = 1.5f;

    private Camera3D _camera;
    private SpotLight3D _flashlight; // We gaan ervan uit dat dit een SpotLight3D is
    private float _rotationX = 0f;
    private Vector3 _baseCameraPos;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _baseCameraPos = _camera.Position;

        // Zoek de zaklamp als child van de camera
        _flashlight = _camera.GetNode<SpotLight3D>("Flashlight");

        // Zorg dat de zaklamp standaard uit staat
        _flashlight.LightEnergy = 0.0f;

        // Muis vastzetten bij start
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        // 1. ESC om muis te tonen/verbergen
        if (@event.IsActionPressed("ui_cancel"))
        {
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;
        }

        // 2. Zaklamp Toggle
        if (@event.IsActionPressed("flashLight"))
        {
            if (_flashlight.LightEnergy > 0)
            {
                _flashlight.LightEnergy = 0.0f;
            }
            else
            {
                _flashlight.LightEnergy = FlashlightEnergy;
            }
        }

        // 3. Camera rotatie
        if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
        {
            RotateY(-mouseMotion.Relative.X * MouseSensitivity);

            _rotationX -= mouseMotion.Relative.Y * MouseSensitivity;
            _rotationX = Mathf.Clamp(_rotationX, Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

            _camera.Rotation = new Vector3(_rotationX, _camera.Rotation.Y, _camera.Rotation.Z);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Zwaartekracht
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

        // Input ophalen uit Input Map
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

        // --- HEAD BOBBING LOGICA ---
        // Alleen bobs als we op de grond staan en bewegen
        _tBob += (float)delta * velocity.Length() * (IsOnFloor() ? 1.0f : 0.0f);
        Vector3 pos = _camera.Position;

        pos.Y = _baseCameraPos.Y + Mathf.Sin(_tBob * BobFreq) * BobAmp;
        pos.X = _baseCameraPos.X + Mathf.Cos(_tBob * BobFreq * 0.5f) * BobAmp;

        _camera.Position = pos;
    }
}
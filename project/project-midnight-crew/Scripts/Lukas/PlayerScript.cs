using Godot;
using System;

public partial class PlayerScript : CharacterBody3D
{
    [Export] public float Speed = 5.0f;
    [Export] public float MouseSensitivity = 0.002f;

    private Camera3D _camera;
    private float _rotationX = 0f;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");

        // Start met de muis vastgezet
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        // 1. Muis bevrijden of weer vastzetten met Escape
        if (@event.IsActionPressed("ui_cancel")) // "ui_cancel" is standaard de Escape-toets
        {
            if (Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }

        // 2. Alleen rondkijken als de muis 'gevangen' is
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

        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_backward", "move_forward");
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
    }
}
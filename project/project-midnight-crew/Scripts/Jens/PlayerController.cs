using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{

	
	[Export] private RayCast3D aimRaycast;
	[Signal]
    public delegate void ShootEventHandler(RayCast3D raycast);
	[Export] private Camera3D camera;
	[Export] private Vector3 cameraPosition;
	[Export] private float mouseSensitivity = 0.003f;

	
	private float _Speed = 5.0f;
	private float _JumpVelocity = 4.5f;

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("shoot"))
    {
        // Stuur de raycast naar het wapen
        EmitSignal(SignalName.Shoot, aimRaycast);
        GD.Print($"Hit: {aimRaycast.GetCollisionPoint()}");
    }
	}

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
		{
			// Horizontal rotation: roteer de HELE speler (Y-axis)
			this.RotateY(-mouseMotion.Relative.X * mouseSensitivity);

			// Vertical rotation: roteer alleen de CAMERA (X-axis)
            camera.RotateX(-mouseMotion.Relative.Y * mouseSensitivity);

			// Voorkom dat de camera omdraait (clamp tussen -86° en +86°, of -1.5 en 1.5 radialen)
    		Vector3 cameraRotation = camera.Rotation;
    		cameraRotation.X = Mathf.Clamp(cameraRotation.X, -1.5f, 1.5f);
    		camera.Rotation = cameraRotation;
		}
    }

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }



	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = _JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * _Speed;
			velocity.Z = direction.Z * _Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, _Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, _Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}

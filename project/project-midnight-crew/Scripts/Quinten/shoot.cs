using Godot;
using System;
using System.Threading;

public partial class shoot : Node3D
{
	RayCast3D raycast = new RayCast3D();
	[Signal] public delegate void ShootEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("shoot"))
		{
			EmitSignal(SignalName.Shoot);
		}
	}
}

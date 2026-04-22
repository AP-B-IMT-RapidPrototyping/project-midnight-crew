using Godot;
using System;
using System.ComponentModel;
using System.Threading;

public partial class Aim : Camera3D
{
	private int StandardFov = 75;
	[Export] private Camera3D camera;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsActionPressed("aim"))
		{
				camera.Fov = 15;
	
		}
		else
		{
			camera.Fov = 75; 
		}
	}
}

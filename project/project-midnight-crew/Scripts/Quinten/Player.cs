using Godot;
using System;

public partial class Player : CharacterBody3D
{
		[Export] private RayCast3D aimRaycast;
    	[Signal] public delegate void ShootEventHandler(RayCast3D raycast);
		public override void _Process(double delta)
		{
			if (Input.IsActionJustPressed("shoot"))
			{
				// Stuur de raycast naar het wapen
				EmitSignal(SignalName.Shoot, aimRaycast);
				GD.Print($"Hit: {aimRaycast.GetCollisionPoint()}");
			}
		}

}

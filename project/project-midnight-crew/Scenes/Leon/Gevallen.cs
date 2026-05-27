using Godot;
using System;

public partial class Gevallen : Area3D
{
    [Export]
    public Marker3D TargetMarker { get; set; }

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is CharacterBody3D player)
        {
            if (TargetMarker != null)
            {
                player.GlobalPosition = TargetMarker.GlobalPosition;

                player.Velocity = Vector3.Zero;
            }
            else
            {
                GD.PrintErr("TargetMarker is niet toegewezen in de Inspector!");
            }
        }
    }

    // Netjes opruimen wanneer de node uit de scene wordt gehaald
    protected override void Dispose(bool disposing)
    {
        BodyEntered -= OnBodyEntered;
        base.Dispose(disposing);
    }
}

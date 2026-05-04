using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class SelectTarget : Node3D
{
	List<CharacterBody3D> Targets = new List<CharacterBody3D>();
	Random enemySelector = new Random();
	int index;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Targets = GetTree().GetNodesInGroup("NPC")
							.OfType<CharacterBody3D>()
						    .ToList();

		index = enemySelector.Next(0, Targets.Count + 1);
		Targets[index].AddToGroup("Target");


		Targets.ForEach(t => GD.Print(t.GetGroups()));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

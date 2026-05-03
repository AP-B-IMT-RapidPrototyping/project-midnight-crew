using Godot;
using System;

public partial class timeSlow : Node
{
	Timer slowMoTimer;
	private bool timerAfgelopen = false;
	[Export] AudioStreamPlayer3D SlowTimeSound = new AudioStreamPlayer3D();
	[Export] AudioStreamPlayer3D Rain = new AudioStreamPlayer3D();
	int isSlowPressed = 1;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// 1. Haal de timer één keer op in Ready
		slowMoTimer = GetNode<Timer>("SlowMoTimer");
		
		// 2. Verbind het signaal één keer in Ready
		slowMoTimer.Timeout += () => timerAfgelopen = true;
	}

	public override void _Process(double delta)
	{
		// Slow-mo aanzetten
		if (Input.IsActionJustPressed("SlowTime") && isSlowPressed % 2 > 0)
		{
			Engine.TimeScale = 0.2;
			Rain.PitchScale = 0.2f;
			isSlowPressed++;
			slowMoTimer.Start(); // Vergeet niet de timer te starten!
			SlowTimeSound.Play();
		}
		// Slow-mo uitzetten (via knop OF via timer)
		else if ((Input.IsActionJustPressed("SlowTime") && isSlowPressed % 2 == 0) || timerAfgelopen)
		{
			Engine.TimeScale = 1.0;
			Rain.PitchScale = 1f;
			isSlowPressed = 1;
			timerAfgelopen = false;
			slowMoTimer.Stop(); // Stop de timer als we handmatig teruggaan
			SlowTimeSound.Stop();
		}
	}
}

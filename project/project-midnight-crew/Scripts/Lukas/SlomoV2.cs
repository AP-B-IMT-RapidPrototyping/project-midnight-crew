using Godot;
using System;

public partial class SlomoV2 : Node
{
    Timer slowMoTimer;
    private bool timerAfgelopen = false;

    [Export] public AudioStreamPlayer3D SlowTimeSound;

    private int isSlowPressed = 1;

    public override void _Ready()
    {
        // Zoek de timer
        slowMoTimer = GetNodeOrNull<Timer>("SlowMoTimer");
        if (slowMoTimer != null)
        {
            slowMoTimer.Timeout += () => timerAfgelopen = true;
        }
    }

    public override void _Process(double delta)
    {
        // 1. Slow-mo aanzetten
        if (Input.IsActionJustPressed("SlowTime") && isSlowPressed % 2 > 0)
        {
            SetSlowMo(true);
        }
        // 2. Slow-mo uitzetten
        else if ((Input.IsActionJustPressed("SlowTime") && isSlowPressed % 2 == 0) || timerAfgelopen)
        {
            SetSlowMo(false);
        }
    }

    private void SetSlowMo(bool active)
    {
        float targetTime = active ? 0.2f : 1.0f;
        Engine.TimeScale = targetTime;

        if (active)
        {
            isSlowPressed++;
            if (slowMoTimer != null) slowMoTimer.Start();

            // Start het geluid
            if (SlowTimeSound != null) SlowTimeSound.Play();
        }
        else
        {
            isSlowPressed = 1;
            timerAfgelopen = false;
            if (slowMoTimer != null) slowMoTimer.Stop();

            // --- DE FIX: Stop het geluid onmiddellijk ---
            if (SlowTimeSound != null && SlowTimeSound.Playing)
            {
                SlowTimeSound.Stop();
            }
        }

        // Pas de pitch aan voor de groep "audio"
        var audioNodes = GetTree().GetNodesInGroup("audio");
        foreach (Node node in audioNodes)
        {
            node.Set("pitch_scale", targetTime);
        }
    }
}
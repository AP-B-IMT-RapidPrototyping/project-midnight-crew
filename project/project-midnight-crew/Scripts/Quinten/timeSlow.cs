using Godot;
using System;

public partial class timeSlow : Node
{
    Timer slowMoTimer;
    private bool timerAfgelopen = false;

    [Export] AudioStreamPlayer3D SlowTimeSound;
    [Export] AudioStreamPlayer3D Rain;
    [Export] Label SlowMoLabel; // Sleep je Label (bijv. in een CanvasLayer) hiernaartoe

    int isSlowPressed = 1;

    public override void _Ready()
    {
        slowMoTimer = GetNode<Timer>("SlowMoTimer");
        slowMoTimer.Timeout += () => timerAfgelopen = true;

        // DIT IS DE KEY: De timer negeert de Engine.TimeScale
        slowMoTimer.ProcessMode = ProcessModeEnum.Always;

        // Verberg het label bij de start
        if (SlowMoLabel != null)
        {
            SlowMoLabel.Visible = false;
        }
    }

    public override void _Process(double delta)
    {
        // Slow-mo aanzetten
        if (Input.IsActionJustPressed("SlowTime") && isSlowPressed % 2 > 0)
        {
            StartSlowMo();
        }
        // Slow-mo uitzetten (via knop OF via timer)
        else if ((Input.IsActionJustPressed("SlowTime") && isSlowPressed % 2 == 0) || timerAfgelopen)
        {
            StopSlowMo();
        }

        // Update de tekst op het scherm
        UpdateLabel();
    }

    private void StartSlowMo()
    {
        Engine.TimeScale = 0.2;
        if (Rain != null) Rain.PitchScale = 0.2f;

        isSlowPressed++;
        slowMoTimer.Start();

        if (SlowTimeSound != null) SlowTimeSound.Play();
        if (SlowMoLabel != null) SlowMoLabel.Visible = true;
    }

    private void StopSlowMo()
    {
        Engine.TimeScale = 1.0;
        if (Rain != null) Rain.PitchScale = 1.0f;

        isSlowPressed = 1;
        timerAfgelopen = false;
        slowMoTimer.Stop();

        if (SlowTimeSound != null) SlowTimeSound.Stop();
        if (SlowMoLabel != null) SlowMoLabel.Visible = false;
    }

    private void UpdateLabel()
    {
        if (SlowMoLabel != null && !slowMoTimer.IsStopped())
        {
            // De tijd die nog over is op de timer
            double timeLeft = slowMoTimer.TimeLeft;
            SlowMoLabel.Text = $"Slowmo: {timeLeft:F2}"; // F2 zorgt voor 2 cijfers achter de komma
        }
    }
}
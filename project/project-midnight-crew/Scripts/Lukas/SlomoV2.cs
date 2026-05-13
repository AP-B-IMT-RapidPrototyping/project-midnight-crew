using Godot;
using System;

public partial class SlomoV2 : Node
{
    private Timer slowMoTimer;
    private bool timerAfgelopen = false;

    [Export] public AudioStreamPlayer3D SlowTimeSound;
    [Export] public ProgressBar SlomoBar;

    [Export] public float MaxSlomoTime = 5.0f;
    [Export] public float RechargeRate = 0.8f;

    private float _currentSlomoEnergy;
    private bool _isSlomoActive = false;
    private float _flickerTime = 0.0f; // Voor het flikker effect

    public override void _Ready()
    {
        _currentSlomoEnergy = MaxSlomoTime;

        slowMoTimer = GetNodeOrNull<Timer>("SlowMoTimer");
        if (slowMoTimer != null)
        {
            slowMoTimer.ProcessMode = ProcessModeEnum.Always;
            slowMoTimer.OneShot = true;
            slowMoTimer.Timeout += () => timerAfgelopen = true;
        }

        if (SlomoBar != null)
        {
            SlomoBar.MaxValue = MaxSlomoTime;
            SlomoBar.Value = _currentSlomoEnergy;
            SlomoBar.Visible = true;
            SlomoBar.ShowPercentage = false;
        }
    }

    public override void _Process(double delta)
    {
        if (_isSlomoActive)
        {
            _currentSlomoEnergy = (float)slowMoTimer.TimeLeft;
            if (Input.IsActionJustPressed("SlowTime")) SetSlowMo(false);
        }
        else
        {
            if (_currentSlomoEnergy < MaxSlomoTime)
            {
                _currentSlomoEnergy += RechargeRate * (float)delta;
                if (_currentSlomoEnergy > MaxSlomoTime) _currentSlomoEnergy = MaxSlomoTime;

                // Tel op voor het flikkeren
                _flickerTime += (float)delta * 10.0f;
            }
            else
            {
                _flickerTime = 0; // Reset als hij vol is
            }

            if (Input.IsActionJustPressed("SlowTime") && _currentSlomoEnergy >= MaxSlomoTime)
            {
                SetSlowMo(true);
            }
        }

        if (timerAfgelopen) SetSlowMo(false);

        UpdateUI();
    }

    private void SetSlowMo(bool active)
    {
        _isSlomoActive = active;
        float targetTime = active ? 0.2f : 1.0f;
        Engine.TimeScale = targetTime;

        if (active)
        {
            if (slowMoTimer != null) slowMoTimer.Start(_currentSlomoEnergy);
            if (SlowTimeSound != null) SlowTimeSound.Play();
        }
        else
        {
            timerAfgelopen = false;
            if (slowMoTimer != null) slowMoTimer.Stop();
            if (SlowTimeSound != null && SlowTimeSound.Playing) SlowTimeSound.Stop();
        }

        var audioNodes = GetTree().GetNodesInGroup("audio");
        foreach (Node node in audioNodes)
        {
            node.Set("pitch_scale", targetTime);
        }
    }

    private void UpdateUI()
    {
        if (SlomoBar == null) return;

        SlomoBar.Value = _currentSlomoEnergy;

        // Haal de StyleBox van de achtergrond op
        var bgStyle = SlomoBar.GetThemeStylebox("background") as StyleBoxFlat;

        if (bgStyle != null)
        {
            if (_currentSlomoEnergy < MaxSlomoTime && !_isSlomoActive)
            {
                // --- OPLAAD EFFECT ---
                // We gebruiken Sinus voor een vloeiende flikkering tussen 0.3 en 1.0 helderheid
                float pulse = (Mathf.Sin(_flickerTime) + 1.0f) / 2.0f;
                Color flickerRed = new Color(1.0f, 0.0f, 0.0f, 0.3f + (pulse * 0.7f));

                bgStyle.BorderColor = flickerRed;
                // Zet de border width aan voor het geval die uit stond
                bgStyle.SetBorderWidthAll(2);
            }
            else
            {
                // --- VOL OF ACTIEF (Normaal) ---
                bgStyle.BorderColor = new Color(1, 1, 1, 0.5f); // Subtiel wit/grijs
            }
        }
    }
}
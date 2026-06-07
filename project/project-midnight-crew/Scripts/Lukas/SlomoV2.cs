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

    // 🔥 NIEUW: Onthoudt of we in de "oplaad-fase" zitten omdat de balk écht leeg is gegaan
    private bool _moetOpladen = false;

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
            SlomoBar.ShowPercentage = false;
        }
    }

    public override void _Process(double delta)
    {
        if (_isSlomoActive)
        {
            _currentSlomoEnergy = (float)slowMoTimer.TimeLeft;

            // Als de energie tijdens het gebruik onder of gelijk aan 0 komt -> Forceer opladen
            if (_currentSlomoEnergy <= 0.0f)
            {
                _currentSlomoEnergy = 0.0f;
                _moetOpladen = true;
                SetSlowMo(false);
            }
            else if (Input.IsActionJustPressed("SlowTime"))
            {
                SetSlowMo(false);
            }
        }
        else
        {
            // 🔥 LOGICA VOOR HET OPLADEN 🔥
            if (_moetOpladen)
            {
                _currentSlomoEnergy += RechargeRate * (float)delta;
                _flickerTime += (float)delta * 10.0f;

                // Pas als hij VOLLEDIG vol is, stoppen we met opladen en mag hij weer gebruikt worden
                if (_currentSlomoEnergy >= MaxSlomoTime)
                {
                    _currentSlomoEnergy = MaxSlomoTime;
                    _moetOpladen = false; // Oplaad-fase klaar!
                    _flickerTime = 0;
                }
            }
            else
            {
                _flickerTime = 0; // Geen flikkering als hij halverwege stilstaat
            }

            // Je mag de slomo aanzetten als je op de knop drukt, zolang je NIET in de verplichte oplaad-fase zit én er energie is
            if (Input.IsActionJustPressed("SlowTime") && !_moetOpladen && _currentSlomoEnergy > 0.0f)
            {
                SetSlowMo(true);
            }
        }

        if (timerAfgelopen)
        {
            _moetOpladen = true; // Timer afgelopen betekent dat hij leeg is -> opladen!
            SetSlowMo(false);
        }

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

        SlomoBar.Visible = GetNode<Node3D>("/root/Main/SettingMain").Visible;
        SlomoBar.Value = _currentSlomoEnergy;

        var bgStyle = SlomoBar.GetThemeStylebox("background") as StyleBoxFlat;

        if (bgStyle != null)
        {
            // 🔥 De balk flikkert nu ALLEEN als hij daadwerkelijk aan het opladen is
            if (_moetOpladen)
            {
                // --- OPLAAD EFFECT ---
                float pulse = (Mathf.Sin(_flickerTime) + 1.0f) / 2.0f;
                Color flickerRed = new Color(1.0f, 0.0f, 0.0f, 0.3f + (pulse * 0.7f));

                bgStyle.BorderColor = flickerRed;
                bgStyle.SetBorderWidthAll(2);
            }
            else
            {
                // --- VOL, REEDS LEEGGLOPEN OF ACTIEF (Normaal) ---
                bgStyle.BorderColor = new Color(1, 1, 1, 0.5f);
            }
        }
    }
}
using Godot;
using System;

public partial class Bliksem : DirectionalLight3D
{
    [Export] public float FlashEnergy = 15.0f;
    [Export] public float DefaultEnergy = 0.0f;
    [Export] public float FlashDuration = 0.8f;
    [Export] Sprite3D bliksem;


    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        LightEnergy = DefaultEnergy;
        StartRandomTimer();
    }
    public override void _PhysicsProcess(double delta)
    {
        if (this.LightEnergy > 0.1)
        {
            bliksem.Visible = true;
        }
        if (this.LightEnergy < 0.1)
        {
            bliksem.Visible = false;
        }
    }
    private void StartRandomTimer()
    {

        float nextFlashIn = _rng.RandfRange(3.0f, 10.0f);
        GetTree().CreateTimer(nextFlashIn).Timeout += TriggerFlash;

    }

    public void TriggerFlash()
    {

        // --- NIEUW: Willekeurige rotatie rond de Y-as ---
        // We kiezen een hoek tussen 0 en 360 graden (in radialen)
        float randomYRotation = _rng.RandfRange(0, Mathf.Tau); // Tau is 2 * PI
        // We behouden de huidige X en Z rotatie, maar veranderen de Y
        Vector3 currentRotation = Rotation;
        currentRotation.Y = randomYRotation;
        Rotation = currentRotation;

        // --- De Flits ---
        LightEnergy = FlashEnergy;

        // Geleidelijk terug naar zwart faden
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "light_energy", DefaultEnergy, FlashDuration)
             .SetTrans(Tween.TransitionType.Expo)
             .SetEase(Tween.EaseType.Out);
        StartRandomTimer();
    }
}
using Godot;
using System;

public partial class Bliksem : DirectionalLight3D
{
    [Export] public float FlashEnergy = 15.0f;
    [Export] public float DefaultEnergy = 0.0f;
    [Export] public float FlashDuration = 0.8f;
    [Export] Sprite3D bliksem;

    [ExportGroup("Audio")]
    // Hier kun je in de Inspector je 5 geluiden slepen
    [Export] public Godot.Collections.Array<AudioStream> ThunderSounds;

    private AudioStreamPlayer3D _audioPlayer;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        // Pad naar je audiospeler gebaseerd op je boomstructuur
        _audioPlayer = GetNode<AudioStreamPlayer3D>("Sprite3D/BliksemSound");

        LightEnergy = DefaultEnergy;
        if (bliksem != null) bliksem.Visible = false;

        _rng.Randomize();
        StartRandomTimer();
    }

    public override void _PhysicsProcess(double delta)
    {
        // We gebruiken nu de LightEnergy om de transparantie van de sprite te regelen
        // Zo flitst de sprite even hard als het licht!
        if (bliksem != null)
        {
            bliksem.Visible = LightEnergy > 0.1f;
        }
    }

    private void StartRandomTimer()
    {
        float nextFlashIn = _rng.RandfRange(3.0f, 10.0f);
        GetTree().CreateTimer(nextFlashIn).Timeout += TriggerFlash;
    }

    public void TriggerFlash()
    {
        // 1. Willekeurige rotatie
        float randomYRotation = _rng.RandfRange(0, Mathf.Tau);
        Vector3 currentRotation = Rotation;
        currentRotation.Y = randomYRotation;
        Rotation = currentRotation;

        // 2. De Flits
        LightEnergy = FlashEnergy;

        // 3. WILLEKEURIG GELUID AFSPELEN
        PlayRandomThunder();

        // 4. Geleidelijk terug naar zwart faden
        Tween tween = GetTree().CreateTween();
        tween.TweenProperty(this, "light_energy", DefaultEnergy, FlashDuration)
             .SetTrans(Tween.TransitionType.Expo)
             .SetEase(Tween.EaseType.Out);

        StartRandomTimer();
    }

    private void PlayRandomThunder()
    {
        if (_audioPlayer == null || ThunderSounds == null || ThunderSounds.Count == 0) return;

        // Kies een willekeurig geluid uit de lijst
        int index = _rng.RandiRange(0, ThunderSounds.Count - 1);
        _audioPlayer.Stream = ThunderSounds[index];

        // Varieer de toonhoogte (pitch) een beetje voor extra realisme
        _audioPlayer.PitchScale = _rng.RandfRange(0.8f, 1.2f);

        _audioPlayer.Play();
    }
}
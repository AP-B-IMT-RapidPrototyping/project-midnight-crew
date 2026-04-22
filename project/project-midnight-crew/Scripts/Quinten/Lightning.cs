using Godot;
using System;

public partial class Lightning : SpotLight3D
{
    [Export] private Godot.Timer countdown;
    // 'this' is al de SpotLight3D, dus een extra referentie is optioneel
    
    private Random _random = new Random();

    public override void _Ready()
    {
        // Koppel de Timeout signal aan een functie
        countdown.Timeout += OnTimerTimeout;
        StartWaiting();
    }

    private void StartWaiting()
    {
        this.Visible = false;
        // Wacht tussen de 2 en 10 seconden voor de volgende flits
        countdown.WaitTime = _random.NextDouble() * (10.0 - 2.0) + 2.0;
        countdown.OneShot = true;
        countdown.Start();
    }

    private async void OnTimerTimeout()
    {
        // 1. Licht aan
        this.Visible = true;
        GD.Print("Kachouw!");

        // 2. Wacht heel even (de flits zelf)
        // We gebruiken await om de rest van de code even te pauzeren
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);

        // 3. Licht uit
        this.Visible = false;
        GD.Print("Licht uit");

        // 4. Start het proces opnieuw
        StartWaiting();
    }
}
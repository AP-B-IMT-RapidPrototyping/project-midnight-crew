using Godot;
using System;

public partial class Eindzone : Area3D
{
    // 🔥 Dit staat nu op Node3D, dus je kunt je Sniper-node er nu wél gewoon inslepen!
    [Export] public Node3D MijnSniper;

    public override void _Ready()
    {
        // We koppelen het signaal handmatig in code
        BodyEntered += OnBodyEntered;
        GD.Print("EindZone is actief en luistert naar binnenkomende lichamen...");
    }

    private void OnBodyEntered(Node3D body)
    {
        // We checken of het object in de groep "NPC4" zit
        if (body.IsInGroup("NPC4"))
        {
            GD.Print($"🚨 GECENTREERD: {body.Name} uit groep NPC4 heeft de eindzone betreden!");

            if (MijnSniper != null)
            {
                // Godot zoekt zelf in het script van deze Node3D naar de functie TriggerMissionFailed
                MijnSniper.Call("TriggerMissionFailed");
            }
            else
            {
                GD.PrintErr("❌ FOUT: Je bent vergeten de Sniper node in te stellen in de inspector van de EindZone!");
            }
        }
        else
        {
            // Dit helpt ons debuggen in de console
            GD.Print($"Er kwam iets de EindZone binnen, maar het zat niet in groep NPC4. Het was: {body.Name}");
        }
    }
}
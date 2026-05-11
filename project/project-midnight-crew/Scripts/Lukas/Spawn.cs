using Godot;
using System;
using System.Collections.Generic; // Nodig voor de List

public partial class Spawn : Node3D
{
    [Export] public float SpawnRadius = 50.0f;

    public override void _Ready()
    {
        // Wacht even tot alles geladen is
        GetTree().CreateTimer(0.2f).Timeout += VerdeelNPCs;
    }

    private void VerdeelNPCs()
    {
        var nodesInGroup = GetTree().GetNodesInGroup("NPC");
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        // We maken een tijdelijke lijst aan om alleen de echte lichamen in op te slaan
        List<CharacterBody3D> echteNPCs = new List<CharacterBody3D>();

        GD.Print($"Systeem: Start verdelen van {nodesInGroup.Count} nodes gevonden in groep 'NPC'...");

        foreach (Node node in nodesInGroup)
        {
            if (node is CharacterBody3D npc)
            {
                // Voeg toe aan onze lijst met kandidaten voor het target
                echteNPCs.Add(npc);

                var agent = npc.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
                if (agent == null) continue;

                Rid mapRid = agent.GetNavigationMap();
                Vector3 spawnPoint = Vector3.Zero;
                int pogingen = 0;

                while (spawnPoint == Vector3.Zero && pogingen < 10)
                {
                    Vector3 randomPos = new Vector3(
                        rng.RandfRange(-SpawnRadius, SpawnRadius),
                        1.0f,
                        rng.RandfRange(-SpawnRadius, SpawnRadius)
                    );

                    spawnPoint = NavigationServer3D.MapGetClosestPoint(mapRid, randomPos);
                    pogingen++;
                }

                if (spawnPoint != Vector3.Zero)
                {
                    npc.GlobalPosition = spawnPoint + new Vector3(0, 0.5f, 0);
                    agent.SetNavigationMap(mapRid);

                    if (npc.HasMethod("PickNewTarget"))
                    {
                        npc.CallDeferred("PickNewTarget");
                    }
                }
            }
        }

        // --- TARGET SELECTIE UIT DE GEFILTERDE LIJST ---
        if (echteNPCs.Count > 0)
        {
            int randomIndex = rng.RandiRange(0, echteNPCs.Count - 1);
            CharacterBody3D target = echteNPCs[randomIndex];

            target.AddToGroup("Target");

            GD.Print("------------------------------------------");
            GD.Print($"MISSIE GEGENEREERD");
            GD.Print($"Totaal aantal burgers: {echteNPCs.Count}");
            GD.Print($"Huidig Doelwit: {target.Name}");
            GD.Print("------------------------------------------");

            // Haal de // hieronder weg als je het target altijd wilt zien tijdens het testen:
            MaakTargetZichtbaar(target);
        }
        else
        {
            GD.PrintErr("FOUT: Geen CharacterBody3D gevonden in groep 'NPC'. Check je groepen!");
        }
    }

    private void MaakTargetZichtbaar(CharacterBody3D target)
    {
        foreach (var child in target.GetChildren())
        {
            if (child is MeshInstance3D mesh)
            {
                var mat = new StandardMaterial3D();
                mat.AlbedoColor = new Color(1, 0, 0); // Rood
                mesh.MaterialOverride = mat;
            }
        }
    }
}
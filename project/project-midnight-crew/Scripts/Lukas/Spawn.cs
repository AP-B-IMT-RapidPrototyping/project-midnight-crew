using Godot;
using System;
using System.Collections.Generic;

public partial class Spawn : Node3D
{
    [Export] public float SpawnRadius = 50.0f;
    [Export] public float RotationSpeed = 2.0f; // Snelheid van het draaien

    private MeshInstance3D _hudMesh; // Referentie opslaan om later te draaien

    public override void _Ready()
    {
        GetTree().CreateTimer(0.2f).Timeout += VerdeelNPCs;
    }

    // _Process wordt elke frame aangeroepen
    public override void _Process(double delta)
    {
        // Als de HUD mesh bestaat, draai hem rond de Y-as
        if (IsInstanceValid(_hudMesh))
        {
            _hudMesh.RotateY((float)(RotationSpeed * delta));
        }
    }

    private void VerdeelNPCs()
    {
        var nodesInGroup = GetTree().GetNodesInGroup("NPC");
        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        List<CharacterBody3D> echteNPCs = new List<CharacterBody3D>();

        foreach (Node node in nodesInGroup)
        {
            if (node is CharacterBody3D npc)
            {
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
                    if (npc.HasMethod("PickNewTarget")) npc.CallDeferred("PickNewTarget");
                }
            }
        }

        if (echteNPCs.Count > 0)
        {
            int randomIndex = rng.RandiRange(0, echteNPCs.Count - 1);
            CharacterBody3D target = echteNPCs[randomIndex];
            target.AddToGroup("Target");
            MaakTargetHUD(target);
        }
    }

    private void MaakTargetHUD(CharacterBody3D target)
    {
        Camera3D camera = GetViewport().GetCamera3D();
        if (camera == null) return;

        MeshInstance3D targetMeshNode = null;
        foreach (var child in target.GetChildren())
        {
            if (child is MeshInstance3D m) { targetMeshNode = m; break; }
        }

        if (targetMeshNode != null && targetMeshNode.Mesh != null)
        {
            _hudMesh = new MeshInstance3D(); // Sla op in de variabele buiten de functie
            _hudMesh.Mesh = targetMeshNode.Mesh;
            _hudMesh.Name = "HUD_TargetPreview";

            camera.AddChild(_hudMesh);

            // Jouw exacte waarden
            _hudMesh.Position = new Vector3(-0.250f, -0.125f, -0.325f);
            _hudMesh.Scale = new Vector3(0.001f, 0.001f, 0.001f);

            StandardMaterial3D hudMat = new StandardMaterial3D();
            if (targetMeshNode.GetActiveMaterial(0) is StandardMaterial3D origMat)
            {
                hudMat.AlbedoColor = origMat.AlbedoColor;
                hudMat.AlbedoTexture = origMat.AlbedoTexture;
            }

            //hudMat.NoDepthTest = true;
            hudMat.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
            _hudMesh.MaterialOverride = hudMat;
            _hudMesh.Layers = 2;
        }
    }
}
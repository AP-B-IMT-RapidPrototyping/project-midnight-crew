using Godot;
using System;
using System.Collections.Generic;

public partial class Spawn : Node3D
{
    [Export] public Vector3 MapSize = new Vector3(300, 0, 300);
    [Export] public float RotationSpeed = 2.0f;

    // --- NIEUWE EXPORTS ---
    [Export] public int GekoppeldLevel = 1;                    // Stel dit in per spawner (1 of 2)
    [Export] public string TargetNpcGroup = "NPC";             // "NPC" voor level 1, "NPC2" voor level 2
    [Export] public string TargetSpawnGroup = "Level 1 SPAWN"; // "Level 1 SPAWN", "Level 2 SPAWN"

    private MeshInstance3D _hudMesh;

    public override void _Ready()
    {
        // We halen eerst de GlobalData singleton op via de root van de scene tree
        var globalData = GetNodeOrNull<GlobalData>("/root/GlobalData");

        if (globalData != null)
        {
            // CHECK: Mag deze specifieke spawner draaien voor het huidige level?
            if (globalData.HuidigSpeelLevel == GekoppeldLevel)
            {
                GD.Print($"[SPAWNER] Level {GekoppeldLevel} is actief. NPCs worden verdeeld...");
                // Delay zodat de NavigationServer tijd heeft om alle regio's correct te registreren
                GetTree().CreateTimer(0.3f).Timeout += VerdeelNPCs;
            }
            else
            {
                GD.Print($"[SPAWNER] Level {GekoppeldLevel} is inactief (Huidig level is {globalData.HuidigSpeelLevel}). Spawner doet niks.");
            }
        }
        else
        {
            GD.PrintErr("FOUT: 'GlobalData' Autoload/Singleton kon niet worden gevonden! Check je Projectinstellingen.");
        }
    }

    public override void _Process(double delta)
    {
        if (IsInstanceValid(_hudMesh))
        {
            _hudMesh.RotateY((float)(RotationSpeed * delta));
        }
    }

    private void VerdeelNPCs()
    {
        var nodesInGroup = GetTree().GetNodesInGroup(TargetNpcGroup);
        if (nodesInGroup.Count == 0) return;

        var targetRegions = GetTree().GetNodesInGroup(TargetSpawnGroup);
        Rid targetRegionRid = new Rid();

        foreach (Node node in targetRegions)
        {
            if (node is NavigationRegion3D region)
            {
                targetRegionRid = region.GetRid();
                break;
            }
        }

        if (targetRegionRid.IsValid == false)
        {
            GD.PrintErr($"FOUT: Geen NavigationRegion3D gevonden in de groep '{TargetSpawnGroup}'!");
            return;
        }

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

                while (spawnPoint == Vector3.Zero && pogingen < 30)
                {
                    Vector3 randomPos = new Vector3(
                        rng.RandfRange(-MapSize.X / 2, MapSize.X / 2),
                        5.0f,
                        rng.RandfRange(-MapSize.Z / 2, MapSize.Z / 2)
                    );

                    Vector3 testPoint = NavigationServer3D.MapGetClosestPoint(mapRid, randomPos);
                    Rid gekozenRegionRid = NavigationServer3D.MapGetClosestPointOwner(mapRid, testPoint);

                    if (gekozenRegionRid == targetRegionRid)
                    {
                        spawnPoint = testPoint;
                    }

                    pogingen++;
                }

                if (spawnPoint != Vector3.Zero)
                {
                    npc.GlobalPosition = spawnPoint + new Vector3(0, 0.2f, 0);
                    agent.SetNavigationMap(mapRid);

                    if (npc.HasMethod("PickNewTarget"))
                        npc.CallDeferred("PickNewTarget");
                }
                else
                {
                    GD.Print($"NPC {npc.Name} kon niet spawnen op {TargetSpawnGroup} binnen het aantal pogingen.");
                }
            }
        }

        // Target selectie (Gebeurt nu automatisch alleen in de actieve spawner)
        if (echteNPCs.Count > 0)
        {
            int randomIndex = rng.RandiRange(0, echteNPCs.Count - 1);
            CharacterBody3D target = echteNPCs[randomIndex];
            target.AddToGroup("Target");
            MaakTargetHUD(target);
            GD.Print($"[GLOBALDATA] Target succesvol gekozen voor Level {GekoppeldLevel} uit groep: {TargetNpcGroup}");
        }
    }

    private void MaakTargetHUD(CharacterBody3D target)
    {
        var playerCam = GetNodeOrNull<Camera3D>("/root/Main/SettingMain/Player/CharacterBody3D/Camera3D");
        if (playerCam == null) return;

        MeshInstance3D targetMeshNode = null;
        foreach (var child in target.GetChildren())
        {
            if (child is MeshInstance3D m) { targetMeshNode = m; break; }
        }

        if (targetMeshNode != null && targetMeshNode.Mesh != null)
        {
            _hudMesh = new MeshInstance3D();
            _hudMesh.Mesh = targetMeshNode.Mesh;
            _hudMesh.Name = "HUD_TargetPreview_" + TargetNpcGroup;

            playerCam.AddChild(_hudMesh);

            _hudMesh.Position = new Vector3(-0.225f, -0.125f, -0.325f);
            _hudMesh.Scale = new Vector3(0.001f, 0.001f, 0.001f);

            StandardMaterial3D hudMat = new StandardMaterial3D();
            if (targetMeshNode.GetActiveMaterial(0) is StandardMaterial3D origMat)
            {
                hudMat.AlbedoColor = origMat.AlbedoColor;
                hudMat.AlbedoTexture = origMat.AlbedoTexture;
            }

            hudMat.ShadingMode = StandardMaterial3D.ShadingModeEnum.Unshaded;
            _hudMesh.MaterialOverride = hudMat;
            _hudMesh.Layers = 2;
        }
    }
}
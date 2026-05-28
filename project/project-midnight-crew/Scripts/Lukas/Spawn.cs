using Godot;
using System;
using System.Collections.Generic;

public partial class Spawn : Node3D
{
    [Export] public Vector3 MapSize = new Vector3(300, 0, 300);
    [Export] public float RotationSpeed = 2.0f;

    // --- NIEUWE EXPORTS ---
    [Export] public int GekoppeldLevel = 1;                     // Stel dit in per spawner (1 of 2)
    [Export] public string TargetNpcGroup = "NPC";              // "NPC" voor level 1, "NPC2" voor level 2
    [Export] public string TargetSpawnGroup = "Level 1 SPAWN"; // "Level 1 SPAWN", "Level 2 SPAWN"

    private MeshInstance3D _hudMesh;

    public override void _Ready()
    {
        AddToGroup("Spawners");
        GD.Print($"[SPAWNER] Level {GekoppeldLevel} staat stand-by op het startscherm...");
    }

    public void CheckEnStartSpawn()
    {
        var globalData = GetNodeOrNull<GlobalData>("/root/GlobalData");

        if (globalData != null)
        {
            if (globalData.HuidigSpeelLevel == GekoppeldLevel)
            {
                if (GekoppeldLevel == 4)
                {
                    GD.Print($"[SPAWNER] Level 4 geactiveerd! Wacht 10 seconden met spawnen van NPCs...");
                    GetTree().CreateTimer(10.0f).Timeout += VerdeelNPCs; // 10 seconden vertraging
                }
                else
                {
                    GD.Print($"[SPAWNER] Level {GekoppeldLevel} NU geactiveerd door knop! NPCs worden verdeeld...");
                    GetTree().CreateTimer(0.3f).Timeout += VerdeelNPCs;  // Oude vertraging voor levels 1, 2, 3
                }
            }
            else
            {
                GD.Print($"[SPAWNER] Level {GekoppeldLevel} hoeft niks te doen voor huidige level {globalData.HuidigSpeelLevel}.");
            }
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
        int npcCount = nodesInGroup.Count;
        if (npcCount == 0) return;

        var targetRegions = GetTree().GetNodesInGroup(TargetSpawnGroup);
        NavigationRegion3D actieveRegionNode = null;
        Rid targetRegionRid = new Rid();

        foreach (Node node in targetRegions)
        {
            if (node is NavigationRegion3D region)
            {
                actieveRegionNode = region;
                targetRegionRid = region.GetRid();
                break;
            }
        }

        if (targetRegionRid.IsValid == false || actieveRegionNode == null)
        {
            GD.PrintErr($"FOUT: Geen NavigationRegion3D gevonden in de groep '{TargetSpawnGroup}'!");
            return;
        }

        RandomNumberGenerator rng = new RandomNumberGenerator();
        rng.Randomize();

        List<CharacterBody3D> echteNPCs = new List<CharacterBody3D>();

        // --- AUTOMATISCHE DAKEN-DETECTIE VOOR LEVEL 3 ---
        List<StaticBody3D> gevondenDaken = new List<StaticBody3D>();

        if (GekoppeldLevel == 3)
        {
            foreach (Node child in actieveRegionNode.GetChildren())
            {
                if (child is StaticBody3D dak)
                {
                    gevondenDaken.Add(dak);
                }
            }
        }

        // --- 🔥 VEILIGE AREA3D-DETECTIE VIA GROEP VOOR LEVEL 4 🔥 ---
        Area3D spawnAreaLevel4 = null;
        if (GekoppeldLevel == 4)
        {
            var areaNodes = GetTree().GetNodesInGroup("SpawnAreaLevel4");
            if (areaNodes.Count > 0 && areaNodes[0] is Area3D gevondenArea)
            {
                spawnAreaLevel4 = gevondenArea;
            }
            else
            {
                GD.PrintErr("WAARSCHUWING: Level 4 actief, maar Area3D in groep 'SpawnAreaLevel4' niet gevonden!");
            }
        }

        bool gebruikDakSysteem = (GekoppeldLevel == 3 && gevondenDaken.Count > 0);
        int dakIndex = 0;

        foreach (Node node in nodesInGroup)
        {
            if (node is CharacterBody3D npc)
            {
                echteNPCs.Add(npc);

                var agent = npc.GetNodeOrNull<NavigationAgent3D>("NavigationAgent3D");
                if (agent == null) continue;

                Rid mapRid = agent.GetNavigationMap();
                Vector3 spawnPoint = Vector3.Zero;

                if (gebruikDakSysteem)
                {
                    StaticBody3D gekozenDak = (dakIndex < gevondenDaken.Count)
                        ? gevondenDaken[dakIndex]
                        : gevondenDaken[rng.RandiRange(0, gevondenDaken.Count - 1)];

                    dakIndex++;

                    Vector3 dakPositie = gekozenDak.GlobalPosition;
                    dakPositie.Y = 5.0f;

                    Vector3 testPoint = NavigationServer3D.MapGetClosestPoint(mapRid, dakPositie);

                    float jitterX = rng.RandfRange(-1.5f, 1.5f);
                    float jitterZ = rng.RandfRange(-1.5f, 1.5f);

                    Vector3 gecorrigeerdPunt = testPoint + new Vector3(jitterX, 0, jitterZ);
                    spawnPoint = NavigationServer3D.MapGetClosestPoint(mapRid, gecorrigeerdPunt);
                }
                else
                {
                    // --- GEOPTIMALISEERDE METHODE (Voor Level 1, 2, Tutorial & 4) ---
                    int pogingen = 0;
                    int maxPogingen = (GekoppeldLevel == 4) ? 300 : 60; // Verhoogd naar 300 voor kleinere areas

                    while (spawnPoint == Vector3.Zero && pogingen < maxPogingen)
                    {
                        Vector3 randomPos = new Vector3(
                            rng.RandfRange(-MapSize.X / 2, MapSize.X / 2),
                            5.0f,
                            rng.RandfRange(-MapSize.Z / 2, MapSize.Z / 2)
                        );

                        Vector3 globaleTestPos = GlobalPosition + randomPos;
                        Vector3 testPoint = NavigationServer3D.MapGetClosestPoint(mapRid, globaleTestPos);

                        if (testPoint != Vector3.Zero)
                        {
                            Rid gevondenOwner = NavigationServer3D.MapGetClosestPointOwner(mapRid, testPoint);
                            bool isGeldigPunt = false;

                            if (gevondenOwner == targetRegionRid)
                            {
                                isGeldigPunt = true;
                            }
                            else if (gevondenOwner.IsValid == false && globaleTestPos.DistanceTo(testPoint) < 15.0f)
                            {
                                isGeldigPunt = true;
                            }

                            // --- CHECK VOOR LEVEL 4 AREA ---
                            if (isGeldigPunt && GekoppeldLevel == 4 && spawnAreaLevel4 != null)
                            {
                                var spaceState = GetWorld3D().DirectSpaceState;
                                var query = new PhysicsPointQueryParameters3D();
                                query.Position = testPoint + new Vector3(0, 0.5f, 0);
                                query.CollideWithAreas = true;
                                query.CollideWithBodies = false;

                                var resultaten = spaceState.IntersectPoint(query);
                                bool bevindtZichInArea = false;

                                foreach (var resultaat in resultaten)
                                {
                                    if (resultaat.ContainsKey("collider") && resultaat["collider"].As<Area3D>() == spawnAreaLevel4)
                                    {
                                        bevindtZichInArea = true;
                                        break;
                                    }
                                }

                                if (!bevindtZichInArea)
                                {
                                    isGeldigPunt = false; // Buiten de area? Afkeuren!
                                }
                            }

                            if (isGeldigPunt)
                            {
                                spawnPoint = testPoint;
                            }
                        }
                        pogingen++;
                    }
                }

                // Spawn de NPC op de gevonden plek
                if (spawnPoint != Vector3.Zero)
                {
                    npc.GlobalPosition = spawnPoint + new Vector3(0, 0.2f, 0);
                    agent.SetNavigationMap(mapRid);

                    if (npc.HasMethod("PickNewTarget"))
                        npc.CallDeferred("PickNewTarget");
                }
                else
                {
                    GD.Print($"NPC {npc.Name} kon echt nergens spawnen in Level 4 area.");
                }
            }
        }

        // Target selectie
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
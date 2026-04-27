using Godot;
using System;

public partial class Sniper : Node3D
{
	[Export] private Timer fireRateTimer;
	[Export] Marker3D muzzle;
	[Export] private Node3D weaponMount;

	public override void _Process(double delta)
    {
        // Volg de weapon mount position en rotation
        GlobalTransform = weaponMount.GlobalTransform;
    }

	public void OnShoot(RayCast3D raycast)
    {
        // Check of we nog in cooldown zitten
        if (!fireRateTimer.IsStopped())
            return;

        // Start de cooldown timer
        fireRateTimer.Start();

        // Bepaal het eindpunt van de beam
        Vector3 beamEnd = raycast.IsColliding()
            ? raycast.GetCollisionPoint()
            : muzzle.GlobalPosition - raycast.GlobalBasis.Z * 100;

        // Teken de beam naar het hit point
        ShowFlash(muzzle.GlobalPosition, beamEnd);

        // Check target hit
        if (raycast.GetCollider() is Targets targetHit)
            targetHit.OnHit();

        GD.Print("Railgun fired!");
    }

	public void ShowFlash(Vector3 start, Vector3 end)
	{
        // Maak een cilinder mesh voor de beam
        var beamMesh = new CylinderMesh();
        beamMesh.TopRadius = 0.01f;
        beamMesh.BottomRadius = 0.01f;
        beamMesh.Height = 1.0f;

        // Maak een unshaded material (belangrijk: Transparantie aanzetten voor vervagen)
        var material = new StandardMaterial3D();
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        material.AlbedoColor = Colors.Yellow;
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha; // Nodig voor vervagen

        // Maak een MeshInstance3D
        var beamInstance = new MeshInstance3D
        {
            Mesh = beamMesh,
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        GetTree().Root.AddChild(beamInstance);

        // Positie en rotatie berekeningen
        Vector3 direction = end - start;
        float distance = direction.Length();
        beamInstance.GlobalPosition = start + direction / 2;
        beamInstance.LookAt(end, Vector3.Up);
        beamInstance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
        beamInstance.Scale = new Vector3(1, distance, 1);

        // --- EASE OUT EFFECT MET TWEEN ---
        float duration = 0.2f; // Iets langer dan 0.05 voor een zichtbaar effect
        var tween = GetTree().CreateTween();

        // We animeren twee eigenschappen tegelijk voor een mooi effect:
        // 1. De dikte (Scale X en Z) gaat naar 0
        tween.Parallel().TweenProperty(beamInstance, "scale", new Vector3(0, distance, 0), duration)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.Out);

        // 2. De kleur vervaagt (Alpha naar 0)
        tween.Parallel().TweenProperty(material, "albedo_color:a", 0.0f, duration)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.Out);

        // Verwijder de beam zodra de animatie klaar is
        tween.Finished += () => beamInstance.QueueFree();
	}
}

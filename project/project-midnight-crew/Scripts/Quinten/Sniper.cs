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

		// Maak een unshaded material in cyan kleur
        var material = new StandardMaterial3D();
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        material.AlbedoColor = Colors.Yellow;

        // Maak een MeshInstance3D met de mesh en material
		var beamInstance = new MeshInstance3D
        {
            Mesh = beamMesh,
            MaterialOverride = material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };

        // Voeg toe aan de scene root
        GetTree().Root.AddChild(beamInstance);

		// Bereken richting en afstand
        Vector3 direction = end - start;
        float distance = direction.Length();

		// Positioneer in het midden tussen start en einde
        beamInstance.GlobalPosition = start + direction / 2;
        // Roteer zodat de beam naar het eindpunt wijst
        beamInstance.LookAt(end, Vector3.Up);
        // Draai 90 graden zodat de cilinder horizontaal ligt
        beamInstance.RotateObjectLocal(Vector3.Right, Mathf.Pi / 2);
        // Schaal de beam op basis van de afstand
        beamInstance.Scale = new Vector3(1, distance, 1);

		// Verwijder de beam na 0.05 seconden
        GetTree().CreateTimer(0.05).Timeout += () => beamInstance.QueueFree();
	}
}

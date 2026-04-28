using Godot;
using System;
using System.ComponentModel;

public partial class StartScreen : Node3D
{
	[Export] private StaticBody3D startKnop;
	[Export] private StaticBody3D settingKnop;
	[Export] private StaticBody3D quitKnop;
	[Export] private StaticBody3D terugKnop;
	[Export] private MeshInstance3D startKleur;
	[Export] private MeshInstance3D settingKleur;
	[Export] private MeshInstance3D quitKleur;
	[Export] private MeshInstance3D terugKleur;
	[Export] private AnimationPlayer SettingDraaien;

	private Color hoverKleur = new Color(0, 0, 0);
	private Color normalKleur = new Color(1, 1, 1);
	
	public override void _Ready()
	{
		if (startKnop != null)
		{
			startKnop.InputEvent += OnStartInput;
			startKnop.MouseEntered += OnStartHover;
            startKnop.MouseExited += OnStartExit;
		
		}
		if (settingKnop != null)
		{
			settingKnop.InputEvent += OnSettingInput;
			settingKnop.MouseEntered += OnSettingHover;
            settingKnop.MouseExited += OnSettingExit;
		} 
		
		if (quitKnop != null)
		{
			quitKnop.InputEvent += OnQuitInput;
			quitKnop.MouseEntered += OnQuitHover;
            quitKnop.MouseExited += OnQuitExit;
		} 

		if (terugKnop != null)
		{
			terugKnop.InputEvent += OnTerugInput;
			terugKnop.MouseEntered += OnTerugHover;
            terugKnop.MouseExited += OnTerugExit;
		} 
		
	}

	private void OnStartHover() => VeranderKleur(startKleur, hoverKleur);
    private void OnStartExit()  => VeranderKleur(startKleur, normalKleur);

    private void OnSettingHover() => VeranderKleur(settingKleur, hoverKleur);
    private void OnSettingExit()  => VeranderKleur(settingKleur, normalKleur);

    private void OnQuitHover() => VeranderKleur(quitKleur, hoverKleur);
    private void OnQuitExit()  => VeranderKleur(quitKleur, normalKleur);

    private void OnTerugHover() => VeranderKleur(terugKleur, hoverKleur);
    private void OnTerugExit()  => VeranderKleur(terugKleur, normalKleur);

	private void VeranderKleur(MeshInstance3D mesh, Color kleur)
    {
        if (mesh != null)
        {
            var mat = mesh.GetActiveMaterial(0) as StandardMaterial3D;
            if (mat != null)
            {
                mat.AlbedoColor = kleur;
            }
        }
    }

	private void OnStartInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Start();
            }
        }
    }

	private void OnSettingInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Setting();
            }
        }
	}

	private void OnQuitInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Quit();
            }
        }
    }

	private void OnTerugInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Terug();
            }
        }
    }

	public void Start()
	{
		GD.Print("Start");
	}

	public void Setting()
	{
		if(SettingDraaien != null)
		{
			SettingDraaien.Play("Setting_draaien");
		}

		GD.Print("Settings");
	}

	public void Quit()
	{
		GD.Print("Quit");
		GetTree().Quit();
	}

	public void Terug()
	{
		if(SettingDraaien != null)
		{
			SettingDraaien.Play("Setting_terug");
		}

		GD.Print("Terug");
	}
}

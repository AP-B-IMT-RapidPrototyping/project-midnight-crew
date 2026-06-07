using Godot;
using System;
using System.ComponentModel;

public partial class StartScreen : Node3D
{
	[Export] private Node3D settingmain;
	[Export] private Camera3D settingmainCamera;
	[Export] private Camera3D startscreenCamera;
	[Export] private StaticBody3D Level1Knop;
	[Export] private StaticBody3D Level2Knop;	
	[Export] private StaticBody3D Level3Knop;	
	[Export] private StaticBody3D Level4Knop;
    [Export] private StaticBody3D GeheimLevelKnop;
	[Export] private StaticBody3D TerugStartKnop;	
	[Export] private MeshInstance3D Level1Kleur;
	[Export] private MeshInstance3D Level2Kleur;	
	[Export] private MeshInstance3D Level3Kleur;	
	[Export] private MeshInstance3D Level4Kleur;
    [Export] private MeshInstance3D GeheimLevelKleur;
	[Export] private MeshInstance3D TerugStartKleur;	
	[Export] private StaticBody3D startKnop;
	[Export] private StaticBody3D settingKnop;
	[Export] private StaticBody3D quitKnop;
	[Export] private StaticBody3D terugKnop;
	[Export] private MeshInstance3D startKleur;
	[Export] private MeshInstance3D settingKleur;
	[Export] private MeshInstance3D quitKleur;
	[Export] private MeshInstance3D terugKleur;
	[Export] private AnimationPlayer AnimatieDraaien;
	[Export] private Label labelSlow;
	[Export] private Control gameUI;
	[Export] public Spawn npcSpawner;
	[Export] private Marker3D startPuntLevel1;
	[Export] private Marker3D startPuntLevel2;
	[Export] private Marker3D startPuntLevel3;
    [Export] private Marker3D startPuntLevel4;
    [Export] private Marker3D startGeheimLevel;
	[Export] private PlayerScript echteSpeler;


	private Vector3 settingMainPositie;
	private Color hoverKleur = new Color(0, 0, 0);
	private Color normalKleur = new Color(1, 1, 1);
	private Color GroenKleur = new Color(0, 1, 0);
	private Color GeelKleur = new Color(1, 1, 0);
	private Color RoodKleur = new Color(1, 0, 0);
	
	public override void _Ready()
	{
		if(settingmain != null)
		{
			settingMainPositie = settingmain.Position;
		}

		startscreenCamera.Current = true;
		settingmain.Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
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

		if (Level1Knop != null)
		{
			Level1Knop.InputEvent += OnLevel1Input;
			Level1Knop.MouseEntered += OnLevel1Hover;
            Level1Knop.MouseExited += OnLevel1Exit;
		} 
		
		if (Level2Knop != null)
		{
			Level2Knop.InputEvent += OnLevel2Input;
			Level2Knop.MouseEntered += OnLevel2Hover;
            Level2Knop.MouseExited += OnLevel2Exit;
		} 

		if (Level3Knop != null)
		{
			Level3Knop.InputEvent += OnLevel3Input;
			Level3Knop.MouseEntered += OnLevel3Hover;
            Level3Knop.MouseExited += OnLevel3Exit;
		}

		if (Level4Knop != null)
		{
			Level4Knop.InputEvent += OnLevel4Input;
			Level4Knop.MouseEntered += OnLevel4Hover;
            Level4Knop.MouseExited += OnLevel4Exit;
		} 

		if (TerugStartKnop != null)
		{
			TerugStartKnop.InputEvent += OnTerugStartInput;
			TerugStartKnop.MouseEntered += OnTerugStartHover;
            TerugStartKnop.MouseExited += OnTerugStartExit;
		} 

        if (GeheimLevelKnop != null)
		{
			GeheimLevelKnop.InputEvent += OnGeheimLevelInput;
			GeheimLevelKnop.MouseEntered += OnGeheimLevelHover;
            GeheimLevelKnop.MouseExited += OnGeheimLevelExit;
		} 
	}

	//startkleur hover
	private void OnStartHover() => VeranderKleur(startKleur, hoverKleur);
    private void OnStartExit()  => VeranderKleur(startKleur, normalKleur);

	//settingkleur hover
    private void OnSettingHover() => VeranderKleur(settingKleur, hoverKleur);
    private void OnSettingExit()  => VeranderKleur(settingKleur, normalKleur);

	//quitkleur hover
    private void OnQuitHover() => VeranderKleur(quitKleur, hoverKleur);
    private void OnQuitExit()  => VeranderKleur(quitKleur, normalKleur);

	//terugkleur hover
    private void OnTerugHover() => VeranderKleur(terugKleur, hoverKleur);
    private void OnTerugExit()  => VeranderKleur(terugKleur, normalKleur);
	
	//level1kleur hover
	private void OnLevel1Hover() => VeranderKleur(Level1Kleur, hoverKleur);
    private void OnLevel1Exit()  => VeranderKleur(Level1Kleur, GroenKleur);

	//level2kleur hover
	private void OnLevel2Hover() => VeranderKleur(Level2Kleur, hoverKleur);
    private void OnLevel2Exit()  => VeranderKleur(Level2Kleur, GeelKleur);

	//level3kleur hover
	private void OnLevel3Hover() => VeranderKleur(Level3Kleur, hoverKleur);
    private void OnLevel3Exit()  => VeranderKleur(Level3Kleur, GeelKleur);

	//level4kleur hover
	private void OnLevel4Hover() => VeranderKleur(Level4Kleur, hoverKleur);
    private void OnLevel4Exit()  => VeranderKleur(Level4Kleur, RoodKleur);

	//terugstartkleur hover
	private void OnTerugStartHover() => VeranderKleur(TerugStartKleur, hoverKleur);
    private void OnTerugStartExit()  => VeranderKleur(TerugStartKleur, normalKleur);
    private void OnGeheimLevelHover() => VeranderKleur(GeheimLevelKleur, hoverKleur);
    private void OnGeheimLevelExit()  => VeranderKleur(GeheimLevelKleur, normalKleur);

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

	private void OnLevel1Input(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Level1();
            }
        }
    }

	private void OnLevel2Input(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Level2();
            }
        }
    }

	private void OnLevel3Input(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Level3();
            }
        }
    }

	private void OnLevel4Input(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Level4();
            }
        }
    }

	private void OnTerugStartInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                TerugStart();
            }
        }
    }

    private void OnGeheimLevelInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                GeheimLevel();
            }
        }
    }

    public void TerugNaarMenu()
    {
        var globalData = GetNode<GlobalData>("/root/GlobalData");
        if (globalData != null) globalData.HuidigSpeelLevel = 0;

        GetTree().ReloadCurrentScene();
    }

    public void Start()
	{
		GD.Print("Start");

		Level1Kleur.Visible = true;
		Level2Kleur.Visible = true;
		Level3Kleur.Visible = true;
		Level4Kleur.Visible = true;
		TerugStartKleur.Visible = true;

		if(AnimatieDraaien != null)
		{
			AnimatieDraaien.Play("start");
		}
	}

	public void Setting()
	{
		if(AnimatieDraaien != null)
		{
			AnimatieDraaien.Play("Setting_draaien");
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
		if(AnimatieDraaien != null)
		{
			AnimatieDraaien.Play("Setting_terug");
		}

		GD.Print("Terug");
	}

    public void Level1()
    {
        var globalData = GetNode<GlobalData>("/root/GlobalData");
        globalData.HuidigSpeelLevel = 1;

        startscreenCamera.Current = false;
        settingmainCamera.Current = true;
        this.Visible = false;
        settingmain.Visible = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        if (echteSpeler != null && startPuntLevel1 != null)
        {
            echteSpeler.GlobalPosition = startPuntLevel1.GlobalPosition;
            echteSpeler.GlobalRotation = startPuntLevel1.GlobalRotation;
            echteSpeler.Velocity = Vector3.Zero;
        }

        labelSlow.Visible = true;
        GD.Print("Level1");

        GetTree().CallGroup("Spawners", "CheckEnStartSpawn");
    }

    public void Level2()
    {
        var globalData = GetNode<GlobalData>("/root/GlobalData");
        if (globalData.MaxVrijgespeeldLevel < 2)
        {
            GD.Print("Je moet eerst Level 1 halen!");
            return;
        }
        globalData.HuidigSpeelLevel = 2;

        if (echteSpeler != null && startPuntLevel2 != null)
        {
            echteSpeler.GlobalPosition = startPuntLevel2.GlobalPosition;
            echteSpeler.GlobalRotation = startPuntLevel2.GlobalRotation;
            echteSpeler.Velocity = Vector3.Zero;
        }

        GD.Print("Level2");
        labelSlow.Visible = true;
        startscreenCamera.Current = false;
        settingmainCamera.Current = true;
        this.Visible = false;
        settingmain.Visible = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        GetTree().CallGroup("Spawners", "CheckEnStartSpawn");
    }

    public void Level3()
	{
		var globalData = GetNode<GlobalData>("/root/GlobalData");
        if (globalData.MaxVrijgespeeldLevel < 3)
        {
            GD.Print("Je moet eerst Level 2 halen!");
            return;
        }
        globalData.HuidigSpeelLevel = 3;

        if (echteSpeler != null && startPuntLevel3 != null)
        {
            echteSpeler.GlobalPosition = startPuntLevel3.GlobalPosition;
            echteSpeler.GlobalRotation = startPuntLevel3.GlobalRotation;
            echteSpeler.Velocity = Vector3.Zero;
        }

		GD.Print("Level3");
        labelSlow.Visible = true;
        startscreenCamera.Current = false;
        settingmainCamera.Current = true;
        this.Visible = false;
        settingmain.Visible = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        GetTree().CallGroup("Spawners", "CheckEnStartSpawn");
	}

	public void Level4()
	{
        var globalData = GetNode<GlobalData>("/root/GlobalData");
        if (globalData.MaxVrijgespeeldLevel < 4)
        {
            GD.Print("Je moet eerst Level 3 halen!");
            return;
        }
        globalData.HuidigSpeelLevel = 4;

        if (echteSpeler != null && startPuntLevel4 != null)
        {
            echteSpeler.GlobalPosition = startPuntLevel4.GlobalPosition;
            echteSpeler.GlobalRotation = startPuntLevel4.GlobalRotation;
            echteSpeler.Velocity = Vector3.Zero;
        }

		GD.Print("Level4");
        labelSlow.Visible = true;
        startscreenCamera.Current = false;
        settingmainCamera.Current = true;
        this.Visible = false;
        settingmain.Visible = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        GetTree().CallGroup("Spawners", "CheckEnStartSpawn");
	}

	public async void TerugStart()
	{
		GD.Print("TerugStart");

		if(AnimatieDraaien != null)
		{
			AnimatieDraaien.Play("terug_start");

			//Delay tot de animatie stopt
			await ToSignal(AnimatieDraaien, "animation_finished");
			Level1Kleur.Visible = false;
			Level2Kleur.Visible = false;
			Level3Kleur.Visible = false;
			Level4Kleur.Visible = false;
			TerugStartKleur.Visible = false;
		}
	}

    public void GeheimLevel()
	{
        var globalData = GetNode<GlobalData>("/root/GlobalData");
        globalData.HuidigSpeelLevel = 5;

        if (echteSpeler != null && startGeheimLevel != null)
        {
            echteSpeler.GlobalPosition = startGeheimLevel.GlobalPosition;
            echteSpeler.GlobalRotation = startGeheimLevel.GlobalRotation;
            echteSpeler.Velocity = Vector3.Zero;
        }

		GD.Print("Level5");
        labelSlow.Visible = true;
        startscreenCamera.Current = false;
        settingmainCamera.Current = true;
        this.Visible = false;
        settingmain.Visible = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;

        GetTree().CallGroup("Spawners", "CheckEnStartSpawn");
	}
}

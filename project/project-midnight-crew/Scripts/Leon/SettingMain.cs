using Godot;
using System;

public partial class SettingMain : Node3D
{
	public static bool IsGepauzeerd { get; private set; } = false;
	[Export] private Node3D startMain;
	[Export] private Camera3D settingmainCamera;
	[Export] private StaticBody3D resumeKnop;
	[Export] private StaticBody3D settingKnop;
	[Export] private StaticBody3D backMainKnop;
	[Export] private StaticBody3D quitKnop;

	[Export] private MeshInstance3D resumeKleur;
	[Export] private MeshInstance3D settingKleur;
	[Export] private MeshInstance3D backMainKeur;
	[Export] private MeshInstance3D quitKleur;
	[Export] private AnimationPlayer animation;
	[Export] private AnimationPlayer animationSniper;
	[Export] private Node3D buttons;
	[Export] private Label labelSlow;
	private int _teller = 0;

	private Color hoverKleur = new Color(0, 0, 0);
	private Color normalKleur = new Color(1, 1, 1);

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		if (resumeKnop != null)
		{
			resumeKnop.InputEvent += OnResumeInput;
			resumeKnop.MouseEntered += OnResumeHover;
            resumeKnop.MouseExited += OnResumeExit;
		
		}

		if (settingKnop != null)
		{
			settingKnop.InputEvent += OnSettingInput;
			settingKnop.MouseEntered += OnSettingHover;
            settingKnop.MouseExited += OnSettingExit;
		} 
		
		if (backMainKnop != null)
		{
			backMainKnop.InputEvent += OnBackMainInput;
			backMainKnop.MouseEntered += OnBackMainHover;
            backMainKnop.MouseExited += OnBackMainExit;
		} 

		if (quitKnop != null)
		{
			quitKnop.InputEvent += OnQuitInput;
			quitKnop.MouseEntered += OnQuitHover;
            quitKnop.MouseExited += OnQuitExit;
		}
	}

	async public override void _Input(InputEvent @event)
    {
		if (!this.Visible)
        {
            return;
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
			if(_teller == 0)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				buttons.Visible = true;
				animation.Play("Animation_DOWN");
				animationSniper.Play("Sniper-move-down");
				_teller++;

				IsGepauzeerd = true;
			}
			else
			{
				IsGepauzeerd = false;
				Input.MouseMode = Input.MouseModeEnum.Hidden;
				Input.MouseMode = Input.MouseModeEnum.Captured;
			    animation.Play("Animation_UP");
				animationSniper.Play("Sniper-move-up");
				await ToSignal(animation, "animation_finished");
				
				buttons.Visible = false;
				_teller = 0;
			}	
            
        }
	}

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

	private void OnResumeHover() => VeranderKleur(resumeKleur, hoverKleur);
    private void OnResumeExit()  => VeranderKleur(resumeKleur, normalKleur);

	//settingkleur hover
    private void OnSettingHover() => VeranderKleur(settingKleur, hoverKleur);
    private void OnSettingExit()  => VeranderKleur(settingKleur, normalKleur);

	//quitkleur hover
    private void OnBackMainHover() => VeranderKleur(backMainKeur, hoverKleur);
    private void OnBackMainExit()  => VeranderKleur(backMainKeur, normalKleur);

	//terugkleur hover
    private void OnQuitHover() => VeranderKleur(quitKleur, hoverKleur);
    private void OnQuitExit()  => VeranderKleur(quitKleur, normalKleur);

	private void OnResumeInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                Resume();
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

	private void OnBackMainInput(Node camera, InputEvent @event, Vector3 position, Vector3 normal, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                BackMain();
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

	public async void Resume()
	{
		IsGepauzeerd = false;
		GD.Print("Resume");
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		Input.MouseMode = Input.MouseModeEnum.Captured;
		animationSniper.Play("Sniper-move-up");
		animation.Play("Animation_UP");	
		await ToSignal(animation, "animation_finished");
		buttons.Visible = false;
		_teller = 0;
	}

	public void Setting()
	{
		GD.Print("Settings");
	}

	public void BackMain()
	{
		labelSlow.Visible = false;
		GD.Print("Back to main menu");

		animation.Stop();
    	animationSniper.Stop();

		//GetTree().ChangeSceneToFile("res://Scenes/Leon/StartScreen.tscn");
		settingmainCamera.Current = false;
		this.Visible = false;
		startMain.Visible = true;

		_teller = 0;
    	IsGepauzeerd = false;
    	buttons.Visible = false;
	}

	public void Quit()
	{
		GD.Print("Quit");
		GetTree().Quit();
	}
}

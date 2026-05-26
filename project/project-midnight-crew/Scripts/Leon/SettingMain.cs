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

        // Zorgt ervoor dat de letters/knoppen visueel BOVENOP muren renderen
        ForceerNoDepthTest(resumeKleur);
        ForceerNoDepthTest(settingKleur);
        ForceerNoDepthTest(backMainKeur);
        ForceerNoDepthTest(quitKleur);

        // We vangen de input handmatig af via de camera, dus we zetten de automatische
        // picking van de objecten zelf uit om dubbele signalen of muren-glitches te voorkomen.
        if (resumeKnop != null) resumeKnop.InputRayPickable = false;
        if (settingKnop != null) settingKnop.InputRayPickable = false;
        if (backMainKnop != null) backMainKnop.InputRayPickable = false;
        if (quitKnop != null) quitKnop.InputRayPickable = false;
    }

    public override void _Process(double delta)
    {
        // Hover-effecten handmatig controleren als de game gepauzeerd is
        if (IsGepauzeerd && settingmainCamera != null)
        {
            HandhaafMuisHover();
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
            if (_teller == 0)
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
                SluitMenu();
            }
        }

        // Luister naar muisklikken wanneer het menu open is
        if (IsGepauzeerd && @event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                CheckMenuKlik(mouseEvent.Position);
            }
        }
    }

    private void CheckMenuKlik(Vector2 muisPos)
    {
        if (settingmainCamera == null) return;

        var spaceState = GetWorld3D().DirectSpaceState;
        Vector3 van = settingmainCamera.ProjectRayOrigin(muisPos);
        Vector3 naar = van + settingmainCamera.ProjectRayNormal(muisPos) * 10.0f;

        var query = PhysicsRayQueryParameters3D.Create(van, naar);

        // 🔥 MASKER = 4 betekent: Kijk EXCLUSIEF naar Physics Layer 3. 
        // Muren op Layer 1 worden nu compleet genegeerd, hoe dicht je er ook op staat!
        query.CollisionMask = 4;
        query.CollideWithBodies = true;

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            StaticBody3D hitKnop = result["collider"].As<StaticBody3D>();

            if (hitKnop == resumeKnop) SluitMenu();
            else if (hitKnop == settingKnop) Setting();
            else if (hitKnop == backMainKnop) BackMain();
            else if (hitKnop == quitKnop) Quit();
        }
    }

    private void HandhaafMuisHover()
    {
        Vector2 muisPos = GetViewport().GetMousePosition();
        var spaceState = GetWorld3D().DirectSpaceState;
        Vector3 van = settingmainCamera.ProjectRayOrigin(muisPos);
        Vector3 naar = van + settingmainCamera.ProjectRayNormal(muisPos) * 10.0f;

        var query = PhysicsRayQueryParameters3D.Create(van, naar);

        // 🔥 Ook hier kijken we alleen naar Layer 3 (waarde 4) voor de hover-kleuren
        query.CollisionMask = 4;

        var result = spaceState.IntersectRay(query);

        // Reset eerst alle kleuren naar normaal
        VeranderKleur(resumeKleur, normalKleur);
        VeranderKleur(settingKleur, normalKleur);
        VeranderKleur(backMainKeur, normalKleur);
        VeranderKleur(quitKleur, normalKleur);

        // Licht de knop op waar de muis nu over zweeft
        if (result.Count > 0)
        {
            StaticBody3D hitKnop = result["collider"].As<StaticBody3D>();
            if (hitKnop == resumeKnop) VeranderKleur(resumeKleur, hoverKleur);
            else if (hitKnop == settingKnop) VeranderKleur(settingKleur, hoverKleur);
            else if (hitKnop == backMainKnop) VeranderKleur(backMainKeur, hoverKleur);
            else if (hitKnop == quitKnop) VeranderKleur(quitKleur, hoverKleur);
        }
    }

    private async void SluitMenu()
    {
        IsGepauzeerd = false;
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        animationSniper.Play("Sniper-move-up");
        animation.Play("Animation_UP");
        await ToSignal(animation, "animation_finished");

        buttons.Visible = false;
        _teller = 0;
    }

    private void ForceerNoDepthTest(MeshInstance3D mesh)
    {
        if (mesh == null) return;
        var origineelMat = mesh.GetActiveMaterial(0) as StandardMaterial3D;
        if (origineelMat != null)
        {
            StandardMaterial3D uniekMat = (StandardMaterial3D)origineelMat.Duplicate();
            uniekMat.NoDepthTest = true;
            uniekMat.RenderPriority = 10;
            mesh.MaterialOverride = uniekMat;
        }
    }

    private void VeranderKleur(MeshInstance3D mesh, Color kleur)
    {
        if (mesh != null)
        {
            var mat = mesh.MaterialOverride as StandardMaterial3D ?? mesh.GetActiveMaterial(0) as StandardMaterial3D;
            if (mat != null)
            {
                mat.AlbedoColor = kleur;
            }
        }
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

        settingmainCamera.Current = false;
        this.Visible = false;
        startMain.Visible = true;

        _teller = 0;
        IsGepauzeerd = false;
        buttons.Visible = false;

        GetTree().Paused = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GD.Print("Terug naar menu");
        GetTree().ReloadCurrentScene();
    }

    public void Quit()
    {
        GD.Print("Quit");
        GetTree().Quit();
    }
}
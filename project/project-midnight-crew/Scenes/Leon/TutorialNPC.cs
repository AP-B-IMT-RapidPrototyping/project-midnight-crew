using Godot;
using System;

public partial class TutorialNPC : MeshInstance3D
{
    // Deze functie wordt dadelijk door de sniper aangeroepen
    public void NeemSchade()
    {
        GD.Print("Tutorial NPC is geraakt en verdwijnt!");

        // QueueFree() op de parent verwijdert de mesh én alle kinderen (StaticBody + Shape) in één klap!
        QueueFree();
    }
}
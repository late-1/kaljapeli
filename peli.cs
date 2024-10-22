using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Widgets;

namespace peli;

/// @author lauri
/// @version 22.10.2024
/// <summary>
/// 
/// </summary>
public class peli : PhysicsGame
{
    private const double NOPEUS = 240;
    private const double HYPPYNOPEUS = 600;
    private const int RUUDUN_KOKO = 50;
    private const int TOLKKI_MAARA = 10;

    private PlatformCharacter pelaaja1;
    private GameObject tausta;
    private int keratytTolkit = 0;

    private Image pelaajanKuva = LoadImage("ukko.png");
    private Image tolkkiKuva = LoadImage("tsingtao.png");
    private Image taustaKuva = LoadImage("tausta.png");
    private Image lasolKuva = LoadImage("lasol.png");
    
    private SoundEffect maaliAani = LoadSoundEffect("can-open-2.wav");
   

    public override void Begin()
    {
        Gravity = new Vector(0, -1000);

        LuoKentta();
        LisaaNappaimet();

        Camera.Follow(pelaaja1);
        Camera.ZoomFactor = 2.0;
        Camera.StayInLevel = true;
        
        MasterVolume = 0.5;
        
    }
    
private void LuoKentta()
    {
    string[] kentta = {
        "                                                      ****                                                                                                                                     ",
         "                                                   ########                                                                                                                                         ",
         "                                                                                                                                                                                          ",
         "                                              ###                                                                                                                                              ",
        "                                                                                                                                                                                           ",
        "                                                   ###     #     #     #     #######                                                                                                      ",
        "                                                                                         #                                                                                                  ",
        "                                                                                              ######                                                                                             ",
        "                                                                                                          ########                                                                             ",
        "                                                                                                                       #                                                                        ",
        "                                                                                                                        #        *                                                           ",
        "                                                                                                                               ###                                                        ",
        "                                                                                                                          #                                                                  ",
        "                                                                                                                        #                                                                    ",
        "                                                                                                                     #                                                                       ",
        "                                   *                                                                   *       #                                                                            ",
        "                                  ###                                                                 ####                                                                                      ",
        "                                                                                            ####                                                                                             ",
        "                                                                 ###                ###                                                                                                         ",
        "     *               *                                       ##        ###    ###                                                                                                                  ",
        "    ###            ####               ####            ###                                                                                                                                         ",
        "         #####               ####              ###                                                                                                                                            ",
        " N             ?                                          #######  *             #      #                                                                                                      ",
        "#####################################################################################                                                                                                          ",
        };

        TileMap tiles = TileMap.FromStringArray(kentta);
        tiles.SetTileMethod('#', LisaaTaso);
        tiles.SetTileMethod('*', LisaaTolkki);
        tiles.SetTileMethod('N', LisaaPelaaja);
        tiles.SetTileMethod('?', LisaaLasol);
        tiles.Execute(RUUDUN_KOKO, RUUDUN_KOKO);

        
        GameObject tausta = new GameObject(Level.Width, Level.Height);
        tausta.Image = taustaKuva;
        tausta.Position = Level.Center;    
      
        Add(tausta, -1);
    }    

    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Color = Color.Gray;
        Add(taso);
        Level.CreateBorders();
    }
    
    private void LisaaLasol(Vector paikka, double leveys, double korkeus)
    {
     PhysicsObject lasol = PhysicsObject.CreateStaticObject(leveys, korkeus);
     lasol.IgnoresCollisionResponse = true;
     lasol.Position = paikka;   
     lasol.Image = lasolKuva;
     lasol.Tag = "lasol";
     Add(lasol);
    }
    
    private void LisaaTolkki(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tolkki = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tolkki.IgnoresCollisionResponse = true;
        tolkki.Position = paikka;
        tolkki.Image = tolkkiKuva;
        tolkki.Tag = "tolkki";
        Add(tolkki);
    }
     
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        pelaaja1 = new PlatformCharacter(leveys, korkeus);
        pelaaja1.Position = paikka;
        pelaaja1.Mass = 4.0;
        pelaaja1.Image = pelaajanKuva;
        AddCollisionHandler(pelaaja1, "tolkki", TormaaTolkkiin);
        AddCollisionHandler(pelaaja1, "lasol", TormaaLasol);
        Add(pelaaja1);
    }

    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");

        Keyboard.Listen(Key.Left, ButtonState.Down, Liikuta, "Liikku vasemmalle", pelaaja1, -NOPEUS);
        Keyboard.Listen(Key.Right, ButtonState.Down, Liikuta, "Liiku oikealle", pelaaja1, NOPEUS);
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Hyppää", pelaaja1, HYPPYNOPEUS);        
    }

    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
    }

    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }

    private void TormaaTolkkiin(PhysicsObject hahmo, PhysicsObject tolkki)
    {
        maaliAani.Play();
        MessageDisplay.Add("Keräsit tölkin!");
        tolkki.Destroy();
        
        keratytTolkit++;
        
        if (keratytTolkit >= TOLKKI_MAARA)
        {
            PeliLoppui();
        }

    }
    
    private void TormaaLasol(PhysicsObject hahmo, PhysicsObject lasol)
    {
        ///peliloppuiAani.Play();
        MessageDisplay.Add("Osuit Lasoliin! Sait alkoholimyrkytyksen!");
                   
        Label peliLoppui = new Label("Peli on loppunut.");      
        peliLoppui.Font = Font.DefaultBold;     
        peliLoppui.Position = Camera.Position;
        peliLoppui.TextScale *= 3;
            peliLoppui.HorizontalAlignment = HorizontalAlignment.Center;
            peliLoppui.VerticalAlignment = VerticalAlignment.Center;
        Add(peliLoppui);
        
       
        lasol.Destroy();
        
    }
    private void PeliLoppui()
        {
            MessageDisplay.Add("Keräsit 10 tölkkiä! Peli loppui.");
    
            
            IsPaused = true;
    
            
            Label peliLoppuViesti = new Label("Keräsit 10 tölkkiä! Peli loppui!");
            peliLoppuViesti.Font = Font.DefaultBold;
            peliLoppuViesti.Color = Color.Green;
            peliLoppuViesti.Position = Camera.Position;
            peliLoppuViesti.TextScale *= 3; 
            peliLoppuViesti.HorizontalAlignment = HorizontalAlignment.Center;
            peliLoppuViesti.VerticalAlignment = VerticalAlignment.Center;
            Add(peliLoppuViesti);
        }
}


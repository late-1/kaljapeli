using System;
using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;
using Jypeli.Controls;
using Jypeli.Effects;

namespace Kaljapeli; 

/// @author lauri
/// @version 18.10.2024
/// <summary>
/// Pelin tarkoituksena on kerätä tölkkejä kentältä. Kun on kerännyt 10 tölkkiä, voittaa. Kentällä on myös esteitä ja apuesineitä. 
/// </summary>
public class Kaljapeli : PhysicsGame
{
    private const double Nopeus = 240;
    private const double HyppyNopeus = 600;
    private const int RuudunLeveys = 40;
    private const int RRuudunKorkeus = 40;
    private const int TolkkiMaara = 10;
    
    private PlatformCharacter pelaaja1;
    
    private int keratytTolkit;
    private int jaljellaOlevatTolkit;
    
    private Image pelaajanKuva = LoadImage("ukko1.png");
    private Image tolkkiKuva = LoadImage("tsingtao.png");
    private Image taustaKuva = LoadImage("tausta.png");
    private Image lasolKuva = LoadImage("lasol.png");
    private Image seinaKuva = LoadImage("seina.png");
    private Image gasKuva = LoadImage("fastgas.png");
    
    private bool gasActive = true;
    private bool isBoosted;
    private SoundEffect kaljaAani = LoadSoundEffect("can-open-2.wav");
    private SoundEffect gasAani = LoadSoundEffect("lempparidj.wav");
    
    public override void Begin()
    {
        Gravity = new Vector(0, -1000);

        KokoNaytto();
        LuoKentta();
        LisaaNappaimet();

        Camera.Follow(pelaaja1);
        Camera.ZoomFactor = 5.0;
        Camera.StayInLevel = true;
        IsFullScreen = false;
        MasterVolume = 0.5;        
    }
    

    private void LuoKentta()
    {
        string[] kentta =
        {
            "                                                                                                                                                                                              ",
            "                                                    *  *  *                                                                                                                                   ",
            "                                                   #########                                                                                                                                  ",
            "                                                                                                                                                                                              ",
            "                                              ###                                                                                                                                             ",
            "                                                                 ?                                                                                                                            ",
            "                                                   ###     #          #     #     #######                                                                                                     ",
            "                                                                                                   #      *                                                                                   ",
            "                                                                                                        ######                                                                                ",
            "                                                                                                                    ########                                                                  ",
            "                                                                                                                              #                                                               ",
            "                                                                                                                                  #       *                                                   ",
            "                                                                                                                                         ###                                                  ",
            "                                                                                                                                    #                                                         ",
            "                                                                                                                               ?  #                                                           ",
            "                                                                                                                               #                                                              ",
            "                                             *                                                                      *      #                                                                  ",
            "                                            ###             ?                                                    ####                                                                         ",
            "                                                          ######             !                         ####                                                                                   ",
            "                                                                            ###                ###                                                                                            ",
            "                                                                                                                                                                                              ",
            "          *                    *                                       ##        ###  ?  ###                                                                                                  ",
            "         ###                  ###               ####            ###                                                                                                                           ",
            "                   #####               ####              ###                                                                                                                                  ",
            " N      !                  ?                                                                                                                                                                   ",
            "##############################################################################################################################################################################################",
        };

        TileMap tiles = TileMap.FromStringArray(kentta);
        tiles.SetTileMethod('#', LisaaTaso);     
        tiles.SetTileMethod('*', LisaaTolkki);
        tiles.SetTileMethod('N', LisaaPelaaja);
        tiles.SetTileMethod('?', LisaaLasol);
        tiles.SetTileMethod('!', LisaaFastgas);
        tiles.Execute(RuudunLeveys, RRuudunKorkeus);

        GameObject tausta = new GameObject(Level.Width, Level.Height);
        tausta.Image = taustaKuva;
        tausta.Position = Level.Center;

        Add(tausta, -1);
    }

    
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Image = seinaKuva;
        taso.Tag = "taso";

        Level.CreateBorders();

        Add(taso);
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


    private void LisaaFastgas(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject gas = PhysicsObject.CreateStaticObject(leveys, korkeus);
        gas.IgnoresCollisionResponse = true;
        gas.Position = paikka;
        gas.Image = gasKuva;
        gas.Tag = "gas";
        Add(gas);
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
        AddCollisionHandler(pelaaja1, "gas", TormaaGas);
        
        Add(pelaaja1);
    }


    private void LisaaNappaimet()
    {
        // Normaalit liikkumiskäskyt
        Keyboard.Listen(Key.Left, ButtonState.Down, () => Liikuta(pelaaja1, isBoosted ? -(Nopeus + 400) : -Nopeus), "Liiku vasemmalle");
        Keyboard.Listen(Key.Right, ButtonState.Down, () => Liikuta(pelaaja1, isBoosted ? Nopeus + 400 : Nopeus), "Liiku oikealle");
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Hyppää", pelaaja1, HyppyNopeus);

        // Muut toiminnot
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.F, ButtonState.Pressed, KokoNaytto, "Aseta peli kokonäytön tilaan");
    
        Keyboard.Listen(Key.T, ButtonState.Pressed, () =>
        {
            int jaljella = NaytaTolkit(); // Päivittää arvon ja palauttaa sen
            MessageDisplay.Add($"Jäljellä olevat tölkit: {jaljella}");
        }, "Näytä jäljellä olevat tölkit");
    }

    
    private void KokoNaytto()
    {
        // Vaihtaa näytön kokonäytön tilaan ja takaisin
        IsFullScreen = !IsFullScreen;
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
        kaljaAani.Play();
        MessageDisplay.Add("Keräsit tölkin!");
        tolkki.Destroy();

        keratytTolkit++;
        if (keratytTolkit >= TolkkiMaara)
        {
            VoititPelin();
        }
    }
    

    private void TormaaLasol(PhysicsObject hahmo, PhysicsObject lasol)
    {
        Font roboto = LoadFont("RobotoMono-Bold.otf");
        roboto.Size = 50;
        
        var rajahdys = new Explosion(800);
        rajahdys.Position = lasol.Position;
        rajahdys.UseShockWave = true;
        Add(rajahdys);
        
        hahmo.IsVisible = false;
        Remove(hahmo);
        
        Label peliLoppui = new Label("Osuit Lasoliin! Sait alkoholimyrkytyksen!");
        peliLoppui.Font = roboto;
        peliLoppui.TextScale *= 2;
        peliLoppui.HorizontalAlignment = HorizontalAlignment.Center;
        peliLoppui.VerticalAlignment = VerticalAlignment.Center;
        peliLoppui.TextColor = Color.Red;

        new Vector(Screen.Width / 2 - peliLoppui.Width / 2, Screen.Height / 2 - peliLoppui.Height / 2);

        Add(peliLoppui);

        lasol.Destroy();
    }


    private void TormaaGas(PhysicsObject hahmo, PhysicsObject gas)
    {
        const double nopeudenLisays = 400;
        const double kestoSekunteina = 5.0;
        gasAani.Play();

        // Aktivoidaan nopeutus ja kamera-zoomi
        if (gasActive) 
        {
            Camera.ZoomFactor = 3.0;
        }
    
        isBoosted = true;  // Asetetaan nopeusbuusti aktiiviseksi

        // Ajastimen avulla nopeutus palautetaan normaaliksi tietyn ajan jälkeen
        Timer.SingleShot(kestoSekunteina, delegate
        {
            Camera.ZoomFactor = 5.0; // Palautetaan kamera alkuperäiseen tilaan
            isBoosted = false; // Poistetaan nopeuden lisäys
        });

        gas.Destroy();
    }

    private int NaytaTolkit() 
    {
        int jaljellaOlevatTolkit = TolkkiMaara - keratytTolkit;
        return jaljellaOlevatTolkit; // Palauttaa jäljellä olevien tölkkien määrän 
    }
    
    
    private void VoititPelin()
    {
        Font roboto = LoadFont("RobotoMono-Bold.otf");
        roboto.Size = 50;

        Label voititPelin = new Label("Keräsit 10 tölkkiä! Voitit!");
        voititPelin.Font = roboto;
        voititPelin.TextScale *= 2;
        voititPelin.HorizontalAlignment = HorizontalAlignment.Center;
        voititPelin.VerticalAlignment = VerticalAlignment.Center;
        voititPelin.TextColor = Color.Green;

        new Vector(Screen.Width / 2 - voititPelin.Width / 2, Screen.Height / 2 - voititPelin.Height / 2);

        Add(voititPelin);

        IsPaused = true;
    }
}
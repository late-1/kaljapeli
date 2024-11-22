using System.Collections.Generic;
using Jypeli;
using Jypeli.Assets;

namespace Kaljapeli; 

/// @author tuomiluu
/// @version 22.11.2024
/// <summary>
/// Pelin tarkoituksena on kerätä tölkkejä kentältä. Kun on kerännyt 10 tölkkiä, voittaa. Kentällä on myös esteitä ja apuesineitä. 
/// </summary>
public class Kaljapeli : PhysicsGame
{
    private const double Nopeus = 240;
    private const double HyppyNopeus = 600;
    private const int RuudunLeveys = 40;
    private const int RuudunKorkeus = 40;
    private const int TolkkiMaara = 10;
    
    private PlatformCharacter _pelaaja;
    
    private int _keratytTolkit;
    
    private Image _pelaajanKuva = LoadImage("ukko1.png");
    private Image _tolkkiKuva = LoadImage("tsingtao.png");
    private Image _taustaKuva = LoadImage("tausta.png");
    private Image _lasolKuva = LoadImage("lasol.png");
    private Image _seinaKuva = LoadImage("seina.png");
    private Image _gasKuva = LoadImage("fastgas.png");
    
    private bool _gasActive = true;
    private bool _isBoosted;
    
    private SoundEffect _kaljaAani = LoadSoundEffect("can-open-2.wav");
    private SoundEffect _gasAani = LoadSoundEffect("lempparidj.wav");
    
    
    /// <summary>
    /// Aloitusmetodi, joka asettaa painovoiman, luo pelikentän, asettaa kameran ja lisää ohjaimet.
    /// </summary>
    public override void Begin()
    {
        Gravity = new Vector(0, -1000);

        KokoNaytto();
        LuoKentta();
        LisaaNappaimet();

        Camera.Follow(_pelaaja);
        Camera.ZoomFactor = 5.0;
        Camera.StayInLevel = true;
        IsFullScreen = false;
        MasterVolume = 0.5;        
    }
    
    
    /// <summary>
    /// Luo pelikentän, joka on määritelty StringArrayssa. 
    /// </summary>
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
            " N      !                 ?                                                                                                                                                                   ",
            "##############################################################################################################################################################################################",
        };

        TileMap tiles = TileMap.FromStringArray(kentta);
        tiles.SetTileMethod('#', LisaaTaso);     
        tiles.SetTileMethod('*', LisaaTolkki);
        tiles.SetTileMethod('N', LisaaPelaaja);
        tiles.SetTileMethod('?', LisaaLasol);
        tiles.SetTileMethod('!', LisaaFastgas);
        tiles.Execute(RuudunLeveys, RuudunKorkeus);

        GameObject tausta = new GameObject(Level.Width, Level.Height);
        tausta.Image = _taustaKuva;
        tausta.Position = Level.Center;

        Add(tausta, -1);
        
        Level.CreateBorders();
    }


    /// <summary>
    /// Lisää törmaäyskäsittelijät pelaajalle
    /// </summary>
    /// <param name="pelaaja">pelaaja, jolle käsittelijät lisätään</param>
    private void LisaaTormays(PlatformCharacter pelaaja)
    {
        var collisionHandlers = new List<(string tag, CollisionHandler<PhysicsObject, PhysicsObject> handler)>
        {
            ("tolkki", TormaaTolkkiin),
            ("lasol", TormaaLasol),
            ("gas", TormaaGas)
        };

        foreach (var handler in collisionHandlers)
        {
            AddCollisionHandler(pelaaja, handler.tag, handler.handler);
        }
    }
    
    
    /// <summary>
    /// Lisää pelikentälle tasoja, joiden sijainti on annettu TileMapissa
    /// </summary>
    /// <param name="paikka">Kentälle luodun tason sijainti</param>
    /// <param name="leveys">Tason leveys</param>
    /// <param name="korkeus">Tason korkeus</param>
    private void LisaaTaso(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject taso = PhysicsObject.CreateStaticObject(leveys, korkeus);
        taso.Position = paikka;
        taso.Image = _seinaKuva;
        taso.Tag = "taso";
        
        Add(taso);
    }

    
    /// <summary>
    /// Lisää kentälle lasol -objektin, johon osuttaessa peli loppuu.
    /// </summary>
    /// <param name="paikka">Objektin paikka</param>
    /// <param name="leveys">Objektin leveys</param>
    /// <param name="korkeus">Objektin korkeus</param>
    private void LisaaLasol(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject lasol = PhysicsObject.CreateStaticObject(leveys, korkeus);
        lasol.IgnoresCollisionResponse = true;
        lasol.Position = paikka;
        lasol.Image = _lasolKuva;
        lasol.Tag = "lasol";
        Add(lasol); 
    }


    /// <summary>
    /// Lisää kentälle fastgas -objektin
    /// </summary>
    /// <param name="paikka"></param>
    /// <param name="leveys"></param>
    /// <param name="korkeus"></param>
    private void LisaaFastgas(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject gas = PhysicsObject.CreateStaticObject(leveys, korkeus);
        gas.IgnoresCollisionResponse = true;
        gas.Position = paikka;
        gas.Image = _gasKuva;
        gas.Tag = "gas";
        Add(gas);
    }

    /// <summary>
    /// lisää peliin tölkit, mitä on möärö kerätä
    /// </summary>
    /// <param name="paikka">tölkin paikka</param>
    /// <param name="leveys">tölkin lelveys</param>
    /// <param name="korkeus">tölkin korkeus</param>
    private void LisaaTolkki(Vector paikka, double leveys, double korkeus)
    {
        PhysicsObject tolkki = PhysicsObject.CreateStaticObject(leveys, korkeus);
        tolkki.IgnoresCollisionResponse = true;
        tolkki.Position = paikka;
        tolkki.Image = _tolkkiKuva;
        tolkki.Tag = "tolkki";
        Add(tolkki);
    }

    
    /// <summary>
    /// lisöä pelaajan kentälle
    /// </summary>
    /// <param name="paikka">pelaajan paikka</param>
    /// <param name="leveys">pelaajan leveys</param>
    /// <param name="korkeus">pelaajan korkeus</param>
    private void LisaaPelaaja(Vector paikka, double leveys, double korkeus)
    {
        _pelaaja = new PlatformCharacter(leveys, korkeus);
        _pelaaja.Position = paikka;
        _pelaaja.Mass = 4.0;
        _pelaaja.Image = _pelaajanKuva;
        
        LisaaTormays(_pelaaja);
        
        Add(_pelaaja);
    }

    
    /// <summary>
    /// Lisää peliin ohjaimet
    /// </summary>
    private void LisaaNappaimet()
    {
        Keyboard.Listen(Key.Left, ButtonState.Down, () => Liikuta(_pelaaja, _isBoosted ? -(Nopeus + 400) : -Nopeus), "Liiku vasemmalle");
        Keyboard.Listen(Key.Right, ButtonState.Down, () => Liikuta(_pelaaja, _isBoosted ? Nopeus + 400 : Nopeus), "Liiku oikealle");
        Keyboard.Listen(Key.Up, ButtonState.Pressed, Hyppaa, "Hyppää", _pelaaja, HyppyNopeus);
        
        Keyboard.Listen(Key.F1, ButtonState.Pressed, ShowControlHelp, "Näytä ohjeet");
        Keyboard.Listen(Key.Escape, ButtonState.Pressed, ConfirmExit, "Lopeta peli");
        Keyboard.Listen(Key.F, ButtonState.Pressed, KokoNaytto, "Aseta peli kokonäytön tilaan");
    
        Keyboard.Listen(Key.T, ButtonState.Pressed, () =>
        {
            int jaljella = NaytaTolkit();
            MessageDisplay.Add($"Jäljellä olevat tölkit: {jaljella}");
        }, "Näytä jäljellä olevat tölkit");
    }

    
    /// <summary>
    /// Toggle kokonäytön tilan päälle ja pois 
    /// </summary>
    private void KokoNaytto()
    {
        IsFullScreen = !IsFullScreen;
    }

    
    /// <summary>
    /// Lisää pelaajalle liikkumisen
    /// </summary>
    /// <param name="hahmo">Liikutettava hahmo</param>
    /// <param name="nopeus">Liikutettavan hahmon nopeus</param>
    private void Liikuta(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Walk(nopeus);
    }


    /// <summary>
    /// Lisää pelaajalle hypyn
    /// </summary>
    /// <param name="hahmo">hahmo jolla hypätään</param>
    /// <param name="nopeus">hypyn nopeus</param>
    private void Hyppaa(PlatformCharacter hahmo, double nopeus)
    {
        hahmo.Jump(nopeus);
    }


    /// <summary>
    /// Lisää tölkki -objektille toiminnan 
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="tolkki"></param>
    private void TormaaTolkkiin(PhysicsObject hahmo, PhysicsObject tolkki)
    {
        _kaljaAani.Play();
        MessageDisplay.Add("Keräsit tölkin!");
        tolkki.Destroy();

        _keratytTolkit++;
        if (_keratytTolkit >= TolkkiMaara)
        {
            VoititPelin();
        }
    }
    

    /// <summary>
    /// Käsittelee pelaajan törmäyksen lasol -objektin kanssa
    /// luo räjähdyksen ja poistaa pelaajan jonka jälkeen näyttää  pelin loppuviestin
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="lasol"></param>
    private void TormaaLasol(PhysicsObject hahmo, PhysicsObject lasol)
    {
        var rajahdys = new Explosion(800);
        rajahdys.Position = lasol.Position;
        rajahdys.UseShockWave = true;
        Add(rajahdys);
        
        hahmo.IsVisible = false;
        Remove(hahmo);
        
        Label peliLoppui = LuoLabel("Osuit Lasoliin! Sait alkoholimyrkytyksen!", Color.Red);
        
        Add(peliLoppui);

        lasol.Destroy();
    }

    
    /// <summary>
    /// Luo textlabelin, jota  voidaan kutsua näyttämään tekstiä
    /// </summary>
    /// <param name="text">Näytettävä teksti</param>
    /// <param name="color">tekstin väri</param>
    /// <returns>Palauttaa ja näyttää luodun labelin</returns>
    private Label LuoLabel(string text, Color color)
    {
        Font roboto = LoadFont("RobotoMono-Bold.ttf");
        roboto.Size = 50;
        Label label = new Label(text); 
        label.Font = roboto;
        label.TextScale *= 2;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TextColor = color;
        
        return label;   
    }
    
    
    /// <summary>
    /// Käsittelee pelaajan törmäyksen fastgas -objektiin
    /// Antaa pelaajalle nopeuden lisäyksen, ja kameran zoomauksen ulospäin määritetyksi ajaksi. 
    /// </summary>
    /// <param name="hahmo"></param>
    /// <param name="gas"></param>
    private void TormaaGas(PhysicsObject hahmo, PhysicsObject gas)
    {
        const double kestoSekunteina = 5.0;
        _gasAani.Play();
       
        if (_gasActive) 
        {
            Camera.ZoomFactor = 3.0;
        }
    
        _isBoosted = true;  
        
        Timer.SingleShot(kestoSekunteina, delegate
        {
            Camera.ZoomFactor = 5.0;
            _isBoosted = false;
        });

        gas.Destroy();
    }


    /// <summary>
    /// Ohjelma laskee jäljellä olevat tölkit
    /// </summary>
    /// <returns>palauttaa jäljellä olevien tölkkien määrän</returns>
    private int NaytaTolkit() 
    {
        int jaljellaOlevatTolkit = TolkkiMaara - _keratytTolkit;
        return jaljellaOlevatTolkit; 
    }
    
    
    /// <summary>
    /// Peli loppuu 
    /// </summary>
    private void VoititPelin()
    {
        Label voititPelin = LuoLabel("Keräsit 10 tölkkiä! Voitit!", Color.Green);
        
        Add(voititPelin);

        IsPaused = true;
    }
}
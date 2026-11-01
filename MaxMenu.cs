// TeoParkour - Mod menu per GTA V MODALITÀ STORIA (offline)
// Requisiti: ScriptHookV + ScriptHookVDotNet (Enhanced per GTA V Enhanced). Nessun'altra libreria.
// Tasti: F7 = menu, F8 = telecamera libera

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using GTA;
using GTA.Math;
using GTA.Native;

public class MaxMenu : Script
{
    readonly ObjectPool pool = new ObjectPool();
    readonly NativeMenu main = new NativeMenu("TeoParkour", "Modalità Storia");
    readonly List<Entity> spawned = new List<Entity>();
    readonly Random rnd = new Random();

    // Giocatore
    NativeCheckboxItem godMode, neverWanted, infAmmo, superJump, fastRun, fastSwim,
                       invisible, noRagdoll, infStamina, noclip;
    // Veicolo
    NativeCheckboxItem vehGod, spawnInside, flyCar;
    // ESP
    NativeCheckboxItem espPeds, espVehicles, espLines;
    // Mondo / video
    NativeCheckboxItem freezeTime, hideHud;

    // Telecamera libera
    Camera freeCam;
    float camPitch, camYaw, camFov = 50f;

    Vehicle flyVeh;
    Model originalModel;
    RelationshipGroup gangA, gangB;

    // Posizioni salvate
    NativeMenu savedMenu;
    bool rebuildSaved = true;
    string SavedFile { get { return Path.Combine(BaseDirectory, "MaxMenu_posizioni.txt"); } }

    public MaxMenu()
    {
        pool.Add(main);

        gangA = World.AddRelationshipGroup("MAXMENU_GANG_A");
        gangB = World.AddRelationshipGroup("MAXMENU_GANG_B");
        gangA.SetRelationshipBetweenGroups(gangB, Relationship.Hate, true);

        BuildPlayerMenu();
        BuildSkinMenu();
        BuildMoneyMenu();
        BuildTeleportMenu();
        BuildVehicleMenu();
        BuildSpawnMenu();
        BuildScenesMenu();
        BuildWeaponsMenu();
        BuildWorldMenu();
        BuildVideoMenu();
        BuildEspMenu();
        BuildLookMenu();

        Tick += OnTick;
        KeyDown += OnKeyDown;
        Aborted += (s, e) => Cleanup();
    }

    NativeMenu Sub(NativeMenu parent, string title, string subtitle)
    {
        var m = new NativeMenu("TeoParkour", subtitle);
        pool.Add(m);
        parent.AddSubMenu(m).Title = title;
        return m;
    }

    static void Notify(string msg) { GTA.UI.Notification.Show("~g~TeoParkour~s~: " + msg); }

    static bool IsOnline() { return Function.Call<bool>(Hash.NETWORK_IS_SESSION_STARTED); }

    // ================================================================ GIOCATORE
    void BuildPlayerMenu()
    {
        var m = Sub(main, "Giocatore", "Opzioni giocatore");
        m.Add(godMode = new NativeCheckboxItem("Invincibilità", false));
        m.Add(neverWanted = new NativeCheckboxItem("Mai ricercato", false));
        m.Add(infAmmo = new NativeCheckboxItem("Munizioni infinite", false));
        m.Add(superJump = new NativeCheckboxItem("Super salto", false));
        m.Add(fastRun = new NativeCheckboxItem("Corsa veloce", false));
        m.Add(fastSwim = new NativeCheckboxItem("Nuoto veloce", false));
        m.Add(invisible = new NativeCheckboxItem("Invisibile", false));
        m.Add(noRagdoll = new NativeCheckboxItem("Niente ragdoll", false));
        m.Add(infStamina = new NativeCheckboxItem("Stamina infinita", false));
        m.Add(noclip = new NativeCheckboxItem("Noclip (attraversa i muri)", false));
        noclip.CheckboxChanged += (s, e) => { if (!noclip.Checked) ReleasePed(); };

        var heal = new NativeItem("Cura e armatura al massimo");
        heal.Activated += (s, e) =>
        {
            var p = Game.Player.Character;
            p.Health = p.MaxHealth;
            p.Armor = 100;
            Notify("Curato");
        };
        m.Add(heal);

        var wanted = new NativeListItem<int>("Imposta livello ricercato", 0, 1, 2, 3, 4, 5);
        wanted.Activated += (s, e) =>
        {
            if (wanted.SelectedItem > 0) neverWanted.Checked = false;
            Game.Player.WantedLevel = wanted.SelectedItem;
        };
        m.Add(wanted);
    }

    void ReleasePed()
    {
        var p = Game.Player.Character;
        if (freeCam == null) p.IsPositionFrozen = false;
        Function.Call(Hash.SET_ENTITY_COLLISION, p, true, true);
    }

    // ================================================================ SKIN
    void BuildSkinMenu()
    {
        var m = Sub(main, "Cambia aspetto", "Skin del personaggio");
        var skins = new NativeListItem<string>("Skin",
            "player_zero", "player_one", "player_two",
            "a_m_y_hipster_01", "a_f_y_hipster_01", "a_m_y_beach_01", "a_f_y_beach_01",
            "s_m_y_cop_01", "s_m_y_swat_01", "s_m_y_fireman_01", "s_m_m_paramedic_01",
            "s_m_y_clown_01", "s_m_m_movalien_01", "u_m_y_zombie_01");
        skins.Activated += (s, e) => ChangeSkin(skins.SelectedItem);
        m.Add(skins);

        var byName = new NativeItem("Skin per nome...");
        byName.Activated += (s, e) => { var n = GetInput(30); if (!string.IsNullOrEmpty(n)) ChangeSkin(n); };
        m.Add(byName);

        var back = new NativeItem("Torna al personaggio originale");
        back.Activated += (s, e) =>
        {
            if (originalModel == null) { Notify("Non hai cambiato skin"); return; }
            if (originalModel.Request(2000)) Game.Player.ChangeModel(originalModel);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character);
            originalModel = null;
        };
        m.Add(back);

        m.Add(new NativeItem("Nota", "Torna al personaggio originale prima di salvare o fare missioni."));
    }

    void ChangeSkin(string name)
    {
        var model = LoadModel(name);
        if (model == null || !model.IsPed) return;
        if (originalModel == null) originalModel = Game.Player.Character.Model;
        Game.Player.ChangeModel(model);
        Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character);
        model.MarkAsNoLongerNeeded();
    }

    // ================================================================ SOLDI
    void BuildMoneyMenu()
    {
        var m = Sub(main, "Soldi", "Soldi del personaggio");
        var amounts = new NativeListItem<int>("Aggiungi", 100000, 1000000, 10000000, 100000000);
        amounts.Activated += (s, e) => AddMoney(amounts.SelectedItem);
        m.Add(amounts);

        var custom = new NativeItem("Importo personalizzato");
        custom.Activated += (s, e) =>
        {
            int v;
            if (int.TryParse(GetInput(10), out v)) AddMoney(v);
        };
        m.Add(custom);

        var reset = new NativeItem("Azzera soldi");
        reset.Activated += (s, e) => SetMoney(0);
        m.Add(reset);
    }

    string MoneyStat()
    {
        var model = Game.Player.Character.Model;
        if (model == new Model(PedHash.Michael)) return "SP0_TOTAL_CASH";
        if (model == new Model(PedHash.Franklin)) return "SP1_TOTAL_CASH";
        if (model == new Model(PedHash.Trevor)) return "SP2_TOTAL_CASH";
        return null;
    }

    int GetMoney(string stat)
    {
        using (var outVal = new OutputArgument())
        {
            Function.Call(Hash.STAT_GET_INT, Game.GenerateHash(stat), outVal, -1);
            return outVal.GetResult<int>();
        }
    }

    void SetMoney(long value)
    {
        string stat = MoneyStat();
        if (stat == null) { Notify("Funziona solo con Michael, Franklin o Trevor"); return; }
        value = Math.Max(0, Math.Min(int.MaxValue, value));
        Function.Call(Hash.STAT_SET_INT, Game.GenerateHash(stat), (int)value, true);
        Notify("Soldi: $" + value.ToString("N0"));
    }

    void AddMoney(int amount)
    {
        string stat = MoneyStat();
        if (stat == null) { Notify("Funziona solo con Michael, Franklin o Trevor"); return; }
        SetMoney((long)GetMoney(stat) + amount);
    }

    // ================================================================ TELEPORT
    void BuildTeleportMenu()
    {
        var m = Sub(main, "Teleport", "Teletrasporto");

        var wp = new NativeItem("Al waypoint della mappa");
        wp.Activated += (s, e) => TeleportToWaypoint();
        m.Add(wp);

        savedMenu = Sub(m, "Le mie posizioni", "Posizioni salvate");
        savedMenu.Shown += (s, e) => rebuildSaved = true;

        var places = new Dictionary<string, Vector3>
        {
            { "Casa di Michael", new Vector3(-852.4f, 160.0f, 65.6f) },
            { "Casa di Franklin", new Vector3(7.9f, 548.1f, 175.5f) },
            { "Roulotte di Trevor", new Vector3(1985.9f, 3812.2f, 32.2f) },
            { "Monte Chiliad", new Vector3(501.8f, 5604.5f, 797.9f) },
            { "Tetto Maze Bank", new Vector3(-75.0f, -818.0f, 326.2f) },
            { "Aeroporto LS", new Vector3(-1336.0f, -3044.0f, 13.9f) },
            { "Fort Zancudo", new Vector3(-2047.4f, 3132.1f, 32.8f) },
            { "Molo di Del Perro", new Vector3(-1850.1f, -1231.7f, 13.0f) },
        };
        foreach (var kv in places)
        {
            var item = new NativeItem(kv.Key);
            var pos = kv.Value;
            item.Activated += (s, e) => TeleportTo(pos);
            m.Add(item);
        }
    }

    void RebuildSavedMenu()
    {
        savedMenu.Clear();

        var save = new NativeItem("+ Salva posizione attuale");
        save.Activated += (s, e) =>
        {
            string name = GetInput(30);
            if (string.IsNullOrWhiteSpace(name)) return;
            Vector3 p = PlayerEntity().Position;
            string line = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}",
                                        name.Replace("|", " "), p.X, p.Y, p.Z);
            try { File.AppendAllText(SavedFile, line + Environment.NewLine); Notify("Salvata: " + name); }
            catch { Notify("Impossibile salvare la posizione"); }
            rebuildSaved = true;
        };
        savedMenu.Add(save);

        if (!File.Exists(SavedFile)) return;
        foreach (var raw in File.ReadAllLines(SavedFile))
        {
            var parts = raw.Split('|');
            if (parts.Length != 4) continue;
            float x, y, z;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out x)) continue;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out y)) continue;
            if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out z)) continue;
            var pos = new Vector3(x, y, z);
            var item = new NativeItem(parts[0]);
            item.Activated += (s, e) => TeleportTo(pos);
            savedMenu.Add(item);
        }
    }

    Entity PlayerEntity()
    {
        var p = Game.Player.Character;
        return p.IsInVehicle() ? (Entity)p.CurrentVehicle : p;
    }

    void TeleportTo(Vector3 pos) { PlayerEntity().Position = pos; }

    void TeleportToWaypoint()
    {
        if (!Game.IsWaypointActive) { Notify("Nessun waypoint impostato"); return; }
        Vector3 wp = World.WaypointPosition;
        Entity ent = PlayerEntity();
        for (float z = 1000f; z >= 0f; z -= 50f)
        {
            ent.Position = new Vector3(wp.X, wp.Y, z);
            Script.Wait(50);
            float ground = World.GetGroundHeight(new Vector3(wp.X, wp.Y, z));
            if (ground > 0f)
            {
                ent.Position = new Vector3(wp.X, wp.Y, ground + 1f);
                return;
            }
        }
        ent.Position = new Vector3(wp.X, wp.Y, 100f);
    }

    // ================================================================ VEICOLO
    void BuildVehicleMenu()
    {
        var m = Sub(main, "Veicolo", "Veicolo attuale");
        m.Add(vehGod = new NativeCheckboxItem("Veicolo invincibile", false));
        m.Add(flyCar = new NativeCheckboxItem("Auto volante", "Accelera/frena per volare dove guarda la telecamera.", false));
        flyCar.CheckboxChanged += (s, e) => { if (!flyCar.Checked) StopFlying(); };

        var repair = new NativeItem("Ripara");
        repair.Activated += (s, e) => { var v = CurrentVehicle(); if (v != null) v.Repair(); };
        m.Add(repair);

        var flip = new NativeItem("Raddrizza");
        flip.Activated += (s, e) => { var v = CurrentVehicle(); if (v != null) v.PlaceOnGround(); };
        m.Add(flip);

        var maxUp = new NativeItem("Potenziamenti al massimo");
        maxUp.Activated += (s, e) =>
        {
            var v = CurrentVehicle();
            if (v == null) return;
            Function.Call(Hash.SET_VEHICLE_MOD_KIT, v, 0);
            for (int type = 0; type < 50; type++)
            {
                int count = Function.Call<int>(Hash.GET_NUM_VEHICLE_MODS, v, type);
                if (count > 0) Function.Call(Hash.SET_VEHICLE_MOD, v, type, count - 1, false);
            }
            Function.Call(Hash.TOGGLE_VEHICLE_MOD, v, 18, true);
            Notify("Veicolo potenziato");
        };
        m.Add(maxUp);

        var color = new NativeItem("Colore casuale");
        color.Activated += (s, e) =>
        {
            var v = CurrentVehicle();
            if (v != null) Function.Call(Hash.SET_VEHICLE_COLOURS, v, rnd.Next(0, 160), rnd.Next(0, 160));
        };
        m.Add(color);
    }

    Vehicle CurrentVehicle()
    {
        var v = Game.Player.Character.CurrentVehicle;
        if (v == null) Notify("Non sei in un veicolo");
        return v;
    }

    void StopFlying()
    {
        if (flyVeh != null && flyVeh.Exists())
            Function.Call(Hash.SET_VEHICLE_GRAVITY, flyVeh, true);
        flyVeh = null;
    }

    // ================================================================ SPAWN
    void BuildSpawnMenu()
    {
        var m = Sub(main, "Spawn", "Veicoli, persone, oggetti");
        m.Add(spawnInside = new NativeCheckboxItem("Entra nel veicolo spawnato", true));

        var cars = new NativeListItem<string>("Veicolo",
            "adder", "zentorno", "t20", "entityxf", "kuruma", "insurgent", "sanchez", "bati",
            "police", "firetruk", "ambulance", "bus", "rhino", "buzzard2", "frogger",
            "lazer", "hydra", "besra", "jetmax", "seashark");
        cars.Activated += (s, e) => SpawnVehicle(cars.SelectedItem);
        m.Add(cars);

        var carByName = new NativeItem("Veicolo per nome...");
        carByName.Activated += (s, e) => { var n = GetInput(30); if (!string.IsNullOrEmpty(n)) SpawnVehicle(n); };
        m.Add(carByName);

        var peds = new NativeListItem<string>("Guardia del corpo",
            "s_m_y_swat_01", "s_m_m_marine_01", "s_m_y_cop_01", "s_m_m_security_01",
            "u_m_y_zombie_01", "a_c_chop");
        peds.Activated += (s, e) => SpawnBodyguard(peds.SelectedItem);
        m.Add(peds);

        var pedByName = new NativeItem("Persona per nome...");
        pedByName.Activated += (s, e) =>
        {
            var n = GetInput(30);
            if (!string.IsNullOrEmpty(n)) SpawnPedAt(n, Game.Player.Character.GetOffsetPosition(new Vector3(0f, 3f, 0f)));
        };
        m.Add(pedByName);

        var props = new NativeListItem<string>("Oggetto",
            "prop_mp_ramp_03", "prop_container_01a", "prop_dumpster_01a",
            "prop_rub_carwreck_3", "prop_roadcone02a");
        props.Activated += (s, e) => SpawnProp(props.SelectedItem);
        m.Add(props);

        var propByName = new NativeItem("Oggetto per nome...");
        propByName.Activated += (s, e) => { var n = GetInput(40); if (!string.IsNullOrEmpty(n)) SpawnProp(n); };
        m.Add(propByName);

        var clear = new NativeItem("Elimina tutto ciò che hai spawnato");
        clear.Activated += (s, e) => ClearSpawned();
        m.Add(clear);
    }

    void ClearSpawned()
    {
        foreach (var ent in spawned) if (ent != null && ent.Exists()) ent.Delete();
        spawned.Clear();
        Notify("Spawn eliminati");
    }

    Model LoadModel(string name)
    {
        var model = new Model(name);
        if (!model.IsInCdImage || !model.Request(2000))
        {
            Notify("Modello non trovato: " + name);
            return null;
        }
        return model;
    }

    void SpawnVehicle(string name)
    {
        var model = LoadModel(name);
        if (model == null || !model.IsVehicle) return;
        var p = Game.Player.Character;
        var v = World.CreateVehicle(model, p.GetOffsetPosition(new Vector3(0f, 5f, 0f)), p.Heading + 90f);
        model.MarkAsNoLongerNeeded();
        if (v == null) return;
        spawned.Add(v);
        if (spawnInside.Checked) { v.Heading = p.Heading; p.SetIntoVehicle(v, VehicleSeat.Driver); }
    }

    Ped SpawnPedAt(string name, Vector3 pos)
    {
        var model = LoadModel(name);
        if (model == null || !model.IsPed) return null;
        var ped = World.CreatePed(model, pos);
        model.MarkAsNoLongerNeeded();
        if (ped != null) spawned.Add(ped);
        return ped;
    }

    void SpawnBodyguard(string name)
    {
        var p = Game.Player.Character;
        var ped = SpawnPedAt(name, p.GetOffsetPosition(new Vector3(0f, 3f, 0f)));
        if (ped == null) return;
        int group = Function.Call<int>(Hash.GET_PED_GROUP_INDEX, p);
        Function.Call(Hash.SET_PED_AS_GROUP_MEMBER, ped, group);
        ped.RelationshipGroup = p.RelationshipGroup;
        ped.Weapons.Give(WeaponHash.CarbineRifle, 9999, true, true);
        ped.MaxHealth = 500; ped.Health = 500; ped.Armor = 100;
    }

    void SpawnProp(string name)
    {
        var model = LoadModel(name);
        if (model == null) return;
        var p = Game.Player.Character;
        var prop = World.CreateProp(model, p.GetOffsetPosition(new Vector3(0f, 6f, 0f)), true, true);
        model.MarkAsNoLongerNeeded();
        if (prop == null) return;
        prop.Heading = p.Heading;
        spawned.Add(prop);
    }

    // ================================================================ SCENE
    void BuildScenesMenu()
    {
        var m = Sub(main, "Scene", "Scene pronte per i video");

        var party = new NativeItem("Festa (gente che balla intorno a te)");
        party.Activated += (s, e) => SpawnCircle(8, 6f, "WORLD_HUMAN_PARTYING",
            "a_f_y_beach_01", "a_m_y_beach_01", "a_f_y_hipster_01", "a_m_y_hipster_01");
        m.Add(party);

        var crowd = new NativeItem("Folla che applaude");
        crowd.Activated += (s, e) => SpawnCircle(10, 8f, "WORLD_HUMAN_CHEERING",
            "a_m_y_hipster_01", "a_f_y_hipster_01", "a_m_y_beach_01", "a_f_y_beach_01");
        m.Add(crowd);

        var gang = new NativeItem("Guerra tra gang");
        gang.Activated += (s, e) => GangWar();
        m.Add(gang);

        var chase = new NativeItem("Inseguimento della polizia (3 stelle)");
        chase.Activated += (s, e) => { neverWanted.Checked = false; Game.Player.WantedLevel = 3; };
        m.Add(chase);

        var clear = new NativeItem("Rimuovi la scena");
        clear.Activated += (s, e) => ClearSpawned();
        m.Add(clear);
    }

    void SpawnCircle(int count, float radius, string scenario, params string[] models)
    {
        var p = Game.Player.Character;
        for (int i = 0; i < count; i++)
        {
            double a = 2 * Math.PI * i / count;
            var offset = new Vector3((float)Math.Cos(a) * radius, (float)Math.Sin(a) * radius, 0f);
            var ped = SpawnPedAt(models[rnd.Next(models.Length)], p.GetOffsetPosition(offset));
            if (ped == null) continue;
            ped.Heading = (float)(a * 180.0 / Math.PI) + 90f;
            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped, scenario, 0, true);
        }
    }

    void GangWar()
    {
        var p = Game.Player.Character;
        for (int i = 0; i < 5; i++)
        {
            var a = SpawnPedAt("g_m_y_ballasout_01", p.GetOffsetPosition(new Vector3(-4f + i * 2f, 15f, 0f)));
            var b = SpawnPedAt("g_m_y_famca_01", p.GetOffsetPosition(new Vector3(-4f + i * 2f, 30f, 0f)));
            if (a != null) { a.RelationshipGroup = gangA; a.Weapons.Give(WeaponHash.Pistol, 999, true, true); a.Task.FightAgainstHatedTargets(80f); }
            if (b != null) { b.RelationshipGroup = gangB; b.Weapons.Give(WeaponHash.MicroSMG, 999, true, true); b.Task.FightAgainstHatedTargets(80f); }
        }
    }

    // ================================================================ ARMI
    void BuildWeaponsMenu()
    {
        var m = Sub(main, "Armi", "Armi");
        var all = new NativeItem("Dammi tutte le armi");
        all.Activated += (s, e) =>
        {
            var p = Game.Player.Character;
            foreach (WeaponHash w in Enum.GetValues(typeof(WeaponHash)))
                if (w != WeaponHash.Unarmed) p.Weapons.Give(w, 9999, false, true);
            Notify("Tutte le armi ottenute");
        };
        m.Add(all);

        var remove = new NativeItem("Togli tutte le armi");
        remove.Activated += (s, e) => Game.Player.Character.Weapons.RemoveAll();
        m.Add(remove);
    }

    // ================================================================ MONDO
    void BuildWorldMenu()
    {
        var m = Sub(main, "Mondo", "Ora e meteo");

        var hour = new NativeListItem<int>("Ora", 0, 3, 6, 9, 12, 15, 18, 21);
        hour.Activated += (s, e) => Function.Call(Hash.SET_CLOCK_TIME, hour.SelectedItem, 0, 0);
        m.Add(hour);

        m.Add(freezeTime = new NativeCheckboxItem("Blocca orario", false));
        freezeTime.CheckboxChanged += (s, e) => Function.Call(Hash.PAUSE_CLOCK, freezeTime.Checked);

        var weather = new NativeListItem<string>("Meteo",
            "EXTRASUNNY", "CLEAR", "CLOUDS", "SMOG", "FOGGY", "OVERCAST", "RAIN",
            "THUNDER", "CLEARING", "SNOW", "BLIZZARD", "SNOWLIGHT", "XMAS", "HALLOWEEN");
        weather.Activated += (s, e) => Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, weather.SelectedItem);
        m.Add(weather);

        var clearArea = new NativeItem("Svuota la zona (persone e veicoli)");
        clearArea.Activated += (s, e) =>
        {
            var pos = Game.Player.Character.Position;
            Function.Call(Hash.CLEAR_AREA, pos.X, pos.Y, pos.Z, 150f, true, false, false, false);
        };
        m.Add(clearArea);
    }

    // ================================================================ VIDEO
    void BuildVideoMenu()
    {
        var m = Sub(main, "Video", "Strumenti per le riprese");

        var cam = new NativeItem("Telecamera libera (F8)",
            "WASD muovi, mouse guarda, Spazio su, Ctrl giù, Shift veloce. F7 per il menu.");
        cam.Activated += (s, e) => ToggleFreeCam();
        m.Add(cam);

        var fov = new NativeListItem<float>("Zoom telecamera (FOV)", 50f, 30f, 20f, 10f, 70f, 90f);
        fov.ItemChanged += (s, e) =>
        {
            camFov = fov.SelectedItem;
            if (freeCam != null) freeCam.FieldOfView = camFov;
        };
        m.Add(fov);

        var slow = new NativeListItem<float>("Velocità del tempo", 1f, 0.75f, 0.5f, 0.25f, 0.1f);
        slow.ItemChanged += (s, e) => Game.TimeScale = slow.SelectedItem;
        m.Add(slow);

        m.Add(hideHud = new NativeCheckboxItem("Nascondi HUD e minimappa", false));
    }

    void ToggleFreeCam()
    {
        var ped = Game.Player.Character;
        if (freeCam == null)
        {
            Vector3 rot = GameplayCamera.Rotation;
            camPitch = rot.X;
            camYaw = rot.Z;
            freeCam = World.CreateCamera(GameplayCamera.Position, new Vector3(camPitch, 0f, camYaw), camFov);
            World.RenderingCamera = freeCam;
            ped.IsPositionFrozen = true;
        }
        else
        {
            World.RenderingCamera = null;
            freeCam.Delete();
            freeCam = null;
            Function.Call(Hash.CLEAR_FOCUS);
            if (!noclip.Checked) ped.IsPositionFrozen = false;
        }
    }

    static float DisabledCtl(GTA.Control c)
    {
        return Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, (int)c);
    }

    static Vector3 RotToDir(float pitchDeg, float yawDeg)
    {
        double p = pitchDeg * Math.PI / 180.0, y = yawDeg * Math.PI / 180.0;
        return new Vector3((float)(-Math.Sin(y) * Math.Cos(p)), (float)(Math.Cos(y) * Math.Cos(p)), (float)Math.Sin(p));
    }

    static Vector3 RightFromYaw(float yawDeg)
    {
        double y = yawDeg * Math.PI / 180.0;
        return new Vector3((float)Math.Cos(y), (float)Math.Sin(y), 0f);
    }

    void UpdateFreeCam()
    {
        if (pool.AreAnyVisible) return; // con il menu aperto la telecamera resta ferma

        Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);

        camYaw -= DisabledCtl(GTA.Control.LookLeftRight) * 6f;
        camPitch = Math.Max(-89f, Math.Min(89f, camPitch - DisabledCtl(GTA.Control.LookUpDown) * 6f));
        freeCam.Rotation = new Vector3(camPitch, 0f, camYaw);

        float speed = (DisabledCtl(GTA.Control.Sprint) > 0.5f ? 2.0f : 0.35f) * Game.LastFrameTime * 60f;
        Vector3 pos = freeCam.Position;
        pos += RotToDir(camPitch, camYaw) * (-DisabledCtl(GTA.Control.MoveUpDown) * speed);
        pos += RightFromYaw(camYaw) * (DisabledCtl(GTA.Control.MoveLeftRight) * speed);
        if (DisabledCtl(GTA.Control.Jump) > 0.5f) pos.Z += speed;
        if (DisabledCtl(GTA.Control.Duck) > 0.5f) pos.Z -= speed;
        freeCam.Position = pos;

        Function.Call(Hash.SET_FOCUS_POS_AND_VEL, pos.X, pos.Y, pos.Z, 0f, 0f, 0f);
    }

    // ================================================================ ASPETTO MENU
    void BuildLookMenu()
    {
        var m = Sub(main, "Aspetto menu", "Banner e ritratto");

        var style = new NativeListItem<string>("Banner", "Gioco (menu M)", "Foglie", "Verde");
        style.ItemChanged += (s, e) =>
        {
            pool.BannerStyle = style.SelectedItem == "Gioco (menu M)" ? 0 : (style.SelectedItem == "Foglie" ? 1 : 2);
        };
        m.Add(style);

        var tint = new NativeCheckboxItem("Colora di verde il banner del gioco", false);
        tint.CheckboxChanged += (s, e) => pool.TintGreen = tint.Checked;
        m.Add(tint);

        var portrait = new NativeListItem<string>("Ritratto", "Franklin", "Michael", "Trevor", "Lamar", "Chop", "Nessuno");
        portrait.ItemChanged += (s, e) =>
        {
            string v = portrait.SelectedItem;
            pool.Portrait = v == "Nessuno" ? null : "char_" + v.ToLower();
        };
        m.Add(portrait);
    }

    // ================================================================ ESP
    void BuildEspMenu()
    {
        var m = Sub(main, "ESP", "Evidenzia entità vicine");
        m.Add(espPeds = new NativeCheckboxItem("Persone (vita, distanza)", false));
        m.Add(espVehicles = new NativeCheckboxItem("Veicoli", false));
        m.Add(espLines = new NativeCheckboxItem("Linee dal giocatore", false));
    }

    void DrawEsp()
    {
        var player = Game.Player.Character;
        Vector3 me = player.Position;

        if (espPeds.Checked)
        {
            foreach (var ped in World.GetNearbyPeds(player, 120f))
            {
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                Color c = ped.IsInCombat ? Color.Red : Color.White;
                float dist = Vector3.Distance(me, ped.Position);
                DrawLabel(ped.Position + new Vector3(0f, 0f, 1.1f), "Vita " + ped.Health + "  " + dist.ToString("0") + "m", c);
                if (espLines.Checked) DrawLine(me, ped.Position, c);
            }
        }

        if (espVehicles.Checked)
        {
            foreach (var veh in World.GetNearbyVehicles(player, 150f))
            {
                if (veh == null || !veh.Exists() || veh == player.CurrentVehicle) continue;
                float dist = Vector3.Distance(me, veh.Position);
                DrawLabel(veh.Position + new Vector3(0f, 0f, 1.5f), veh.LocalizedName + "  " + dist.ToString("0") + "m", Color.Cyan);
                if (espLines.Checked) DrawLine(me, veh.Position, Color.Cyan);
            }
        }
    }

    static void DrawLabel(Vector3 world, string text, Color color)
    {
        PointF screen = GTA.UI.Screen.WorldToScreen(world);
        if (screen.IsEmpty) return;
        new GTA.UI.TextElement(text, screen, 0.3f, color, GTA.UI.Font.ChaletLondon,
                               GTA.UI.Alignment.Center, true, true).Draw();
    }

    static void DrawLine(Vector3 a, Vector3 b, Color c)
    {
        Function.Call(Hash.DRAW_LINE, a.X, a.Y, a.Z, b.X, b.Y, b.Z, (int)c.R, (int)c.G, (int)c.B, 255);
    }

    // ================================================================ UTILITY
    static string GetInput(int maxLength)
    {
        Function.Call(Hash.DISPLAY_ONSCREEN_KEYBOARD, 0, "FMMC_KEY_TIP8", "", "", "", "", "", maxLength);
        int state;
        while ((state = Function.Call<int>(Hash.UPDATE_ONSCREEN_KEYBOARD)) == 0) Script.Wait(0);
        return state == 1 ? Function.Call<string>(Hash.GET_ONSCREEN_KEYBOARD_RESULT) : null;
    }

    void Cleanup()
    {
        if (freeCam != null)
        {
            World.RenderingCamera = null;
            freeCam.Delete();
            freeCam = null;
            Function.Call(Hash.CLEAR_FOCUS);
        }
        StopFlying();
        Game.TimeScale = 1f;
        var p = Game.Player.Character;
        if (p != null && p.Exists())
        {
            p.IsPositionFrozen = false;
            Function.Call(Hash.SET_ENTITY_COLLISION, p, true, true);
        }
    }

    // ================================================================ LOOP
    void OnTick(object sender, EventArgs e)
    {
        // Sicurezza: niente di tutto questo in una sessione online
        if (IsOnline())
        {
            pool.HideAll();
            Cleanup();
            return;
        }

        if (pool.BannerPath == null) pool.BannerPath = Path.Combine(BaseDirectory, "TeoParkour_banner.png");
        if (rebuildSaved) { rebuildSaved = false; RebuildSavedMenu(); }

        pool.Process();

        var player = Game.Player;
        var ped = player.Character;

        ped.IsInvincible = godMode.Checked;
        if (neverWanted.Checked) player.WantedLevel = 0;
        Function.Call(Hash.SET_PED_INFINITE_AMMO_CLIP, ped, infAmmo.Checked);
        if (superJump.Checked) player.SetSuperJumpThisFrame();
        Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, player, fastRun.Checked ? 1.49f : 1.0f);
        Function.Call(Hash.SET_SWIM_MULTIPLIER_FOR_PLAYER, player, fastSwim.Checked ? 1.49f : 1.0f);
        ped.IsVisible = !invisible.Checked;
        ped.CanRagdoll = !noRagdoll.Checked;
        if (infStamina.Checked) Function.Call(Hash.RESTORE_PLAYER_STAMINA, player, 1.0f);

        var veh = ped.CurrentVehicle;
        if (veh != null) veh.IsInvincible = vehGod.Checked;

        // Auto volante
        if (flyCar.Checked && veh != null && veh.Driver == ped)
        {
            if (flyVeh != veh) { StopFlying(); flyVeh = veh; }
            Function.Call(Hash.SET_VEHICLE_GRAVITY, veh, false);
            float input = Game.GetControlValueNormalized(GTA.Control.VehicleAccelerate)
                        - Game.GetControlValueNormalized(GTA.Control.VehicleBrake);
            Vector3 camRot = GameplayCamera.Rotation;
            veh.Rotation = new Vector3(camRot.X, 0f, camRot.Z);
            veh.Velocity = GameplayCamera.Direction * (input * 60f);
        }
        else if (flyVeh != null && (veh != flyVeh || !flyCar.Checked))
        {
            StopFlying();
        }

        // Noclip
        if (noclip.Checked && veh == null && freeCam == null)
        {
            ped.IsPositionFrozen = true;
            Function.Call(Hash.SET_ENTITY_COLLISION, ped, false, false);
            float yaw = GameplayCamera.Rotation.Z;
            float speed = (Game.GetControlValueNormalized(GTA.Control.Sprint) > 0.5f ? 2.5f : 0.6f) * Game.LastFrameTime * 60f;
            Vector3 pos = ped.Position;
            pos += GameplayCamera.Direction * (-Game.GetControlValueNormalized(GTA.Control.MoveUpDown) * speed);
            pos += RightFromYaw(yaw) * (Game.GetControlValueNormalized(GTA.Control.MoveLeftRight) * speed);
            if (Game.GetControlValueNormalized(GTA.Control.Jump) > 0.5f) pos.Z += speed;
            if (Game.GetControlValueNormalized(GTA.Control.Duck) > 0.5f) pos.Z -= speed;
            ped.PositionNoOffset = pos;
            ped.Heading = yaw;
        }

        if (freeCam != null) UpdateFreeCam();

        if (hideHud.Checked) Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);

        DrawEsp();
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (IsOnline()) return;
        if (e.KeyCode == Keys.F7)
        {
            if (pool.AreAnyVisible) pool.HideAll();
            else main.Visible = true;
        }
        else if (e.KeyCode == Keys.F8)
        {
            ToggleFreeCam();
        }
    }
}

// =====================================================================
// Mini sistema di menu integrato (nessuna libreria esterna richiesta)
// Comandi: Frecce su/giu = scorri, Frecce sx/dx = cambia valore,
//          Invio = seleziona, Backspace = indietro
// =====================================================================

public class NativeItem
{
    public string Title;
    public string Description;
    public event EventHandler Activated;

    public NativeItem(string title) : this(title, "") { }
    public NativeItem(string title, string description) { Title = title; Description = description; }

    public virtual string ValueText { get { return ""; } }
    public virtual void OnActivate(ObjectPool pool) { if (Activated != null) Activated(this, EventArgs.Empty); }
    public virtual void OnLeftRight(int dir) { }
}

public class NativeCheckboxItem : NativeItem
{
    bool check;
    public event EventHandler CheckboxChanged;

    public NativeCheckboxItem(string title, bool check) : this(title, "", check) { }
    public NativeCheckboxItem(string title, string description, bool check) : base(title, description) { this.check = check; }

    public bool Checked
    {
        get { return check; }
        set
        {
            if (check == value) return;
            check = value;
            if (CheckboxChanged != null) CheckboxChanged(this, EventArgs.Empty);
        }
    }

    public override string ValueText { get { return check ? "~g~ON" : "~r~OFF"; } }
    public override void OnActivate(ObjectPool pool) { Checked = !Checked; base.OnActivate(pool); }
    public override void OnLeftRight(int dir) { Checked = !Checked; }
}

public class NativeListItem<T> : NativeItem
{
    readonly T[] items;
    int index;
    public event EventHandler ItemChanged;

    public NativeListItem(string title, params T[] items) : base(title) { this.items = items; }

    public T SelectedItem { get { return items[index]; } }

    public override string ValueText
    {
        get { return "< " + Convert.ToString(items[index], CultureInfo.InvariantCulture) + " >"; }
    }

    public override void OnLeftRight(int dir)
    {
        if (items.Length == 0) return;
        index = (index + dir + items.Length) % items.Length;
        if (ItemChanged != null) ItemChanged(this, EventArgs.Empty);
    }
}

public class NativeSubmenuItem : NativeItem
{
    public readonly NativeMenu Menu;
    public NativeSubmenuItem(NativeMenu menu, string title) : base(title) { Menu = menu; }
    public override string ValueText { get { return ">>"; } }
    public override void OnActivate(ObjectPool pool) { pool.Push(Menu); base.OnActivate(pool); }
}

public class NativeMenu
{
    public string Title;
    public string Subtitle;
    public readonly List<NativeItem> Items = new List<NativeItem>();
    public int Selected;
    public int Scroll;
    internal ObjectPool Pool;
    public event EventHandler Shown;

    public NativeMenu(string title, string subtitle) { Title = title; Subtitle = subtitle; }

    public void Add(NativeItem item) { Items.Add(item); }

    public NativeSubmenuItem AddSubMenu(NativeMenu menu)
    {
        var item = new NativeSubmenuItem(menu, menu.Subtitle);
        Items.Add(item);
        return item;
    }

    public void Clear() { Items.Clear(); Selected = 0; Scroll = 0; }

    internal void RaiseShown() { if (Shown != null) Shown(this, EventArgs.Empty); }

    public bool Visible
    {
        get { return Pool != null && Pool.Current == this; }
        set
        {
            if (Pool == null) return;
            if (value) Pool.Open(this);
            else if (Pool.Current == this) Pool.HideAll();
        }
    }
}

public class ObjectPool
{
    readonly List<NativeMenu> menus = new List<NativeMenu>();
    readonly Stack<NativeMenu> stack = new Stack<NativeMenu>();
    readonly Dictionary<Keys, bool> prevDown = new Dictionary<Keys, bool>();
    readonly Dictionary<Keys, int> nextRepeat = new Dictionary<Keys, int>();

    const float X = 30f, Y = 40f, W = 400f, HeaderH = 80f, SubH = 30f, ItemH = 32f;
    const int MaxVisible = 12;

    // Tema TeoParkour
    static readonly Color Lime = Color.FromArgb(255, 50, 255, 50);
    static readonly Color LimeBg = Color.FromArgb(240, 50, 230, 50);
    static readonly Color Panel = Color.FromArgb(225, 0, 0, 0);
    static readonly Color FranklinGreen = Color.FromArgb(255, 30, 120, 50);

    public string BannerPath;
    GTA.UI.CustomSprite banner;
    bool bannerFailed;

    // 0 = banner del gioco (menu interazione "M"), 1 = foglie (immagine personalizzata), 2 = verde pieno
    public int BannerStyle = 0;
    public bool TintGreen = false;
    // Ritratto del personaggio (texture del gioco), null = nessuno
    public string Portrait = "char_franklin";
    readonly Dictionary<string, GTA.UI.Sprite> sprites = new Dictionary<string, GTA.UI.Sprite>();

    GTA.UI.Sprite GameSprite(string dict, string name, SizeF size, PointF pos)
    {
        string key = dict + "/" + name;
        GTA.UI.Sprite sp;
        if (!sprites.TryGetValue(key, out sp))
        {
            sp = new GTA.UI.Sprite(dict, name, size, pos);
            sprites[key] = sp;
        }
        sp.Size = size;
        sp.Position = pos;
        return sp;
    }

    public void Add(NativeMenu menu) { menu.Pool = this; menus.Add(menu); }

    public NativeMenu Current { get { return stack.Count > 0 ? stack.Peek() : null; } }
    public bool AreAnyVisible { get { return stack.Count > 0; } }

    public void Open(NativeMenu menu) { stack.Clear(); Push(menu); }

    public void Push(NativeMenu menu)
    {
        stack.Push(menu);
        menu.RaiseShown();
    }

    public void HideAll() { stack.Clear(); }

    bool Pressed(Keys key, bool repeat)
    {
        bool down = Game.IsKeyPressed(key);
        bool was;
        prevDown.TryGetValue(key, out was);
        prevDown[key] = down;
        int now = Game.GameTime;
        if (down && !was)
        {
            nextRepeat[key] = now + 400;
            return true;
        }
        if (down && repeat)
        {
            int next;
            if (nextRepeat.TryGetValue(key, out next) && now >= next)
            {
                nextRepeat[key] = now + 80;
                return true;
            }
        }
        return false;
    }

    static void Disable(int control)
    {
        Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, control, true);
    }

    public void Process()
    {
        // Aggiorna lo stato dei tasti anche a menu chiuso, così non scattano pressioni "vecchie"
        bool up = Pressed(Keys.Up, true) | Pressed(Keys.NumPad8, true);
        bool down = Pressed(Keys.Down, true) | Pressed(Keys.NumPad2, true);
        bool left = Pressed(Keys.Left, true) | Pressed(Keys.NumPad4, true);
        bool right = Pressed(Keys.Right, true) | Pressed(Keys.NumPad6, true);
        bool enter = Pressed(Keys.Return, false) | Pressed(Keys.NumPad5, false);
        bool back = Pressed(Keys.Back, false) | Pressed(Keys.NumPad0, false);

        if (stack.Count == 0) return;

        // Evita che il telefono e altri comandi si attivino mentre usi il menu
        foreach (int c in new int[] { 27, 172, 173, 174, 175, 176, 177, 201, 202, 37, 19, 85, 80 }) Disable(c);

        NativeMenu menu = stack.Peek();
        int n = menu.Items.Count;
        if (menu.Selected >= n) menu.Selected = Math.Max(0, n - 1);

        if (n > 0)
        {
            if (up) menu.Selected = (menu.Selected - 1 + n) % n;
            if (down) menu.Selected = (menu.Selected + 1) % n;
            if (left) menu.Items[menu.Selected].OnLeftRight(-1);
            if (right) menu.Items[menu.Selected].OnLeftRight(1);
            if (enter) menu.Items[menu.Selected].OnActivate(this);
        }
        if (back && stack.Count > 0 && stack.Peek() == menu) stack.Pop();

        if (stack.Count > 0) Draw(stack.Peek());
    }

    static void Rect(float x, float y, float w, float h, Color c)
    {
        new GTA.UI.ContainerElement(new PointF(x, y), new SizeF(w, h), c).Draw();
    }

    static void Text(string s, float x, float y, float scale, Color c, GTA.UI.Alignment align)
    {
        new GTA.UI.TextElement(s, new PointF(x, y), scale, c, GTA.UI.Font.Pricedown, align, false, false).Draw();
    }

    static void SmallText(string s, float x, float y, float scale, Color c)
    {
        new GTA.UI.TextElement(s, new PointF(x, y), scale, c, GTA.UI.Font.ChaletLondon, GTA.UI.Alignment.Left, false, false).Draw();
    }

    void DrawBanner()
    {
        // Sfondo verde di riserva (resta visibile se un'immagine non si carica)
        Rect(X, Y, W, HeaderH, FranklinGreen);

        if (BannerStyle == 0)
        {
            var bg = GameSprite("commonmenu", "interaction_bgd", new SizeF(W, HeaderH), new PointF(X, Y));
            bg.Color = TintGreen ? Color.FromArgb(255, 90, 220, 110) : Color.White;
            bg.Draw();
        }
        else if (BannerStyle == 1 && !bannerFailed && !string.IsNullOrEmpty(BannerPath))
        {
            try
            {
                if (banner == null)
                {
                    if (!File.Exists(BannerPath)) bannerFailed = true;
                    else banner = new GTA.UI.CustomSprite(BannerPath, new SizeF(W, HeaderH), new PointF(X, Y));
                }
                if (banner != null) banner.Draw();
            }
            catch { bannerFailed = true; }
        }

        // Ritratto del personaggio a sinistra
        if (!string.IsNullOrEmpty(Portrait))
        {
            float ps = HeaderH - 14f;
            Rect(X + 6f, Y + 6f, ps + 2f, ps + 2f, Lime);
            GameSprite(Portrait, Portrait, new SizeF(ps, ps), new PointF(X + 7f, Y + 7f)).Draw();
        }

        // Linea lime sotto il banner
        Rect(X, Y + HeaderH - 3f, W, 3f, Lime);
    }

    void Draw(NativeMenu menu)
    {
        int n = menu.Items.Count;
        if (menu.Selected < menu.Scroll) menu.Scroll = menu.Selected;
        if (menu.Selected >= menu.Scroll + MaxVisible) menu.Scroll = menu.Selected - MaxVisible + 1;
        if (menu.Scroll < 0) menu.Scroll = 0;

        // Intestazione con banner
        DrawBanner();
        float titleX = string.IsNullOrEmpty(Portrait) ? X + W / 2f : X + W / 2f + (HeaderH - 8f) / 2f;
        new GTA.UI.TextElement(menu.Title, new PointF(titleX, Y + 8f), 1.25f, Color.White,
                               GTA.UI.Font.Pricedown, GTA.UI.Alignment.Center, true, true).Draw();

        // Sottotitolo
        float y = Y + HeaderH;
        Rect(X, y, W, SubH, Panel);
        Text(menu.Subtitle.ToUpper(), X + 8f, y + 1f, 0.45f, Lime, GTA.UI.Alignment.Left);
        Text((n == 0 ? 0 : menu.Selected + 1) + " / " + n, X + W - 8f, y + 1f, 0.45f, Lime, GTA.UI.Alignment.Right);
        y += SubH;

        // Voci
        int last = Math.Min(n, menu.Scroll + MaxVisible);
        for (int i = menu.Scroll; i < last; i++)
        {
            NativeItem item = menu.Items[i];
            bool sel = i == menu.Selected;
            Rect(X, y, W, ItemH, sel ? LimeBg : Panel);
            Color tc = sel ? Color.Black : Lime;
            Text(item.Title, X + 10f, y + 1f, 0.48f, tc, GTA.UI.Alignment.Left);
            string v = item.ValueText;
            if (v.Length > 0) Text(v, X + W - 10f, y + 1f, 0.48f, tc, GTA.UI.Alignment.Right);
            y += ItemH;
        }

        // Descrizione della voce selezionata
        if (n > 0 && !string.IsNullOrEmpty(menu.Items[menu.Selected].Description))
        {
            y += 4f;
            Rect(X, y, W, 52f, Panel);
            Rect(X, y, 4f, 52f, Lime);
            var t = new GTA.UI.TextElement(menu.Items[menu.Selected].Description, new PointF(X + 10f, y + 4f), 0.32f,
                                           Lime, GTA.UI.Font.ChaletLondon, GTA.UI.Alignment.Left, false, false);
            t.WrapWidth = W - 16f;
            t.Draw();
            y += 52f;
        }

        // Aiuto comandi
        Rect(X, y + 4f, W, 24f, Panel);
        SmallText("Frecce: muovi/cambia   Invio: ok   Backspace: indietro   F7: chiudi",
                  X + 8f, y + 7f, 0.27f, Color.FromArgb(255, 150, 230, 150));
    }
}

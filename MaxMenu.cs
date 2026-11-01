// TeoParkour - Mod menu for GTA V STORY MODE (offline)
// Requires: ScriptHookV + ScriptHookVDotNet (the Enhanced build for GTA V Enhanced). No other libraries.
// Keys: F7 = menu, F8 = free camera

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
    readonly NativeMenu main = new NativeMenu("TeoParkour", "Story Mode");
    readonly List<Entity> spawned = new List<Entity>();
    readonly Random rnd = new Random();

    // Player
    NativeCheckboxItem godMode, neverWanted, infAmmo, superJump, fastRun, fastSwim,
                       invisible, noRagdoll, infStamina, noclip;
    // Vehicle
    NativeCheckboxItem vehGod, spawnInside, flyCar;
    // ESP
    NativeCheckboxItem espPeds, espVehicles, espLines;
    // World / video
    NativeCheckboxItem freezeTime, hideHud;

    // Free camera
    Camera freeCam;
    float camPitch, camYaw, camFov = 50f;

    Vehicle flyVeh;
    Model originalModel;
    RelationshipGroup gangA, gangB;

    // Saved positions
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

    // ================================================================ PLAYER
    void BuildPlayerMenu()
    {
        var m = Sub(main, "Player", "Player options");
        m.Add(godMode = new NativeCheckboxItem("God mode", false));
        m.Add(neverWanted = new NativeCheckboxItem("Never wanted", false));
        m.Add(infAmmo = new NativeCheckboxItem("Infinite ammo", false));
        m.Add(superJump = new NativeCheckboxItem("Super jump", false));
        m.Add(fastRun = new NativeCheckboxItem("Fast run", false));
        m.Add(fastSwim = new NativeCheckboxItem("Fast swim", false));
        m.Add(invisible = new NativeCheckboxItem("Invisible", false));
        m.Add(noRagdoll = new NativeCheckboxItem("No ragdoll", false));
        m.Add(infStamina = new NativeCheckboxItem("Infinite stamina", false));
        m.Add(noclip = new NativeCheckboxItem("Noclip (walk through walls)", false));
        noclip.CheckboxChanged += (s, e) => { if (!noclip.Checked) ReleasePed(); };

        var heal = new NativeItem("Full health and armor");
        heal.Activated += (s, e) =>
        {
            var p = Game.Player.Character;
            p.Health = p.MaxHealth;
            p.Armor = 100;
            Notify("Healed");
        };
        m.Add(heal);

        var wanted = new NativeListItem<int>("Set wanted level", 0, 1, 2, 3, 4, 5);
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
        var m = Sub(main, "Change look", "Character skin");
        var skins = new NativeListItem<string>("Skin",
            "player_zero", "player_one", "player_two",
            "a_m_y_hipster_01", "a_f_y_hipster_01", "a_m_y_beach_01", "a_f_y_beach_01",
            "s_m_y_cop_01", "s_m_y_swat_01", "s_m_y_fireman_01", "s_m_m_paramedic_01",
            "s_m_y_clown_01", "s_m_m_movalien_01", "u_m_y_zombie_01");
        skins.Activated += (s, e) => ChangeSkin(skins.SelectedItem);
        m.Add(skins);

        var byName = new NativeItem("Skin by name...");
        byName.Activated += (s, e) => { var n = GetInput(30); if (!string.IsNullOrEmpty(n)) ChangeSkin(n); };
        m.Add(byName);

        var back = new NativeItem("Back to original character");
        back.Activated += (s, e) =>
        {
            if (originalModel == null) { Notify("You haven't changed skin"); return; }
            if (originalModel.Request(2000)) Game.Player.ChangeModel(originalModel);
            Function.Call(Hash.SET_PED_DEFAULT_COMPONENT_VARIATION, Game.Player.Character);
            originalModel = null;
        };
        m.Add(back);

        m.Add(new NativeItem("Note", "Switch back to the original character before saving or playing missions."));
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

    // ================================================================ MONEY
    void BuildMoneyMenu()
    {
        var m = Sub(main, "Money", "Character money");
        var amounts = new NativeListItem<int>("Add", 100000, 1000000, 10000000, 100000000);
        amounts.Activated += (s, e) => AddMoney(amounts.SelectedItem);
        m.Add(amounts);

        var custom = new NativeItem("Custom amount");
        custom.Activated += (s, e) =>
        {
            int v;
            if (int.TryParse(GetInput(10), out v)) AddMoney(v);
        };
        m.Add(custom);

        var reset = new NativeItem("Reset money");
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
        if (stat == null) { Notify("Only works with Michael, Franklin or Trevor"); return; }
        value = Math.Max(0, Math.Min(int.MaxValue, value));
        Function.Call(Hash.STAT_SET_INT, Game.GenerateHash(stat), (int)value, true);
        Notify("Money: $" + value.ToString("N0"));
    }

    void AddMoney(int amount)
    {
        string stat = MoneyStat();
        if (stat == null) { Notify("Only works with Michael, Franklin or Trevor"); return; }
        SetMoney((long)GetMoney(stat) + amount);
    }

    // ================================================================ TELEPORT
    void BuildTeleportMenu()
    {
        var m = Sub(main, "Teleport", "Teleport");

        var wp = new NativeItem("To map waypoint");
        wp.Activated += (s, e) => TeleportToWaypoint();
        m.Add(wp);

        savedMenu = Sub(m, "My positions", "Saved positions");
        savedMenu.Shown += (s, e) => rebuildSaved = true;

        var places = new Dictionary<string, Vector3>
        {
            { "Michael's house", new Vector3(-852.4f, 160.0f, 65.6f) },
            { "Franklin's house", new Vector3(7.9f, 548.1f, 175.5f) },
            { "Trevor's trailer", new Vector3(1985.9f, 3812.2f, 32.2f) },
            { "Mount Chiliad", new Vector3(501.8f, 5604.5f, 797.9f) },
            { "Maze Bank roof", new Vector3(-75.0f, -818.0f, 326.2f) },
            { "LS Airport", new Vector3(-1336.0f, -3044.0f, 13.9f) },
            { "Fort Zancudo", new Vector3(-2047.4f, 3132.1f, 32.8f) },
            { "Del Perro Pier", new Vector3(-1850.1f, -1231.7f, 13.0f) },
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

        var save = new NativeItem("+ Save current position");
        save.Activated += (s, e) =>
        {
            string name = GetInput(30);
            if (string.IsNullOrWhiteSpace(name)) return;
            Vector3 p = PlayerEntity().Position;
            string line = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}",
                                        name.Replace("|", " "), p.X, p.Y, p.Z);
            try { File.AppendAllText(SavedFile, line + Environment.NewLine); Notify("Saved: " + name); }
            catch { Notify("Could not save the position"); }
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
        if (!Game.IsWaypointActive) { Notify("No waypoint set"); return; }
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

    // ================================================================ VEHICLE
    void BuildVehicleMenu()
    {
        var m = Sub(main, "Vehicle", "Current vehicle");
        m.Add(vehGod = new NativeCheckboxItem("Invincible vehicle", false));
        m.Add(flyCar = new NativeCheckboxItem("Flying car", "Accelerate/brake to fly where the camera is looking.", false));
        flyCar.CheckboxChanged += (s, e) => { if (!flyCar.Checked) StopFlying(); };

        var repair = new NativeItem("Repair");
        repair.Activated += (s, e) => { var v = CurrentVehicle(); if (v != null) v.Repair(); };
        m.Add(repair);

        var flip = new NativeItem("Flip upright");
        flip.Activated += (s, e) => { var v = CurrentVehicle(); if (v != null) v.PlaceOnGround(); };
        m.Add(flip);

        var maxUp = new NativeItem("Max upgrades");
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
            Notify("Vehicle upgraded");
        };
        m.Add(maxUp);

        var color = new NativeItem("Random color");
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
        if (v == null) Notify("You are not in a vehicle");
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
        var m = Sub(main, "Spawn", "Vehicles, people, objects");
        m.Add(spawnInside = new NativeCheckboxItem("Get into spawned vehicle", true));

        var cars = new NativeListItem<string>("Vehicle",
            "adder", "zentorno", "t20", "entityxf", "kuruma", "insurgent", "sanchez", "bati",
            "police", "firetruk", "ambulance", "bus", "rhino", "buzzard2", "frogger",
            "lazer", "hydra", "besra", "jetmax", "seashark");
        cars.Activated += (s, e) => SpawnVehicle(cars.SelectedItem);
        m.Add(cars);

        var carByName = new NativeItem("Vehicle by name...");
        carByName.Activated += (s, e) => { var n = GetInput(30); if (!string.IsNullOrEmpty(n)) SpawnVehicle(n); };
        m.Add(carByName);

        var peds = new NativeListItem<string>("Bodyguard",
            "s_m_y_swat_01", "s_m_m_marine_01", "s_m_y_cop_01", "s_m_m_security_01",
            "u_m_y_zombie_01", "a_c_chop");
        peds.Activated += (s, e) => SpawnBodyguard(peds.SelectedItem);
        m.Add(peds);

        var pedByName = new NativeItem("Person by name...");
        pedByName.Activated += (s, e) =>
        {
            var n = GetInput(30);
            if (!string.IsNullOrEmpty(n)) SpawnPedAt(n, Game.Player.Character.GetOffsetPosition(new Vector3(0f, 3f, 0f)));
        };
        m.Add(pedByName);

        var props = new NativeListItem<string>("Object",
            "prop_mp_ramp_03", "prop_container_01a", "prop_dumpster_01a",
            "prop_rub_carwreck_3", "prop_roadcone02a");
        props.Activated += (s, e) => SpawnProp(props.SelectedItem);
        m.Add(props);

        var propByName = new NativeItem("Object by name...");
        propByName.Activated += (s, e) => { var n = GetInput(40); if (!string.IsNullOrEmpty(n)) SpawnProp(n); };
        m.Add(propByName);

        var clear = new NativeItem("Delete everything you spawned");
        clear.Activated += (s, e) => ClearSpawned();
        m.Add(clear);
    }

    void ClearSpawned()
    {
        foreach (var ent in spawned) if (ent != null && ent.Exists()) ent.Delete();
        spawned.Clear();
        Notify("Spawned entities deleted");
    }

    Model LoadModel(string name)
    {
        var model = new Model(name);
        if (!model.IsInCdImage || !model.Request(2000))
        {
            Notify("Model not found: " + name);
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

    // ================================================================ SCENES
    void BuildScenesMenu()
    {
        var m = Sub(main, "Scenes", "Ready-made scenes for videos");

        var party = new NativeItem("Party (people dancing around you)");
        party.Activated += (s, e) => SpawnCircle(8, 6f, "WORLD_HUMAN_PARTYING",
            "a_f_y_beach_01", "a_m_y_beach_01", "a_f_y_hipster_01", "a_m_y_hipster_01");
        m.Add(party);

        var crowd = new NativeItem("Cheering crowd");
        crowd.Activated += (s, e) => SpawnCircle(10, 8f, "WORLD_HUMAN_CHEERING",
            "a_m_y_hipster_01", "a_f_y_hipster_01", "a_m_y_beach_01", "a_f_y_beach_01");
        m.Add(crowd);

        var gang = new NativeItem("Gang war");
        gang.Activated += (s, e) => GangWar();
        m.Add(gang);

        var chase = new NativeItem("Police chase (3 stars)");
        chase.Activated += (s, e) => { neverWanted.Checked = false; Game.Player.WantedLevel = 3; };
        m.Add(chase);

        var clear = new NativeItem("Remove the scene");
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

    // ================================================================ WEAPONS
    void BuildWeaponsMenu()
    {
        var m = Sub(main, "Weapons", "Weapons");
        var all = new NativeItem("Give all weapons");
        all.Activated += (s, e) =>
        {
            var p = Game.Player.Character;
            foreach (WeaponHash w in Enum.GetValues(typeof(WeaponHash)))
                if (w != WeaponHash.Unarmed) p.Weapons.Give(w, 9999, false, true);
            Notify("All weapons given");
        };
        m.Add(all);

        var remove = new NativeItem("Remove all weapons");
        remove.Activated += (s, e) => Game.Player.Character.Weapons.RemoveAll();
        m.Add(remove);
    }

    // ================================================================ WORLD
    void BuildWorldMenu()
    {
        var m = Sub(main, "World", "Time and weather");

        var hour = new NativeListItem<int>("Hour", 0, 3, 6, 9, 12, 15, 18, 21);
        hour.Activated += (s, e) => Function.Call(Hash.SET_CLOCK_TIME, hour.SelectedItem, 0, 0);
        m.Add(hour);

        m.Add(freezeTime = new NativeCheckboxItem("Freeze time", false));
        freezeTime.CheckboxChanged += (s, e) => Function.Call(Hash.PAUSE_CLOCK, freezeTime.Checked);

        var weather = new NativeListItem<string>("Weather",
            "EXTRASUNNY", "CLEAR", "CLOUDS", "SMOG", "FOGGY", "OVERCAST", "RAIN",
            "THUNDER", "CLEARING", "SNOW", "BLIZZARD", "SNOWLIGHT", "XMAS", "HALLOWEEN");
        weather.Activated += (s, e) => Function.Call(Hash.SET_WEATHER_TYPE_NOW_PERSIST, weather.SelectedItem);
        m.Add(weather);

        var clearArea = new NativeItem("Clear the area (people and vehicles)");
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
        var m = Sub(main, "Video", "Recording tools");

        var cam = new NativeItem("Free camera (F8)",
            "WASD move, mouse look, Space up, Ctrl down, Shift fast. F7 for the menu.");
        cam.Activated += (s, e) => ToggleFreeCam();
        m.Add(cam);

        var fov = new NativeListItem<float>("Camera zoom (FOV)", 50f, 30f, 20f, 10f, 70f, 90f);
        fov.ItemChanged += (s, e) =>
        {
            camFov = fov.SelectedItem;
            if (freeCam != null) freeCam.FieldOfView = camFov;
        };
        m.Add(fov);

        var slow = new NativeListItem<float>("Time speed", 1f, 0.75f, 0.5f, 0.25f, 0.1f);
        slow.ItemChanged += (s, e) => Game.TimeScale = slow.SelectedItem;
        m.Add(slow);

        m.Add(hideHud = new NativeCheckboxItem("Hide HUD and minimap", false));
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
        if (pool.AreAnyVisible) return; // the camera stays still while the menu is open

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

    // ================================================================ MENU LOOK
    void BuildLookMenu()
    {
        var m = Sub(main, "Menu look", "Banner and portrait");

        var style = new NativeListItem<string>("Banner", "Game (M menu)", "Leaves", "Green");
        style.ItemChanged += (s, e) =>
        {
            pool.BannerStyle = style.SelectedItem == "Game (M menu)" ? 0 : (style.SelectedItem == "Leaves" ? 1 : 2);
        };
        m.Add(style);

        var tint = new NativeCheckboxItem("Tint the game banner green", false);
        tint.CheckboxChanged += (s, e) => pool.TintGreen = tint.Checked;
        m.Add(tint);

        var portrait = new NativeListItem<string>("Portrait", "Franklin", "Michael", "Trevor", "Lamar", "Chop", "None");
        portrait.ItemChanged += (s, e) =>
        {
            string v = portrait.SelectedItem;
            pool.Portrait = v == "None" ? null : "char_" + v.ToLower();
        };
        m.Add(portrait);
    }

    // ================================================================ ESP
    void BuildEspMenu()
    {
        var m = Sub(main, "ESP", "Highlight nearby entities");
        m.Add(espPeds = new NativeCheckboxItem("People (health, distance)", false));
        m.Add(espVehicles = new NativeCheckboxItem("Vehicles", false));
        m.Add(espLines = new NativeCheckboxItem("Lines from player", false));
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
                DrawLabel(ped.Position + new Vector3(0f, 0f, 1.1f), "HP " + ped.Health + "  " + dist.ToString("0") + "m", c);
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
        // Safety: none of this runs in an online session
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

        // Flying car
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
// Built-in mini menu system (no external library needed)
// Controls: Up/Down arrows = scroll, Left/Right arrows = change value,
//           Enter = select, Backspace = back
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

    // TeoParkour theme
    static readonly Color Lime = Color.FromArgb(255, 50, 255, 50);
    static readonly Color LimeBg = Color.FromArgb(240, 50, 230, 50);
    static readonly Color Panel = Color.FromArgb(225, 0, 0, 0);
    static readonly Color FranklinGreen = Color.FromArgb(255, 30, 120, 50);

    public string BannerPath;
    GTA.UI.CustomSprite banner;
    bool bannerFailed;

    // 0 = game banner (interaction menu "M"), 1 = leaves (custom image), 2 = plain green
    public int BannerStyle = 0;
    public bool TintGreen = false;
    // Character portrait (game texture), null = none
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
        // Track key state even while the menu is closed, so stale presses never fire
        bool up = Pressed(Keys.Up, true) | Pressed(Keys.NumPad8, true);
        bool down = Pressed(Keys.Down, true) | Pressed(Keys.NumPad2, true);
        bool left = Pressed(Keys.Left, true) | Pressed(Keys.NumPad4, true);
        bool right = Pressed(Keys.Right, true) | Pressed(Keys.NumPad6, true);
        bool enter = Pressed(Keys.Return, false) | Pressed(Keys.NumPad5, false);
        bool back = Pressed(Keys.Back, false) | Pressed(Keys.NumPad0, false);

        if (stack.Count == 0) return;

        // Stop the phone and other controls from firing while the menu is in use
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
        // Fallback green background (stays visible if an image fails to load)
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

        // Character portrait on the left
        if (!string.IsNullOrEmpty(Portrait))
        {
            float ps = HeaderH - 14f;
            Rect(X + 6f, Y + 6f, ps + 2f, ps + 2f, Lime);
            GameSprite(Portrait, Portrait, new SizeF(ps, ps), new PointF(X + 7f, Y + 7f)).Draw();
        }

        // Lime line under the banner
        Rect(X, Y + HeaderH - 3f, W, 3f, Lime);
    }

    void Draw(NativeMenu menu)
    {
        int n = menu.Items.Count;
        if (menu.Selected < menu.Scroll) menu.Scroll = menu.Selected;
        if (menu.Selected >= menu.Scroll + MaxVisible) menu.Scroll = menu.Selected - MaxVisible + 1;
        if (menu.Scroll < 0) menu.Scroll = 0;

        // Header with banner
        DrawBanner();
        float titleX = string.IsNullOrEmpty(Portrait) ? X + W / 2f : X + W / 2f + (HeaderH - 8f) / 2f;
        new GTA.UI.TextElement(menu.Title, new PointF(titleX, Y + 8f), 1.25f, Color.White,
                               GTA.UI.Font.Pricedown, GTA.UI.Alignment.Center, true, true).Draw();

        // Subtitle
        float y = Y + HeaderH;
        Rect(X, y, W, SubH, Panel);
        Text(menu.Subtitle.ToUpper(), X + 8f, y + 1f, 0.45f, Lime, GTA.UI.Alignment.Left);
        Text((n == 0 ? 0 : menu.Selected + 1) + " / " + n, X + W - 8f, y + 1f, 0.45f, Lime, GTA.UI.Alignment.Right);
        y += SubH;

        // Items
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

        // Description of the selected item
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

        // Controls help
        Rect(X, y + 4f, W, 24f, Panel);
        SmallText("Arrows: move/change   Enter: ok   Backspace: back   F7: close",
                  X + 8f, y + 7f, 0.27f, Color.FromArgb(255, 150, 230, 150));
    }
}

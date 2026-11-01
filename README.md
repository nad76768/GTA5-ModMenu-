# TeoParkour – GTA V Mod Menu (Story Mode)

A mod menu for **GTA V story mode (offline)**, written in C# for ScriptHookVDotNet.
Works on both **GTA V Enhanced** and **Legacy**. It switches itself off if the game enters an online session.

## Features

- **Player**: god mode, never wanted, infinite ammo, super jump, fast run/swim, invisible, no ragdoll, infinite stamina, noclip
- **Change look**: character and ped skins, back to the original character
- **Money**: add or reset money for Michael, Franklin and Trevor
- **Teleport**: to the waypoint, famous places and your own saved positions
- **Vehicle**: invincible, flying car, repair, flip upright, max upgrades, random color
- **Spawn**: vehicles, bodyguards, people and objects (also by model name)
- **Scenes**: party, cheering crowd, gang war, police chase
- **Weapons**, **World** (time, weather, clear area), **Video** (free camera, zoom, slow motion, hidden HUD)
- **ESP**: health and distance of nearby people and vehicles
- **Menu look**: game banner, leaves or plain green; portrait of Franklin, Michael, Trevor, Lamar or Chop

## Install

See [INSTALL.txt](INSTALL.txt). In short:

1. Download **ScriptHookV** from [dev-c.com](http://www.dev-c.com/gtav/scripthookv/) and **ScriptHookVDotNet** ([Enhanced](https://github.com/Chiheb-Bacha/scripthookvdotnetenhanced/releases) or [Legacy](https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases)) and put the zips in the `download` folder.
2. Close GTA V and run `Install.bat`.
3. Disable BattlEye: `-nobattleye` in the Steam launch options and untick it in the Rockstar Games Launcher.
4. In story mode, press **F7**.

ScriptHookV and ScriptHookVDotNet files are **not included** in this repository: their license does not allow redistribution.

## Files

| File | What it does |
|---|---|
| `MaxMenu.cs` | The TeoParkour menu (goes in the GTA V `scripts` folder) |
| `TeoParkour_banner.png` | Optional "leaves" banner |
| `Install.bat` / `Install-TeoParkour.ps1` | Installer with a graphical window |
| `Mod ON.bat` / `Mod OFF.bat` | Enable and disable the mods by moving the files |

## Controls

| Key | Action |
|---|---|
| F7 | Open/close the menu |
| Arrows | Scroll and change values |
| Enter / Backspace | Select / back |
| F8 | Free camera |

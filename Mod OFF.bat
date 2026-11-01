@echo off
setlocal
title TeoParkour - Mod OFF
rem GTA V folder: change it here if you move the game
set "GTA=G:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced"
set "STORE=%GTA%\_TeoParkour_OFF"
set "FILES=args.txt xinput1_4.dll dinput8.dll ScriptHookV.dll ScriptHookVDotNet.asi ScriptHookVDotNet.ini ScriptHookVDotNet2.dll ScriptHookVDotNet3.dll MinHook.x64.dll"

if not exist "%GTA%\GTA5_Enhanced.exe" if not exist "%GTA%\GTA5.exe" (
  echo GTA V not found in: "%GTA%"
  echo Open this file with Notepad and fix the "set GTA=" line.
  pause & exit /b 1
)
tasklist /FI "IMAGENAME eq GTA5_Enhanced.exe" 2>nul | find /I "GTA5_Enhanced.exe" >nul && (echo Close GTA V before continuing. & pause & exit /b 1)
tasklist /FI "IMAGENAME eq GTA5.exe" 2>nul | find /I "GTA5.exe" >nul && (echo Close GTA V before continuing. & pause & exit /b 1)

if not exist "%STORE%" mkdir "%STORE%"
set MOVED=0
for %%F in (%FILES%) do (
  if exist "%GTA%\%%F" (
    move /Y "%GTA%\%%F" "%STORE%\%%F" >nul
    echo Moved %%F
    set MOVED=1
  )
)
echo.
if "%MOVED%"=="0" (echo Mods were already disabled.) else (echo MODS DISABLED.)
echo.
echo To play GTA Online, also remember to:
echo  - remove -nobattleye from the Steam Launch Options
echo  - re-enable BattlEye in the Rockstar Games Launcher settings
echo.
pause

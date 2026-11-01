@echo off
setlocal
title TeoParkour - Mod ON
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

if not exist "%STORE%" (
  echo Mods are already enabled ^(or were never disabled with Mod OFF^).
  pause & exit /b 0
)
for %%F in (%FILES%) do (
  if exist "%STORE%\%%F" (
    move /Y "%STORE%\%%F" "%GTA%\%%F" >nul
    echo Restored %%F
  )
)
rmdir "%STORE%" 2>nul
echo.
echo MODS ENABLED. Start GTA in story mode and press F7.
echo.
echo Also remember to:
echo  - put -nobattleye back in the Steam Launch Options
echo  - disable BattlEye in the Rockstar Games Launcher settings
echo.
pause

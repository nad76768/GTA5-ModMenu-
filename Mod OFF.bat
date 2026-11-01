@echo off
setlocal
title TeoParkour - Mod OFF
rem Cartella di GTA V: cambiala qui se sposti il gioco
set "GTA=G:\SteamLibrary\steamapps\common\Grand Theft Auto V Enhanced"
set "STORE=%GTA%\_TeoParkour_OFF"
set "FILES=args.txt xinput1_4.dll dinput8.dll ScriptHookV.dll ScriptHookVDotNet.asi ScriptHookVDotNet.ini ScriptHookVDotNet2.dll ScriptHookVDotNet3.dll MinHook.x64.dll"

if not exist "%GTA%\GTA5_Enhanced.exe" if not exist "%GTA%\GTA5.exe" (
  echo Non trovo GTA V in: "%GTA%"
  echo Apri questo file con il Blocco note e correggi la riga "set GTA=".
  pause & exit /b 1
)
tasklist /FI "IMAGENAME eq GTA5_Enhanced.exe" 2>nul | find /I "GTA5_Enhanced.exe" >nul && (echo Chiudi GTA V prima di continuare. & pause & exit /b 1)
tasklist /FI "IMAGENAME eq GTA5.exe" 2>nul | find /I "GTA5.exe" >nul && (echo Chiudi GTA V prima di continuare. & pause & exit /b 1)

if not exist "%STORE%" mkdir "%STORE%"
set MOVED=0
for %%F in (%FILES%) do (
  if exist "%GTA%\%%F" (
    move /Y "%GTA%\%%F" "%STORE%\%%F" >nul
    echo Spostato %%F
    set MOVED=1
  )
)
echo.
if "%MOVED%"=="0" (echo Le mod erano gia' disattivate.) else (echo MOD DISATTIVATE.)
echo.
echo Per giocare a GTA Online ricordati anche di:
echo  - togliere -nobattleye dalle Opzioni di avvio su Steam
echo  - riattivare BattlEye nelle impostazioni del Rockstar Games Launcher
echo.
pause

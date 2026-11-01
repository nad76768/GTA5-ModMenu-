@echo off
setlocal
title TeoParkour - Mod ON
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

if not exist "%STORE%" (
  echo Le mod sono gia' attive ^(oppure non sono mai state disattivate con Mod OFF^).
  pause & exit /b 0
)
for %%F in (%FILES%) do (
  if exist "%STORE%\%%F" (
    move /Y "%STORE%\%%F" "%GTA%\%%F" >nul
    echo Rimesso %%F
  )
)
rmdir "%STORE%" 2>nul
echo.
echo MOD ATTIVATE. Avvia GTA in modalita' storia e premi F7.
echo.
echo Ricordati anche di:
echo  - rimettere -nobattleye nelle Opzioni di avvio su Steam
echo  - disattivare BattlEye nelle impostazioni del Rockstar Games Launcher
echo.
pause

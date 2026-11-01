# TeoParkour – Mod menu per GTA V (modalità storia)

Mod menu per **GTA V in modalità storia (offline)**, scritto in C# per ScriptHookVDotNet.
Funziona su **GTA V Enhanced** e **Legacy**. Si disattiva da solo se il gioco entra in una sessione online.

## Funzioni

- **Giocatore**: invincibilità, mai ricercato, munizioni infinite, super salto, corsa/nuoto veloci, invisibile, niente ragdoll, stamina infinita, noclip
- **Cambia aspetto**: skin dei personaggi e dei pedoni, ritorno al personaggio originale
- **Soldi**: aggiungi o azzera i soldi di Michael, Franklin e Trevor
- **Teleport**: al waypoint, luoghi famosi e posizioni salvate da te
- **Veicolo**: invincibile, auto volante, ripara, raddrizza, potenziamenti al massimo, colore casuale
- **Spawn**: veicoli, guardie del corpo, persone e oggetti (anche per nome)
- **Scene**: festa, folla che applaude, guerra tra gang, inseguimento della polizia
- **Armi**, **Mondo** (ora, meteo, svuota zona), **Video** (telecamera libera, zoom, slow motion, HUD nascosto)
- **ESP**: vita e distanza di persone e veicoli vicini
- **Aspetto menu**: banner del gioco, foglie o verde; ritratto di Franklin, Michael, Trevor, Lamar o Chop

## Installazione

Vedi [LEGGIMI.txt](LEGGIMI.txt). In breve:

1. Scarica **ScriptHookV** da [dev-c.com](http://www.dev-c.com/gtav/scripthookv/) e **ScriptHookVDotNet** ([Enhanced](https://github.com/Chiheb-Bacha/scripthookvdotnetenhanced/releases) o [Legacy](https://github.com/scripthookvdotnet/scripthookvdotnet-nightly/releases)) e metti gli zip nella cartella `download`.
2. Chiudi GTA V e avvia `Installa.bat`.
3. Disattiva BattlEye: `-nobattleye` nelle opzioni di avvio di Steam e spunta tolta nel Rockstar Games Launcher.
4. In modalità storia premi **F7**.

I file di ScriptHookV e ScriptHookVDotNet **non sono inclusi** nel repository: la loro licenza non ne consente la ridistribuzione.

## File

| File | Cosa fa |
|---|---|
| `MaxMenu.cs` | Il menu TeoParkour (va nella cartella `scripts` di GTA V) |
| `TeoParkour_banner.png` | Banner "foglie" opzionale |
| `Installa.bat` / `Installa-MaxMenu.ps1` | Installatore con interfaccia grafica |
| `Mod ON.bat` / `Mod OFF.bat` | Attivano e disattivano le mod spostando i file |

## Comandi

| Tasto | Azione |
|---|---|
| F7 | Apri/chiudi il menu |
| Frecce | Scorri e cambia valori |
| Invio / Backspace | Seleziona / indietro |
| F8 | Telecamera libera |

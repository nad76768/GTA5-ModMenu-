# Installatore MaxMenu per GTA V (modalità storia)
# Copia ScriptHookV, ScriptHookVDotNet e il menu TeoParkour nella cartella di GTA V.

# --- Avvio come amministratore (serve per scrivere in Program Files) ---
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Start-Process powershell -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$here = Split-Path -Parent $PSCommandPath
$manifestName = "MaxMenu_file_installati.txt"

# --- Ricerca automatica della cartella di GTA V ---
function Test-GtaFolder($path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return $false }
    return (Test-Path (Join-Path $path "GTA5.exe")) -or (Test-Path (Join-Path $path "GTA5_Enhanced.exe"))
}

function Find-GtaFolder {
    $candidates = @()
    $regKeys = @(
        @{ Key = "HKLM:\SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V"; Value = "InstallFolder" },
        @{ Key = "HKLM:\SOFTWARE\WOW6432Node\Rockstar Games\GTAV"; Value = "InstallFolderSteam" },
        @{ Key = "HKLM:\SOFTWARE\WOW6432Node\Rockstar Games\GTAV"; Value = "InstallFolderEpic" }
    )
    foreach ($r in $regKeys) {
        try {
            $v = (Get-ItemProperty -Path $r.Key -Name $r.Value -ErrorAction Stop).($r.Value)
            if ($v) { $candidates += $v; $candidates += (Split-Path $v -Parent) }
        } catch {}
    }
    $candidates += @(
        "C:\Program Files\Rockstar Games\Grand Theft Auto V",
        "C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced",
        "C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V",
        "C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V Enhanced",
        "C:\Program Files\Epic Games\GTAV",
        "C:\Program Files\Epic Games\GTAV Enhanced"
    )
    foreach ($c in $candidates) { if (Test-GtaFolder $c) { return $c } }
    return ""
}

# --- Finestra ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "Installatore TeoParkour"
$form.Size = New-Object System.Drawing.Size(620, 470)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false

$font = New-Object System.Drawing.Font("Segoe UI", 9)
$form.Font = $font

$title = New-Object System.Windows.Forms.Label
$title.Text = "TeoParkour - mod menu per la modalita' storia di GTA V"
$title.Font = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$title.Location = New-Object System.Drawing.Point(15, 12)
$title.AutoSize = $true
$form.Controls.Add($title)

function Add-PathRow($labelText, $y, $initial) {
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Text = $labelText
    $lbl.Location = New-Object System.Drawing.Point(15, $y)
    $lbl.AutoSize = $true
    $form.Controls.Add($lbl)

    $box = New-Object System.Windows.Forms.TextBox
    $box.Location = New-Object System.Drawing.Point(15, ($y + 20))
    $box.Size = New-Object System.Drawing.Size(480, 24)
    $box.Text = $initial
    $form.Controls.Add($box)

    $btn = New-Object System.Windows.Forms.Button
    $btn.Text = "Sfoglia..."
    $btn.Location = New-Object System.Drawing.Point(505, ($y + 18))
    $btn.Size = New-Object System.Drawing.Size(85, 26)
    $btn.Add_Click({
        $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
        $dlg.SelectedPath = $box.Text
        if ($dlg.ShowDialog() -eq "OK") { $box.Text = $dlg.SelectedPath }
    }.GetNewClosure())
    $form.Controls.Add($btn)
    return $box
}

$gtaBox = Add-PathRow "Cartella di GTA V (dove si trova GTA5.exe):" 50 (Find-GtaFolder)
$dlBox  = Add-PathRow "Cartella con i file scaricati (ScriptHookV e ScriptHookVDotNet):" 105 (Join-Path $here "download")

$log = New-Object System.Windows.Forms.TextBox
$log.Multiline = $true
$log.ReadOnly = $true
$log.ScrollBars = "Vertical"
$log.Location = New-Object System.Drawing.Point(15, 165)
$log.Size = New-Object System.Drawing.Size(575, 200)
$log.BackColor = [System.Drawing.Color]::White
$form.Controls.Add($log)

function Write-Log($msg) {
    $log.AppendText($msg + "`r`n")
    [System.Windows.Forms.Application]::DoEvents()
}

$installBtn = New-Object System.Windows.Forms.Button
$installBtn.Text = "Installa"
$installBtn.Location = New-Object System.Drawing.Point(15, 380)
$installBtn.Size = New-Object System.Drawing.Size(180, 36)
$installBtn.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($installBtn)

$uninstallBtn = New-Object System.Windows.Forms.Button
$uninstallBtn.Text = "Disinstalla"
$uninstallBtn.Location = New-Object System.Drawing.Point(205, 380)
$uninstallBtn.Size = New-Object System.Drawing.Size(120, 36)
$form.Controls.Add($uninstallBtn)

$closeBtn = New-Object System.Windows.Forms.Button
$closeBtn.Text = "Chiudi"
$closeBtn.Location = New-Object System.Drawing.Point(490, 380)
$closeBtn.Size = New-Object System.Drawing.Size(100, 36)
$closeBtn.Add_Click({ $form.Close() })
$form.Controls.Add($closeBtn)

function Test-GtaRunning {
    return [bool](Get-Process -Name "GTA5", "GTA5_Enhanced", "PlayGTAV" -ErrorAction SilentlyContinue)
}

# --- Rimozione di un'installazione precedente ---
function Remove-OldInstall($gta) {
    $manifest = Join-Path $gta $manifestName
    if (-not (Test-Path $manifest)) { return }
    foreach ($rel in Get-Content $manifest) {
        if ([string]::IsNullOrWhiteSpace($rel)) { continue }
        $path = Join-Path $gta $rel
        if (Test-Path $path) { Remove-Item $path -Force }
    }
    Remove-Item $manifest -Force
    Get-ChildItem -Path $gta -Filter "*.backup_maxmenu" -File | ForEach-Object {
        $orig = Join-Path $gta ($_.Name -replace '\.backup_maxmenu$', '')
        Move-Item $_.FullName $orig -Force
    }
}

# --- Installazione ---
$installBtn.Add_Click({
    $log.Clear()
    $gta = $gtaBox.Text.Trim()
    $dl  = $dlBox.Text.Trim()

    if (-not (Test-GtaFolder $gta)) { Write-Log "ERRORE: in questa cartella non trovo GTA5.exe. Scegli la cartella giusta."; return }
    if (Test-GtaRunning) { Write-Log "ERRORE: chiudi GTA V prima di installare."; return }
    if (-not (Test-Path $dl)) { Write-Log "ERRORE: la cartella dei file scaricati non esiste."; return }

    $menuFile = Join-Path $here "MaxMenu.cs"
    if (-not (Test-Path $menuFile)) { Write-Log "ERRORE: MaxMenu.cs deve stare nella stessa cartella dell'installatore."; return }

    $isEnhanced = Test-Path (Join-Path $gta "GTA5_Enhanced.exe")
    if ($isEnhanced) { Write-Log "Rilevato GTA V Enhanced." } else { Write-Log "Rilevato GTA V Legacy." }

    try {
        # Estrae gli zip in una cartella temporanea
        $temp = Join-Path $env:TEMP "MaxMenuInstall"
        if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
        New-Item -ItemType Directory -Path $temp | Out-Null

        Get-ChildItem -Path $dl -Filter *.zip -File | ForEach-Object {
            Unblock-File -Path $_.FullName
            Write-Log "Estraggo $($_.Name)..."
            Expand-Archive -Path $_.FullName -DestinationPath (Join-Path $temp $_.BaseName) -Force
        }

        function Find-All($pattern) {
            Get-ChildItem -Path @($dl, $temp) -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue
        }

        $shv   = Find-All "ScriptHookV.dll" | Select-Object -First 1

        # ScriptHookVDotNet: per la Enhanced serve la versione "Enhanced" (va bene anche per la Legacy)
        $shvdnAll = @(Find-All "ScriptHookVDotNet*.asi")
        $shvdn = $shvdnAll | Where-Object { $_.FullName -match 'Enhanced' } | Select-Object -First 1
        if (-not $shvdn -and -not $isEnhanced) { $shvdn = $shvdnAll | Select-Object -First 1 }

        $missing = @()
        if (-not $shv) { $missing += "ScriptHookV (zip da dev-c.com)" }
        if (-not $shvdn) {
            if ($isEnhanced) { $missing += "ScriptHookVDotNet ENHANCED (github.com/Chiheb-Bacha/scripthookvdotnetenhanced)" }
            else { $missing += "ScriptHookVDotNet (zip da GitHub)" }
        }
        if ($missing.Count -gt 0) {
            Write-Log "Mancano questi file nella cartella dei download:"
            $missing | ForEach-Object { Write-Log "  - $_" }
            return
        }

        # Toglie i file di un'installazione precedente
        Remove-OldInstall $gta
        Write-Log "Pulita l'installazione precedente."

        $installed = New-Object System.Collections.Generic.List[string]

        function Install-RootFile($src) {
            $dest = Join-Path $gta $src.Name
            $backup = "$dest.backup_maxmenu"
            if ((Test-Path $dest) -and -not (Test-Path $backup)) {
                if ((Get-FileHash $dest).Hash -ne (Get-FileHash $src.FullName).Hash) {
                    Copy-Item $dest $backup
                    Write-Log "Backup di $($src.Name) originale."
                }
            }
            Copy-Item $src.FullName $dest -Force
            Unblock-File $dest
            $installed.Add($src.Name)
            Write-Log "Copiato $($src.Name)"
        }

        # ScriptHookV: tutto il contenuto della cartella bin (caricatori ASI e args.txt), senza il trainer di esempio
        Get-ChildItem -Path $shv.DirectoryName -File |
            Where-Object { $_.Name -ne "NativeTrainer.asi" } |
            ForEach-Object { Install-RootFile $_ }

        # ScriptHookVDotNet: .asi, .dll e .ini della sua cartella
        Get-ChildItem -Path $shvdn.DirectoryName -File |
            Where-Object { $_.Extension -in @(".asi", ".dll", ".ini") } |
            ForEach-Object { Install-RootFile $_ }

        # Cartella scripts
        $scripts = Join-Path $gta "scripts"
        if (-not (Test-Path $scripts)) { New-Item -ItemType Directory -Path $scripts | Out-Null }

        $toScripts = @($menuFile)
        $bannerFile = Join-Path $here "TeoParkour_banner.png"
        if (Test-Path $bannerFile) { $toScripts += $bannerFile }

        foreach ($f in $toScripts) {
            $name = Split-Path $f -Leaf
            $dest = Join-Path $scripts $name
            Copy-Item $f $dest -Force
            Unblock-File $dest
            $installed.Add("scripts\$name")
            Write-Log "Copiato scripts\$name"
        }

        $installed | Set-Content -Path (Join-Path $gta $manifestName) -Encoding UTF8
        Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue

        Write-Log ""
        Write-Log "FATTO! Avvia GTA V in modalita' storia e premi F7."
        Write-Log "F8 = telecamera libera. Il menu non funziona in GTA Online."
    }
    catch {
        Write-Log "ERRORE: $($_.Exception.Message)"
    }
})

# --- Disinstallazione ---
$uninstallBtn.Add_Click({
    $log.Clear()
    $gta = $gtaBox.Text.Trim()

    if (-not (Test-Path (Join-Path $gta $manifestName))) { Write-Log "Non trovo un'installazione di MaxMenu in questa cartella."; return }
    if (Test-GtaRunning) { Write-Log "ERRORE: chiudi GTA V prima di disinstallare."; return }

    $answer = [System.Windows.Forms.MessageBox]::Show(
        "Rimuovo TeoParkour, ScriptHookV e ScriptHookVDotNet dalla cartella di GTA V?",
        "Disinstalla", "YesNo", "Question")
    if ($answer -ne "Yes") { return }

    try {
        Remove-OldInstall $gta
        Write-Log "Disinstallazione completata."
        Write-Log "Le tue posizioni salvate restano in scripts\MaxMenu_posizioni.txt"
    }
    catch {
        Write-Log "ERRORE: $($_.Exception.Message)"
    }
})

[void]$form.ShowDialog()

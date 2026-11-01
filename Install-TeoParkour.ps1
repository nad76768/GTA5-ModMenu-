# TeoParkour installer for GTA V (story mode)
# Copies ScriptHookV, ScriptHookVDotNet and the TeoParkour menu into the GTA V folder.

# --- Run as administrator (needed to write into Program Files) ---
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

# --- Automatic GTA V folder detection ---
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

# --- Window ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "TeoParkour Installer"
$form.Size = New-Object System.Drawing.Size(620, 470)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedDialog"
$form.MaximizeBox = $false

$font = New-Object System.Drawing.Font("Segoe UI", 9)
$form.Font = $font

$title = New-Object System.Windows.Forms.Label
$title.Text = "TeoParkour - mod menu for GTA V story mode"
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
    $btn.Text = "Browse..."
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

$gtaBox = Add-PathRow "GTA V folder (where GTA5.exe is):" 50 (Find-GtaFolder)
$dlBox  = Add-PathRow "Folder with the downloaded files (ScriptHookV and ScriptHookVDotNet):" 105 (Join-Path $here "download")

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
$installBtn.Text = "Install"
$installBtn.Location = New-Object System.Drawing.Point(15, 380)
$installBtn.Size = New-Object System.Drawing.Size(180, 36)
$installBtn.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$form.Controls.Add($installBtn)

$uninstallBtn = New-Object System.Windows.Forms.Button
$uninstallBtn.Text = "Uninstall"
$uninstallBtn.Location = New-Object System.Drawing.Point(205, 380)
$uninstallBtn.Size = New-Object System.Drawing.Size(120, 36)
$form.Controls.Add($uninstallBtn)

$closeBtn = New-Object System.Windows.Forms.Button
$closeBtn.Text = "Close"
$closeBtn.Location = New-Object System.Drawing.Point(490, 380)
$closeBtn.Size = New-Object System.Drawing.Size(100, 36)
$closeBtn.Add_Click({ $form.Close() })
$form.Controls.Add($closeBtn)

function Test-GtaRunning {
    return [bool](Get-Process -Name "GTA5", "GTA5_Enhanced", "PlayGTAV" -ErrorAction SilentlyContinue)
}

# --- Removal of a previous install ---
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

# --- Install ---
$installBtn.Add_Click({
    $log.Clear()
    $gta = $gtaBox.Text.Trim()
    $dl  = $dlBox.Text.Trim()

    if (-not (Test-GtaFolder $gta)) { Write-Log "ERROR: GTA5.exe was not found in this folder. Pick the right folder."; return }
    if (Test-GtaRunning) { Write-Log "ERROR: close GTA V before installing."; return }
    if (-not (Test-Path $dl)) { Write-Log "ERROR: the downloads folder does not exist."; return }

    $menuFile = Join-Path $here "MaxMenu.cs"
    if (-not (Test-Path $menuFile)) { Write-Log "ERROR: MaxMenu.cs must be in the same folder as the installer."; return }

    $isEnhanced = Test-Path (Join-Path $gta "GTA5_Enhanced.exe")
    if ($isEnhanced) { Write-Log "Detected GTA V Enhanced." } else { Write-Log "Detected GTA V Legacy." }

    try {
        # Extract the zips into a temp folder
        $temp = Join-Path $env:TEMP "MaxMenuInstall"
        if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
        New-Item -ItemType Directory -Path $temp | Out-Null

        Get-ChildItem -Path $dl -Filter *.zip -File | ForEach-Object {
            Unblock-File -Path $_.FullName
            Write-Log "Extracting $($_.Name)..."
            Expand-Archive -Path $_.FullName -DestinationPath (Join-Path $temp $_.BaseName) -Force
        }

        function Find-All($pattern) {
            Get-ChildItem -Path @($dl, $temp) -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue
        }

        $shv   = Find-All "ScriptHookV.dll" | Select-Object -First 1

        # ScriptHookVDotNet: Enhanced needs the "Enhanced" build (which also works on Legacy)
        $shvdnAll = @(Find-All "ScriptHookVDotNet*.asi")
        $shvdn = $shvdnAll | Where-Object { $_.FullName -match 'Enhanced' } | Select-Object -First 1
        if (-not $shvdn -and -not $isEnhanced) { $shvdn = $shvdnAll | Select-Object -First 1 }

        $missing = @()
        if (-not $shv) { $missing += "ScriptHookV (zip from dev-c.com)" }
        if (-not $shvdn) {
            if ($isEnhanced) { $missing += "ScriptHookVDotNet ENHANCED (github.com/Chiheb-Bacha/scripthookvdotnetenhanced)" }
            else { $missing += "ScriptHookVDotNet (zip from GitHub)" }
        }
        if ($missing.Count -gt 0) {
            Write-Log "These files are missing from the downloads folder:"
            $missing | ForEach-Object { Write-Log "  - $_" }
            return
        }

        # Remove the files of a previous install
        Remove-OldInstall $gta
        Write-Log "Previous install cleaned up."

        $installed = New-Object System.Collections.Generic.List[string]

        function Install-RootFile($src) {
            $dest = Join-Path $gta $src.Name
            $backup = "$dest.backup_maxmenu"
            if ((Test-Path $dest) -and -not (Test-Path $backup)) {
                if ((Get-FileHash $dest).Hash -ne (Get-FileHash $src.FullName).Hash) {
                    Copy-Item $dest $backup
                    Write-Log "Backed up the original $($src.Name)."
                }
            }
            Copy-Item $src.FullName $dest -Force
            Unblock-File $dest
            $installed.Add($src.Name)
            Write-Log "Copied $($src.Name)"
        }

        # ScriptHookV: everything in the bin folder (ASI loaders and args.txt), except the sample trainer
        Get-ChildItem -Path $shv.DirectoryName -File |
            Where-Object { $_.Name -ne "NativeTrainer.asi" } |
            ForEach-Object { Install-RootFile $_ }

        # ScriptHookVDotNet: the .asi, .dll and .ini files from its folder
        Get-ChildItem -Path $shvdn.DirectoryName -File |
            Where-Object { $_.Extension -in @(".asi", ".dll", ".ini") } |
            ForEach-Object { Install-RootFile $_ }

        # scripts folder
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
            Write-Log "Copied scripts\$name"
        }

        $installed | Set-Content -Path (Join-Path $gta $manifestName) -Encoding UTF8
        Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue

        Write-Log ""
        Write-Log "DONE! Start GTA V in story mode and press F7."
        Write-Log "F8 = free camera. The menu does not work in GTA Online."
    }
    catch {
        Write-Log "ERROR: $($_.Exception.Message)"
    }
})

# --- Uninstall ---
$uninstallBtn.Add_Click({
    $log.Clear()
    $gta = $gtaBox.Text.Trim()

    if (-not (Test-Path (Join-Path $gta $manifestName))) { Write-Log "No TeoParkour install found in this folder."; return }
    if (Test-GtaRunning) { Write-Log "ERROR: close GTA V before uninstalling."; return }

    $answer = [System.Windows.Forms.MessageBox]::Show(
        "Remove TeoParkour, ScriptHookV and ScriptHookVDotNet from the GTA V folder?",
        "Uninstall", "YesNo", "Question")
    if ($answer -ne "Yes") { return }

    try {
        Remove-OldInstall $gta
        Write-Log "Uninstall complete."
        Write-Log "Your saved positions stay in scripts\MaxMenu_posizioni.txt"
    }
    catch {
        Write-Log "ERROR: $($_.Exception.Message)"
    }
})

[void]$form.ShowDialog()

param(
    [string]$Project = 'X:\Jeux Vidéos\SkiStation\Nouveau dossier (71)\SkiStation_Alpine_Empires_Avant_Premiere'
)

$ErrorActionPreference = 'Stop'
$backup = $null
$attempted = [System.Collections.Generic.List[string]]::new()
$updated = $false

try {
    $manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'MANIFEST.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    $parentManifestName = 'ALPINE_EMPIRES_BLOC_2_SAUVEGARDE/MANIFEST_BLOC_02_SAUVEGARDE.json'
    if (-not (Test-Path -LiteralPath (Join-Path $Project $parentManifestName) -PathType Leaf)) {
        Add-Type -AssemblyName System.Windows.Forms
        $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
        try {
            $dialog.Description = 'Choisir le projet SkiStation_Alpine_Empires_Avant_Premiere'
            $dialog.ShowNewFolderButton = $false
            if ($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
                throw 'Aucun projet sélectionné.'
            }
            $Project = $dialog.SelectedPath
        } finally {
            $dialog.Dispose()
        }
    }

    $projectRoot = (Get-Item -LiteralPath $Project).FullName
    $parentManifest = Join-Path $projectRoot $parentManifestName
    if (-not (Test-Path -LiteralPath $parentManifest -PathType Leaf) -or
        (Get-FileHash -LiteralPath $parentManifest -Algorithm SHA256).Hash -ne $manifest.parentManifestSha256) {
        throw 'Le complément Sauvegarde matérielle précédent est absent ou sa filiation est différente.'
    }

    $unityLock = Join-Path $projectRoot 'Temp/UnityLockfile'
    if (Test-Path -LiteralPath $unityLock -PathType Leaf) {
        try {
            $probe = [IO.File]::Open($unityLock, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $probe.Dispose()
        } catch {
            throw 'Fermez le projet dans Unity avant de lancer cette mise à jour.'
        }
    }

    $marker = Join-Path $projectRoot 'ALPINE_EMPIRES_BLOC_2_COMPTES/MANIFEST_BLOC_02_COMPTES.json'
    if (Test-Path -LiteralPath $marker) {
        throw 'Le complément Comptes alliés est déjà présent.'
    }

    foreach ($dependency in $manifest.verifiedDependencies) {
        $target = Join-Path $projectRoot $dependency.path
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $dependency.sha256) {
            throw "Un fichier de la version précédente ne correspond pas au manifeste : $($dependency.path)."
        }
    }

    foreach ($file in $manifest.files) {
        $target = Join-Path $projectRoot $file.path
        $payload = Join-Path (Join-Path $PSScriptRoot 'Payload') $file.path
        if ($null -eq $file.before) {
            if (Test-Path -LiteralPath $target) { throw "Un fichier à créer existe déjà : $($file.path)." }
        } elseif ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $file.before) {
            throw "Le fichier installé ne correspond pas à la version précédente : $($file.path)."
        }
        if ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash -ne $file.after) {
            throw "Fichier de mise à jour altéré : $($file.path)."
        }
    }

    $backup = Join-Path $projectRoot ('AlpineEmpires_Backup_Comptes_' + [Guid]::NewGuid().ToString('N'))
    foreach ($file in $manifest.files) {
        $target = Join-Path $projectRoot $file.path
        if ($null -eq $file.before) { continue }
        $original = Join-Path $backup $file.path
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($original)) | Out-Null
        [IO.File]::Copy($target, $original)
        if ((Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash -ne $file.before) {
            throw "La sauvegarde de $($file.path) a échoué."
        }
    }

    foreach ($file in $manifest.files) {
        $target = Join-Path $projectRoot $file.path
        $payload = Join-Path (Join-Path $PSScriptRoot 'Payload') $file.path
        $attempted.Add($file.path)
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        [IO.File]::Copy($payload, $target, ($null -ne $file.before))
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $file.after) {
            throw "La copie n’a pas pu être vérifiée : $($file.path)."
        }
    }

    $updated = $true
} catch {
    Write-Host ('Mise à jour arrêtée. ' + $_.Exception.Message) -ForegroundColor Red
} finally {
    if (-not $updated -and $backup) {
        foreach ($path in $attempted) {
            try {
                $old = Join-Path $backup $path
                $target = Join-Path $projectRoot $path
                if (Test-Path -LiteralPath $old -PathType Leaf) {
                    [IO.File]::Copy($old, $target, $true)
                } elseif (Test-Path -LiteralPath $target -PathType Leaf) {
                    Remove-Item -LiteralPath $target
                }
            } catch {
                Write-Host "Restauration manuelle requise pour $path. Copie d’origine : $backup" -ForegroundColor Red
            }
        }
    }
}

if (-not $updated) { exit 1 }
Write-Host 'Complément Comptes alliés installé. Rouvrez ce projet dans Unity Hub :'
Write-Host $projectRoot
Write-Host 'SkiStation_Bootstrap > Play > Alpine Empires > Nouvelle partie solo > Jouer ma station en 3D'
Write-Host "Sauvegarde des scripts précédents : $backup"

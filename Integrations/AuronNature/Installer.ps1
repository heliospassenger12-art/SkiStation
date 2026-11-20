[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$Projet)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $Projet).Path
$patch = Join-Path $PSScriptRoot 'AuronNature.patch'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Manifest.json') -Raw | ConvertFrom-Json

function Get-SourceHash([string]$path) {
    $text = [IO.File]::ReadAllText($path).Replace("`r`n", "`n")
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))
}

foreach ($required in $manifest.requires) {
    $item = Join-Path $root $required.path
    if (!(Test-Path -LiteralPath $item -PathType Leaf) -or (Get-FileHash -LiteralPath $item).Hash -ne $required.sha256) {
        throw "Prérequis Auron différent ou absent : $($required.path)"
    }
}
$pending = foreach ($record in $manifest.scripts) {
    if ($record.path -notmatch '^Assets/SkiStation/Scripts/[A-Za-z0-9_./-]+\.cs$' -or $record.path.Split('/') -contains '..') {
        throw "Chemin du correctif invalide : $($record.path)"
    }
    $destination = Join-Path $root $record.path
    $current = Get-SourceHash $destination
    if ($current -eq $record.after) { continue }
    if ($current -ne $record.before) { throw "Version locale différente : $($record.path)" }
    $record
}
$dataDir = Join-Path $root 'Assets/SkiStation/Resources/TerrainGeneration/Nature'
$layers = foreach ($layer in $manifest.layers) {
    if ($layer.name -notmatch '^auron(Vegetation|Geology)\.json(\.meta)?$') { throw "Couche invalide : $($layer.name)" }
    $src = Join-Path $PSScriptRoot $layer.name
    $dst = Join-Path $dataDir $layer.name
    if ((Get-SourceHash $src) -ne $layer.sha256) { throw "Couche du kit modifiée : $($layer.name)" }
    if (Test-Path -LiteralPath $dst) {
        if ((Get-SourceHash $dst) -ne $layer.sha256) { throw "Autre couche locale présente : $($layer.name)" }
        continue
    }
    [pscustomobject]@{source=$src; destination=$dst; sha256=$layer.sha256}
}
if (!$pending -and !$layers) { "Le décor d’Auron est déjà installé."; return }
if ($pending) {
    $includes = @($pending | ForEach-Object { '--include=' + $_.path })
    git -c core.longpaths=true -C $root apply --check --ignore-space-change --whitespace=nowarn @includes -- $patch
    if ($LASTEXITCODE) { throw 'Le correctif ne correspond pas aux sources du projet.' }
}
if (!$PSCmdlet.ShouldProcess($root, "Installer le décor d’Auron")) { return }
if ($pending) {
    git -c core.longpaths=true -C $root apply --ignore-space-change --whitespace=nowarn @includes -- $patch
    if ($LASTEXITCODE) { throw 'Application du correctif échouée.' }
    foreach ($record in $pending) {
        if ((Get-SourceHash (Join-Path $root $record.path)) -ne $record.after) {
            throw "Script différent après installation : $($record.path)"
        }
    }
}
foreach ($layer in $layers) {
    Copy-Item -LiteralPath $layer.source -Destination $layer.destination
    if ((Get-SourceHash $layer.destination) -ne $layer.sha256) { throw "Couche différente après installation : $($layer.destination)" }
}
"Décor d’Auron installé."

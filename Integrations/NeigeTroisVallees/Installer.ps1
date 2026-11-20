[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$Projet)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $Projet).Path
$patch = Join-Path $PSScriptRoot 'NeigeTroisVallees.patch'
$manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Manifest.json') -Raw | ConvertFrom-Json

function Get-SourceHash([string]$path) {
    $text = [IO.File]::ReadAllText($path).Replace("`r`n", "`n")
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))
}

$pending = foreach ($record in $manifest) {
    if ($record.path -notmatch '^Assets/SkiStation/Scripts/[A-Za-z0-9_./-]+\.cs$' -or $record.path.Split('/') -contains '..') {
        throw "Chemin du kit invalide : $($record.path)"
    }
    $destination = Join-Path $root $record.path
    $current = Get-SourceHash $destination
    if ($current -eq $record.after) { continue }
    if ($current -ne $record.before) { throw "Version locale différente : $($record.path)" }
    $record
}
if (!$pending) { 'Le rendu hivernal est déjà installé.'; return }
$includes = @($pending | ForEach-Object { '--include='+$_.path })
git -c core.longpaths=true -C $root apply --check --ignore-space-change --whitespace=nowarn @includes -- $patch
if ($LASTEXITCODE) { throw 'Le correctif ne correspond pas aux sources du projet.' }
if (!$PSCmdlet.ShouldProcess($root,'Installer le rendu hivernal')) { return }
git -c core.longpaths=true -C $root apply --ignore-space-change --whitespace=nowarn @includes -- $patch
if ($LASTEXITCODE) { throw 'Application du correctif échouée.' }
foreach ($record in $pending) {
    if ((Get-SourceHash (Join-Path $root $record.path)) -ne $record.after) {
        throw "Vérification après installation échouée : $($record.path)"
    }
}
"$(@($pending).Count) fichier(s) du rendu hivernal vérifié(s)."

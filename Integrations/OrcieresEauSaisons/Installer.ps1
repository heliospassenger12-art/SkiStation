[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$Projet)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Projet).Path
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'MANIFEST.json') -Raw | ConvertFrom-Json
$issues=[System.Collections.Generic.List[string]]::new()
$pending=[System.Collections.Generic.List[object]]::new()

function Fingerprint([string]$path,[bool]$text) {
    if(!$text){return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}
    $normalized=[IO.File]::ReadAllText($path).Replace(([string][char]13+[char]10),[string][char]10)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normalized)))
}

$version=Join-Path $root 'ProjectSettings/ProjectVersion.txt'
if(!(Test-Path -LiteralPath $version) -or !(Select-String -LiteralPath $version -SimpleMatch 'm_EditorVersion: 6000.3.7f1' -Quiet)){
    throw 'Ce kit requiert le projet Avant Première sous Unity 6000.3.7f1.'
}
foreach($required in $manifest.require){
    $file=Join-Path $root $required.path
    if(!(Test-Path -LiteralPath $file -PathType Leaf)){
        $issues.Add("Prérequis absent : $($required.path)")
    } elseif($required.sha256 -and (Fingerprint $file $false) -ne $required.sha256){
        $issues.Add("Prérequis incompatible : $($required.path)")
    } elseif($required.marker -and !(Select-String -LiteralPath $file -SimpleMatch $required.marker -Quiet)){
        $issues.Add("Prérequis incompatible : $($required.path)")
    }
}
foreach($record in $manifest.files){
    if($record.path -notmatch '^Assets/SkiStation/(Scripts|Resources)/[A-Za-z0-9_./-]+$' -or $record.path.Split('/') -contains '..'){
        throw "Chemin du kit invalide : $($record.path)"
    }
    $source=Join-Path (Join-Path $PSScriptRoot 'Payload') $record.path
    $target=Join-Path $root $record.path
    if(!(Test-Path -LiteralPath $source -PathType Leaf) -or (Fingerprint $source $record.text) -ne $record.sha256){
        $issues.Add("Fichier du kit absent ou modifié : $($record.path)")
        continue
    }
    if(Test-Path -LiteralPath $target -PathType Leaf){
        $current=Fingerprint $target $record.text
        if($current -eq $record.sha256 -or ($record.compatible -and $current -eq $record.compatible)){continue}
        if(!$record.previous -or $current -ne $record.previous){
            $issues.Add("Version locale différente : $($record.path)")
            continue
        }
    } elseif($record.previous){
        $issues.Add("Fichier de la version cible absent : $($record.path)")
        continue
    }
    $pending.Add([pscustomobject]@{source=$source;target=$target;path=$record.path})
}
if($issues.Count){throw ($issues -join [Environment]::NewLine)}
if(!$pending.Count){Write-Output 'Le kit Orcières est déjà installé.';return}
$filesCopied = 0
foreach($item in $pending) {
    if($PSCmdlet.ShouldProcess($item.path,'Installer la version Orcières')){
        New-Item -ItemType Directory -Path (Split-Path -Parent $item.target) -Force | Out-Null
        Copy-Item -LiteralPath $item.source -Destination $item.target -Force
        $filesCopied++
    }
}
Write-Output "$filesCopied fichier(s) installé(s)."

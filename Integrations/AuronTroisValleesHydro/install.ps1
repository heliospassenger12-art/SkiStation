param([Parameter(Mandatory)][string]$Project)
$ErrorActionPreference='Stop'
$root=(Resolve-Path -LiteralPath $Project).Path
$base=Join-Path $root 'Assets/SkiStation/Scripts'
$edits=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'edits.json') -Raw | ConvertFrom-Json -AsHashtable
$nl=[string][char]10
$crlf=[string][char]13+$nl
$writes=[ordered]@{}
foreach($file in $edits.Keys) {
 $path=Join-Path $base $file
 $original=[IO.File]::ReadAllText($path)
 $old=$original.Replace($crlf,$nl)
 $next=$old
 foreach($edit in $edits[$file]){
  if($next.Contains($edit.b)){continue}
  if(!$next.Contains($edit.a)){throw "Point de montage absent : $file"}
  $next=$next.Replace($edit.a,$edit.b)
 }
 if($next -ne $old) {
  if($original.Contains($crlf)){$next=$next.Replace($nl,$crlf)}
  $writes[$path]=$next
 }
}
$source=Join-Path $root 'Assets/SkiStation/Resources/TerrainGeneration'
$catalogue=Join-Path $source 'France50/Catalogue.json'
$catalogueText=[IO.File]::ReadAllText($catalogue)
$catalogueNewline=if($catalogueText.Contains($crlf)){$crlf}else{$nl}
$data=$catalogueText|ConvertFrom-Json -Depth 40
$maps=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'maps.json') -Raw | ConvertFrom-Json -AsHashtable
$copies=[ordered]@{}
foreach($key in $maps.Keys){
 $map=@($data.maps|Where-Object {$_.id -eq $maps[$key].domain})
 if($map.Count -ne 1){throw "Domaine introuvable : $key"}
 $resource=($map[0].resource -replace '^TerrainGeneration/','')+'.bytes'
 if(!(Test-Path -LiteralPath (Join-Path $source $resource) -PathType Leaf)){throw "Relief jouable absent : $key"}
 $input=Join-Path $PSScriptRoot "$key.json"
 $destination=Join-Path $source "Nature/$key.json"
 if(!(Test-Path -LiteralPath $input -PathType Leaf)){throw "Données hydrographiques absentes : $key"}
 if(Test-Path -LiteralPath $destination) {
  if((Get-FileHash -LiteralPath $input).Hash -ne (Get-FileHash -LiteralPath $destination).Hash){throw "Autre couche hydrographique déjà présente : $key"}
 } else {
  $copies[$input]=$destination
 }
 $link="TerrainGeneration/Nature/$key"
 if($map[0].nature -eq $link){continue}
 if($map[0].nature){throw "Une autre couche est déjà liée : $key"}
 $anchor='"resource": "'+$map[0].resource+'"'
 if([regex]::Matches($catalogueText,[regex]::Escape($anchor)).Count -ne 1){throw "Ressource ambiguë : $key"}
 $catalogueText=$catalogueText.Replace($anchor,($anchor+','+$catalogueNewline+'      "nature": "'+$link+'"'))
}
foreach($path in $writes.Keys){[IO.File]::WriteAllText($path,$writes[$path])}
foreach($input in $copies.Keys){
 Copy-Item -LiteralPath $input -Destination $copies[$input]
 $meta="$input.meta"
 if(Test-Path -LiteralPath $meta){Copy-Item -LiteralPath $meta -Destination ($copies[$input]+'.meta')}
}
if($copies.Count -ne 0 -or $catalogueText -ne [IO.File]::ReadAllText($catalogue)){
 [IO.File]::WriteAllText($catalogue,$catalogueText)
}
"Lot hydrologie écrit dans $root"

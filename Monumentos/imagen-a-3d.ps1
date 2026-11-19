# Una imagen de un edificio -> un .glb junto a las otras mallas.
# Uso: .\imagen-a-3d.ps1 -Imagen .\imagenes\capilla-santa-apolonia.jpg -Seed 11
# La foto tiene que ser un solo edificio, entero, de frente o en tres cuartos,
# con fondo plano. Cada lado debe medir 4096 px o menos.
# Hunyuan Geo (https://3d.hunyuan.tencent.com/studio/creation/geo) pide esa
# misma foto, pero la pagina exige iniciar sesion. Este script no entra ahi.
# La malla la hace grok-imagine-3d a traves de three_d.py.
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Imagen,
    [string]$Salida,
    [int]$Seed = 7
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $Imagen)) {
    Write-Error "No esta la imagen: $Imagen"
}
$foto = (Resolve-Path -LiteralPath $Imagen).Path
if (-not $Salida) {
    $nombre = [System.IO.Path]::GetFileNameWithoutExtension($foto) + ".glb"
    $carpeta = Join-Path $PSScriptRoot "glb"
    New-Item -ItemType Directory -Force -Path $carpeta | Out-Null
    $Salida = Join-Path $carpeta $nombre
}
else {
    $dir = Split-Path -Parent $Salida
    if ($dir) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
}
$script = "C:\Users\Eduardo\.grok\bundled\skills\game-assets\scripts\three_d.py"
& py -3 $script generate --image $foto --seed $Seed --out $Salida
if ($LASTEXITCODE -eq 0) { exit 0 }
Write-Host ""
Write-Host "La malla no se bajo. En este equipo grok-imagine-3d responde 400:"
Write-Host "Zero Data Retention no deja usar el paso 3D, porque tiene que guardar la imagen."
Write-Host "No reintentes la misma foto: el rechazo no cambia."
Write-Host ""
Write-Host "Hunyuan Geo, con tu sesion:"
Write-Host "1. Abre https://3d.hunyuan.tencent.com/studio/creation/geo e inicia sesion."
Write-Host "2. Sube una sola foto: edificio entero, tres cuartos, fondo plano."
Write-Host "   Cada lado entre 128 y 5000 px. El archivo, menos de 6 MB."
Write-Host "3. Esa ruta es geometria (malla sin textura). Si la pantalla ofrece Normal, esa trae color."
Write-Host "4. Guarda el archivo en:"
Write-Host "   $Salida"
Write-Host "No lo copies a Assets: el proyecto no tiene importador glTF."
exit $LASTEXITCODE

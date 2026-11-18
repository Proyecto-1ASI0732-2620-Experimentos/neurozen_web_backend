# Comprueba con el oEmbed público de YouTube (sin clave) que cada enlace de la biblioteca existe y permite insertarse.
#   200 = OK (disponible e insertable) · 401 = el dueño bloqueó la inserción · 403/404 = privado, eliminado o no existe
# Si el elegido falla, muestra la primera alternativa que funciona; cámbiala con
#   PATCH /api/v1/resource-libraries/{id} { "contentUrl": "https://www.youtube.com/watch?v=ID" } (token de ADMIN).
# Uso (Windows PowerShell 5+ o PowerShell 7): powershell -ExecutionPolicy Bypass -File tools\check-youtube-links.ps1
$ErrorActionPreference = 'Stop'
function Get-Status([string]$id) {
    $url = "https://www.youtube.com/oembed?format=json&url=" +
        [uri]::EscapeDataString("https://www.youtube.com/watch?v=$id")
    try { (Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15).StatusCode }
    catch { if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 } }
}
if ((Get-Status 'CVMBSsqTGOk') -eq 0) {
    Write-Host 'Sin conexión con www.youtube.com: ejecuta el script desde una red con acceso a YouTube.' -ForegroundColor Red
    exit 2
}
$failed = $false
foreach ($line in Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot 'youtube-links.txt')) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
    $title, $chosen, $alternatives = $line.Split('|')
    $code = Get-Status $chosen
    if ($code -eq 200) { Write-Host "OK     $title -> https://www.youtube.com/watch?v=$chosen"; continue }
    $failed = $true
    $replacement = $alternatives.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) |
        Where-Object { (Get-Status $_) -eq 200 } | Select-Object -First 1
    $alt = if ($replacement) { "https://www.youtube.com/watch?v=$replacement" } else { 'ninguna disponible' }
    Write-Host "FALLA  $title -> $chosen (HTTP $code). Alternativa: $alt" -ForegroundColor Yellow
}
if ($failed) { exit 1 }

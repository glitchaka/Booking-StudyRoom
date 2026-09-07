param(
    [string]$Database = ".\\DataStore\\booking-studyroom.db",
    [string]$Destination = ".\\Backups"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$stamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
Copy-Item $Database (Join-Path $Destination "booking-studyroom_$stamp.db")
Write-Host "Respaldo creado correctamente."

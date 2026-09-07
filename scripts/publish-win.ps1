param(
    [string]$Output = ".\\publish"
)

$ErrorActionPreference = "Stop"

dotnet publish .\\BookingStudyRoom.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $Output

Write-Host "Publicado en $Output"
Write-Host "Ejecuta BookingStudyRoom.exe en el PC que actuará como servidor."

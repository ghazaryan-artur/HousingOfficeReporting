# Build a self-contained single-file HousingOffice.exe for Windows x64.
# Run in PowerShell from the repo root:  ./publish.ps1

$ErrorActionPreference = "Stop"
$proj = "src/HousingOffice.App/HousingOffice.App.csproj"
$out  = "publish"

if (Test-Path $out) { Remove-Item -Recurse -Force $out }

dotnet publish $proj `
    -c Release `
    -r win-x64 `
    --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $out

Write-Host ""
Write-Host "Готово. Файл: $out\HousingOffice.exe" -ForegroundColor Green
Write-Host "Скопируйте эту папку на целевой компьютер и запускайте HousingOffice.exe двойным кликом." -ForegroundColor Green

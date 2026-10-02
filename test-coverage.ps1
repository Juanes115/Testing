param (
    [switch]$Detailed
)

Write-Host "==> Ejecutando pruebas unitarias con recoleccion de cobertura..." -ForegroundColor Cyan
$verbosity = if ($Detailed) { "normal" } else { "minimal" }

dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults --verbosity $verbosity

if ($LASTEXITCODE -ne 0) {
    Write-Host "Hubo errores en las pruebas unitarias." -ForegroundColor Red
    exit $LASTEXITCODE
}

$latestCoverage = Get-ChildItem -Path ./TestResults -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue | 
                  Sort-Object LastWriteTime -Descending | 
                  Select-Object -First 1

if ($latestCoverage) {
    Copy-Item $latestCoverage.FullName -Destination "./coverage.cobertura.xml" -Force
    Write-Host "`n[OK] Archivo de cobertura generado y sincronizado en ./coverage.cobertura.xml" -ForegroundColor Green
    Write-Host "[TIP] Para visualizar la cobertura en VS Code:" -ForegroundColor Yellow
    Write-Host "      1. Haz clic en 'Watch' en la barra inferior izquierda (de Coverage Gutters) o" -ForegroundColor Yellow
    Write-Host "      2. Presiona 'Ctrl+Shift+P' y selecciona 'Coverage Gutters: Display Coverage'" -ForegroundColor Yellow
}

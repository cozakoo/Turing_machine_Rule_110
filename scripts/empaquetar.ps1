<#
.SYNOPSIS
    Empaqueta el proyecto en el archivo .zip de entrega.

.DESCRIPTION
    Genera dist/tp-integrador-regla110.zip con el código fuente, el informe técnico,
    el archivo de casos de prueba y la tabla de transiciones. Se excluyen los
    artefactos de compilación (bin/, obj/) y los metadatos de Git.

.EXAMPLE
    pwsh -File scripts/empaquetar.ps1
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = "dist",
    [string]$ArchiveName = "tp-integrador-regla110.zip"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $root)) {
    throw "No se pudo determinar la raíz del proyecto."
}

$outputPath = Join-Path $root $OutputDirectory
if (-not (Test-Path -LiteralPath $outputPath)) {
    New-Item -ItemType Directory -Path $outputPath | Out-Null
}

$archivePath = Join-Path $outputPath $ArchiveName
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

$included = @(
    "README.md",
    "Rule110",
    "Rule110.TestRunner",
    "docs",
    "scripts",
    "resultados-regla110.txt",
    "tabla-transiciones-maquina-turing.txt",
    "TP_integrador_final.pdf"
)

$staging = Join-Path $outputPath "staging"
if (Test-Path -LiteralPath $staging) {
    Remove-Item -LiteralPath $staging -Recurse -Force
}
New-Item -ItemType Directory -Path $staging | Out-Null

foreach ($item in $included) {
    $source = Join-Path $root $item
    if (-not (Test-Path -LiteralPath $source)) {
        continue
    }

    Copy-Item -LiteralPath $source -Destination $staging -Recurse -Force
}

Get-ChildItem -LiteralPath $staging -Recurse -Force |
    Where-Object { $_.Name -in @("bin", "obj") } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

Get-ChildItem -LiteralPath $staging -Recurse -Force -Directory -Filter ".vs" |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $archivePath -Force
Remove-Item -LiteralPath $staging -Recurse -Force

$size = [math]::Round((Get-Item -LiteralPath $archivePath).Length / 1KB, 1)
Write-Host "Paquete de entrega generado: $archivePath ($size KB)"

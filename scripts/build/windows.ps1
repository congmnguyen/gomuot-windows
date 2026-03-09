[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$RustTarget = "x86_64-pc-windows-msvc",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$CoreDir = Join-Path $ProjectRoot "core"
$WindowsDir = Join-Path $ProjectRoot "platforms/windows"
$AppDir = Join-Path $WindowsDir "GoMuot"
$NativeDir = Join-Path $AppDir "Native"
$PublishDir = Join-Path $WindowsDir "publish"
$CargoProfileDir = if ($Configuration -ieq "Release") { "release" } else { "debug" }
$DllPath = Join-Path $CoreDir "target/$RustTarget/$CargoProfileDir/gomuot_core.dll"

function Require-Command([string]$Name, [string]$Hint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name not found. $Hint"
    }
}

function Get-VersionInfo {
    $rawTag = ""

    if (Get-Command git -ErrorAction SilentlyContinue) {
        try {
            $rawTag = (git -C $ProjectRoot describe --tags --abbrev=0 --match "v*" --exclude "v*-pre*" 2>$null).Trim()
        } catch {
            $rawTag = ""
        }
    }

    if ([string]::IsNullOrWhiteSpace($rawTag)) {
        return @{
            Tag = "v0.0.0"
            Version = "0.0.0"
            AssemblyVersion = "0.0.0.0"
        }
    }

    $version = $rawTag.TrimStart("v")
    $major = 0
    if ($version -match '^(\d+)') {
        $major = [int]$Matches[1]
    }

    return @{
        Tag = $rawTag
        Version = $version
        AssemblyVersion = "$major.0.0.0"
    }
}

Write-Host "Building GoMuot for Windows" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"

Require-Command "cargo" "Install Rust from https://rustup.rs"
Require-Command "dotnet" "Install .NET 8 SDK from https://dotnet.microsoft.com/download"

$versionInfo = Get-VersionInfo
$zipName = "GoMuot-$($versionInfo.Version)-$RuntimeIdentifier.zip"
$zipPath = Join-Path $WindowsDir $zipName

if ($Clean) {
    Write-Host ""
    Write-Host "Cleaning previous artifacts..." -ForegroundColor Yellow
    Get-Process GoMuot -ErrorAction SilentlyContinue | Stop-Process -Force
    Remove-Item -Recurse -Force (Join-Path $AppDir "bin") -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force (Join-Path $AppDir "obj") -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue
    Remove-Item -Force $zipPath -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "[1/3] Building Rust core..." -ForegroundColor Yellow
Push-Location $CoreDir
try {
    if ($Configuration -ieq "Release") {
        cargo build --release --target $RustTarget
    } else {
        cargo build --target $RustTarget
    }
    if ($LASTEXITCODE -ne 0) {
        throw "cargo build failed with exit code $LASTEXITCODE"
    }
} finally {
    Pop-Location
}

if (-not (Test-Path $DllPath)) {
    throw "Rust build succeeded but DLL was not found at: $DllPath"
}

New-Item -ItemType Directory -Force -Path $NativeDir | Out-Null
Copy-Item $DllPath (Join-Path $NativeDir "gomuot_core.dll") -Force
Write-Host "  DLL: $DllPath"

Write-Host ""
Write-Host "[2/3] Publishing WPF app..." -ForegroundColor Yellow
Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue
Push-Location $AppDir
try {
    dotnet publish `
        -c $Configuration `
        -r $RuntimeIdentifier `
        --self-contained false `
        -p:Version=$($versionInfo.Version) `
        -p:FileVersion=$($versionInfo.Version) `
        -p:AssemblyVersion=$($versionInfo.AssemblyVersion) `
        -o $PublishDir `
        -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }
} finally {
    Pop-Location
}

Write-Host "  Publish: $PublishDir"

Write-Host ""
Write-Host "[3/3] Packaging zip..." -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Build complete." -ForegroundColor Green
Write-Host "Publish directory: $PublishDir"
Write-Host "Zip package: $zipPath"

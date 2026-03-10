[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$RuntimeIdentifier = "win-x64",
    [string]$RustTarget = "x86_64-pc-windows-msvc",
    [switch]$Clean,
    [string]$SignToolPath = "",
    [string]$SigningCertificatePath = "",
    [string]$SigningCertificatePassword = "",
    [string]$SigningCertificateThumbprint = "",
    [string]$TimestampUrl = "http://timestamp.digicert.com",
    [switch]$SkipSigning
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

function Get-ValueOrEnv([string]$Value, [string]$EnvName) {
    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }

    $envValue = [Environment]::GetEnvironmentVariable($EnvName)
    if ([string]::IsNullOrWhiteSpace($envValue)) {
        return ""
    }

    return $envValue
}

function Resolve-SignToolPath([string]$PreferredPath) {
    $candidate = Get-ValueOrEnv $PreferredPath "GOMUOT_SIGNTOOL_PATH"
    if (-not [string]::IsNullOrWhiteSpace($candidate)) {
        if (-not (Test-Path $candidate)) {
            throw "signtool.exe was not found at: $candidate"
        }
        return (Resolve-Path $candidate).Path
    }

    $command = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $kitRoots = @(
        (Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"),
        (Join-Path $env:ProgramFiles "Windows Kits\10\bin")
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and (Test-Path $_) }

    foreach ($kitRoot in $kitRoots) {
        $match = Get-ChildItem -Path $kitRoot -Filter "signtool.exe" -Recurse -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($match) {
            return $match.FullName
        }
    }

    throw "signtool.exe not found. Install Windows SDK Signing Tools or pass -SignToolPath."
}

function Get-SigningConfig {
    if ($SkipSigning) {
        return @{
            Enabled = $false
            Reason = "disabled by -SkipSigning"
        }
    }

    $certificatePath = Get-ValueOrEnv $SigningCertificatePath "GOMUOT_SIGN_PFX_PATH"
    $certificatePassword = Get-ValueOrEnv $SigningCertificatePassword "GOMUOT_SIGN_PFX_PASSWORD"
    $certificateThumbprint = Get-ValueOrEnv $SigningCertificateThumbprint "GOMUOT_SIGN_CERT_THUMBPRINT"
    $timestamp = Get-ValueOrEnv $TimestampUrl "GOMUOT_SIGN_TIMESTAMP_URL"

    if ([string]::IsNullOrWhiteSpace($certificatePath) -and [string]::IsNullOrWhiteSpace($certificateThumbprint)) {
        return @{
            Enabled = $false
            Reason = "no signing certificate configured"
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($certificatePath) -and -not (Test-Path $certificatePath)) {
        throw "Signing certificate was not found at: $certificatePath"
    }

    return @{
        Enabled = $true
        SignToolPath = Resolve-SignToolPath $SignToolPath
        CertificatePath = $certificatePath
        CertificatePassword = $certificatePassword
        CertificateThumbprint = $certificateThumbprint
        TimestampUrl = $timestamp
    }
}

function Invoke-SignFile([hashtable]$SigningConfig, [string]$FilePath) {
    if (-not (Test-Path $FilePath)) {
        return
    }

    $arguments = @("sign", "/fd", "sha256")

    if (-not [string]::IsNullOrWhiteSpace($SigningConfig.TimestampUrl)) {
        $arguments += @("/tr", $SigningConfig.TimestampUrl, "/td", "sha256")
    }

    if (-not [string]::IsNullOrWhiteSpace($SigningConfig.CertificatePath)) {
        $arguments += @("/f", $SigningConfig.CertificatePath)
        if (-not [string]::IsNullOrWhiteSpace($SigningConfig.CertificatePassword)) {
            $arguments += @("/p", $SigningConfig.CertificatePassword)
        }
    } else {
        $arguments += @("/sha1", $SigningConfig.CertificateThumbprint)
    }

    $arguments += $FilePath

    Write-Host "  Signing: $FilePath"
    & $SigningConfig.SignToolPath @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "signtool.exe failed for $FilePath with exit code $LASTEXITCODE"
    }
}

Write-Host "Building GoMuot for Windows" -ForegroundColor Cyan
Write-Host "Project: $ProjectRoot"

Require-Command "cargo" "Install Rust from https://rustup.rs"
Require-Command "dotnet" "Install .NET 8 SDK from https://dotnet.microsoft.com/download"

$versionInfo = Get-VersionInfo
$signingConfig = Get-SigningConfig
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
Write-Host "[1/4] Building Rust core..." -ForegroundColor Yellow
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
Write-Host "[2/4] Publishing WinForms app..." -ForegroundColor Yellow
Remove-Item -Recurse -Force $PublishDir -ErrorAction SilentlyContinue
Push-Location $AppDir
try {
    dotnet publish `
        -c $Configuration `
        -r $RuntimeIdentifier `
        --self-contained true `
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
Write-Host "[3/4] Code signing..." -ForegroundColor Yellow
if ($signingConfig.Enabled) {
    $filesToSign = @(
        (Join-Path $PublishDir "GoMuot.exe"),
        (Join-Path $PublishDir "GoMuot.dll"),
        (Join-Path $PublishDir "gomuot_core.dll")
    )

    foreach ($file in $filesToSign) {
        Invoke-SignFile $signingConfig $file
    }
} else {
    Write-Host "  Skipped ($($signingConfig.Reason))." -ForegroundColor DarkYellow
}

Write-Host ""
Write-Host "[4/4] Packaging zip..." -ForegroundColor Yellow
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}

Compress-Archive -Path (Join-Path $PublishDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Build complete." -ForegroundColor Green
Write-Host "Publish directory: $PublishDir"
Write-Host "Zip package: $zipPath"
if ($signingConfig.Enabled) {
    Write-Host "Artifacts signed with Authenticode." -ForegroundColor Green
}

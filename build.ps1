[CmdletBinding()]
param (
    [string]$Configuration = "Release",
    [switch]$Rebuild,
    [switch]$Package,
    [switch]$Clean,
    [switch]$PurgeConfig
)

$ErrorActionPreference = "Stop"

# Resolve active profile from Environment.props
$profile = "DEV"
$envPropsPath = Join-Path $PSScriptRoot "Environment.props"
if (Test-Path $envPropsPath) {
    try {
        [xml]$xml = Get-Content $envPropsPath
        if ($xml.Project.PropertyGroup.R2ModmanProfile) {
            $profile = $xml.Project.PropertyGroup.R2ModmanProfile.Trim()
        }
    } catch { }
}

$deployPluginDir = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\$profile\BepInEx\plugins\XpandedPortal"
$configPath = "$env:APPDATA\r2modmanPlus-local\Valheim\profiles\$profile\BepInEx\config\com.user.xpandedportal.cfg"

if ($Clean) {
    Write-Host "Cleaning XpandedPortal artifacts and deployed files..." -ForegroundColor Cyan

    if (Test-Path $deployPluginDir) {
        Remove-Item -Recurse -Force $deployPluginDir
        Write-Host "[Clean] Removed deployed plugin: $deployPluginDir" -ForegroundColor Yellow
    } else {
        Write-Host "[Clean] No deployed plugin found in profile '$profile'." -ForegroundColor Gray
    }

    if ($PurgeConfig) {
        if (Test-Path $configPath) {
            Remove-Item -Force $configPath
            Write-Host "[Clean] Purged mod configuration: $configPath" -ForegroundColor Yellow
        } else {
            Write-Host "[Clean] No configuration file found to purge." -ForegroundColor Gray
        }
    }

    @("bin", "obj", "dist") | ForEach-Object {
        $localPath = Join-Path $PSScriptRoot $_
        if (Test-Path $localPath) {
            Remove-Item -Recurse -Force $localPath
            Write-Host "[Clean] Removed local folder: $_/" -ForegroundColor Gray
        }
    }

    Write-Host "Clean completed successfully!" -ForegroundColor Green
    return
}

# Ensure user-installed .NET 8 SDK is used
$userDotNet = "$env:LocalAppData\Microsoft\dotnet"
if (Test-Path "$userDotNet\dotnet.exe") {
    $env:DOTNET_ROOT = $userDotNet
    $dotnetExe = "$userDotNet\dotnet.exe"
    $env:Path = "$userDotNet;$env:Path"
} else {
    $dotnetExe = "dotnet"
}

Push-Location $PSScriptRoot
try {
    # Synchronize version from manifest.json to .csproj and Plugin.cs if present
    $manifestPath = Join-Path $PSScriptRoot "manifest.json"
    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $syncVersion = $manifest.version_number
        $csprojPath = Join-Path $PSScriptRoot "XpandedPortal.csproj"
        if (Test-Path $csprojPath) {
            $csprojRaw = Get-Content $csprojPath -Raw
            $csprojUpdated = $csprojRaw -replace '<Version>[^<]+</Version>', "<Version>$syncVersion</Version>"
            if ($csprojRaw -ne $csprojUpdated) {
                Set-Content -Path $csprojPath -Value $csprojUpdated -NoNewline -Encoding UTF8
                Write-Host "[VersionSync] Updated XpandedPortal.csproj -> v$syncVersion" -ForegroundColor Cyan
            }
        }
        $pluginPath = Join-Path $PSScriptRoot "src\Plugin.cs"
        if (Test-Path $pluginPath) {
            $pluginRaw = Get-Content $pluginPath -Raw
            $pluginUpdated = $pluginRaw -replace 'ModVersion\s*=\s*"[^"]+"', "ModVersion = `"$syncVersion`""
            if ($pluginRaw -ne $pluginUpdated) {
                Set-Content -Path $pluginPath -Value $pluginUpdated -NoNewline -Encoding UTF8
                Write-Host "[VersionSync] Updated src/Plugin.cs -> v$syncVersion" -ForegroundColor Cyan
            }
        }
    }

    Write-Host "Building XpandedPortal [$Configuration]..." -ForegroundColor Cyan

    $csproj = Join-Path $PSScriptRoot "XpandedPortal.csproj"
    $buildArgs = @("build", $csproj, "-c", $Configuration)
    if ($Rebuild) {
        $buildArgs += "--no-incremental"
    }

    & $dotnetExe @buildArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Build and deployment completed successfully!" -ForegroundColor Green

    # Compose CHANGELOG.md from changelogs/v*.md if changelogs directory exists
    $changelogsDir = Join-Path $PSScriptRoot "changelogs"
    $changelogPath = Join-Path $PSScriptRoot "CHANGELOG.md"
    if (Test-Path $changelogsDir) {
        $modDisplayName = if ($manifest) { $manifest.name } else { (Get-Item $PSScriptRoot).Name }
        $logHeader = "# Changelog`n`nAll notable changes to **$modDisplayName** will be documented in this file.`n"
        $versionFiles = @(Get-ChildItem -Path $changelogsDir -Filter "v*.md" | ForEach-Object {
            if ($_.BaseName -match '^v?(\d+\.\d+\.\d+.*)$') {
                [PSCustomObject]@{
                    File = $_
                    Version = [version]($matches[1].Split('-')[0])
                    Raw = Get-Content $_.FullName -Raw
                }
            }
        } | Sort-Object -Property Version -Descending)
        if ($versionFiles.Count -gt 0) {
            $composed = $logHeader + "`n" + (($versionFiles | ForEach-Object { $_.Raw.Trim() }) -join "`n`n") + "`n"
            [System.IO.File]::WriteAllText($changelogPath, $composed, [System.Text.Encoding]::UTF8)
            Write-Host "[Changelog] Composed root CHANGELOG.md from $($versionFiles.Count) version fragment(s)." -ForegroundColor Cyan
        }
    }

if ($Package) {
    Write-Host "Packaging Thunderstore release zip..." -ForegroundColor Cyan
    
    $manifestPath = Join-Path $PSScriptRoot "manifest.json"
    $iconPath = Join-Path $PSScriptRoot "icon.png"
    $readmePath = Join-Path $PSScriptRoot "README.md"
    $changelogPath = Join-Path $PSScriptRoot "CHANGELOG.md"
    
    if (-not (Test-Path $manifestPath)) { throw "Missing manifest.json" }
    if (-not (Test-Path $iconPath)) { throw "Missing icon.png" }
    if (-not (Test-Path $readmePath)) { throw "Missing README.md" }
    if (-not (Test-Path $changelogPath)) { throw "Missing CHANGELOG.md" }

    $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
    $version = $manifest.version_number
    $modName = $manifest.name

    $targetDll = Join-Path $PSScriptRoot "bin\$Configuration\netstandard2.1\$modName.dll"
    $targetPdb = Join-Path $PSScriptRoot "bin\$Configuration\netstandard2.1\$modName.pdb"
    if (-not (Test-Path $targetDll)) {
        # Fallback to PortalTweaks.dll if assembly name wasn't renamed
        $fallbackDll = Join-Path $PSScriptRoot "bin\$Configuration\netstandard2.1\PortalTweaks.dll"
        if (Test-Path $fallbackDll) {
            $targetDll = $fallbackDll
            $targetPdb = Join-Path $PSScriptRoot "bin\$Configuration\netstandard2.1\PortalTweaks.pdb"
        } else {
            throw "Missing compiled assembly at $targetDll"
        }
    }

    $distDir = Join-Path $PSScriptRoot "dist"
    if (-not (Test-Path $distDir)) {
        New-Item -ItemType Directory -Path $distDir | Out-Null
    }

    $stagingDir = Join-Path $distDir "staging"
    if (Test-Path $stagingDir) {
        Remove-Item -Recurse -Force $stagingDir
    }
    New-Item -ItemType Directory -Path $stagingDir | Out-Null

    Copy-Item $manifestPath -Destination $stagingDir
    Copy-Item $iconPath -Destination $stagingDir
    Copy-Item $readmePath -Destination $stagingDir
    Copy-Item $changelogPath -Destination $stagingDir
    Copy-Item $targetDll -Destination $stagingDir
    if (Test-Path $targetPdb) {
        Copy-Item $targetPdb -Destination $stagingDir
    }

    $zipFileName = "$modName-$version.zip"
    $zipPath = Join-Path $distDir $zipFileName
    if (Test-Path $zipPath) {
        Remove-Item -Force $zipPath
    }

    Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -Force
    Remove-Item -Recurse -Force $stagingDir

    Write-Host "[Package] Created release zip: $zipPath" -ForegroundColor Green
}
} finally {
    Pop-Location
}

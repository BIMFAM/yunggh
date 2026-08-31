<#
    publish-rhino8.ps1

    Builds yunggh for Rhino 8, packages it as a .yak alongside its manifest.yml,
    and (after an explicit confirmation) pushes it to the Rhino Package Manager.

    Launched by publish-rhino8.bat -- double-click that, not this.
#>

$ErrorActionPreference = 'Stop'

# --- paths ------------------------------------------------------------------
$ScriptDir     = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot      = Split-Path -Parent $ScriptDir
$PackageDir    = Join-Path $ScriptDir 'Rhino 8'
$ManifestPath  = Join-Path $PackageDir 'manifest.yml'
$Staging       = Join-Path $ScriptDir '_build\rh8'
$ProjectPath   = Join-Path $RepoRoot 'yunggh\yunggh.csproj'
$IconSource    = Join-Path $RepoRoot 'yunggh\Resources\yunggh.png'
$YakPath       = 'C:\Program Files\Rhino 8\System\Yak.exe'
$Configuration = 'RH8'

# Assemblies supplied by Rhino/Grasshopper itself -- never ship these.
$ExcludedDlls = @(
    'RhinoCommon.dll', 'Grasshopper.dll', 'GH_IO.dll',
    'Eto.dll', 'Rhino.UI.dll', 'GrasshopperIO.dll'
)

# --- helpers ----------------------------------------------------------------
function Write-Step($number, $text) {
    Write-Host ''
    Write-Host "[$number] $text" -ForegroundColor Cyan
}

function Fail($message) {
    Write-Host ''
    Write-Host "ERROR: $message" -ForegroundColor Red
    exit 1
}

function Write-Utf8NoBom($path, $lines) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllLines($path, $lines, $encoding)
}

# Returns the cached token's expiry as a DateTimeOffset, or $null when there is
# no usable credentials file. Only the expires_at field is read -- never the token.
function Get-YakTokenExpiry($configPath) {
    if (-not (Test-Path $configPath)) { return $null }
    $line = Select-String -Path $configPath -Pattern '^\s*expires_at\s*:\s*(.+)$' |
            Select-Object -First 1
    if (-not $line) { return $null }
    $raw = $line.Matches[0].Groups[1].Value.Trim().Trim('"', "'")
    try { return [datetimeoffset]::Parse($raw) } catch { return $null }
}

# The yak server validates keywords more strictly than 'yak build' does, and it
# only does so at push time -- after the confirmation prompt. Catch it up front.
# Returns the offending keywords, empty when all are fine.
function Get-InvalidKeywords($lines) {
    $bad        = @()
    $inKeywords = $false
    foreach ($line in $lines) {
        if ($line -match '^\s*keywords\s*:') { $inKeywords = $true; continue }
        if (-not $inKeywords) { continue }
        if ($line -match '^\s*-\s*(.+?)\s*$') {
            $keyword = $matches[1].Trim().Trim('"', "'")
            if ($keyword -notmatch '^[A-Za-z0-9_-]+$') { $bad += $keyword }
        } elseif ($line.Trim()) {
            break   # a non-list, non-blank line ends the keywords block
        }
    }
    return $bad
}

function Invoke-YakLogin($yakPath) {
    Write-Host '  Opening Rhino Accounts sign-in in your browser...'
    Write-Host '  (complete the sign-in, then come back to this window)'
    & $yakPath login
    if ($LASTEXITCODE -ne 0) { return $false }
    return $true
}

Write-Host '===========================================================' -ForegroundColor Yellow
Write-Host ' Publish yunggh -> Rhino 8 Package Manager' -ForegroundColor Yellow
Write-Host '===========================================================' -ForegroundColor Yellow

# --- 1. locate tools --------------------------------------------------------
Write-Step 1 'Checking tools'

if (-not (Test-Path $YakPath)) {
    Fail "Yak.exe not found at: $YakPath -- install Rhino 8, or edit the `$YakPath variable in this script."
}
Write-Host "  Yak.exe  : $YakPath"

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Fail 'dotnet was not found on PATH. Install the .NET SDK from https://dotnet.microsoft.com/download'
}
Write-Host "  dotnet   : $($dotnet.Source)"

if (-not (Test-Path $ProjectPath))  { Fail "Project not found: $ProjectPath" }
if (-not (Test-Path $ManifestPath)) { Fail "manifest.yml not found: $ManifestPath" }
if (-not (Test-Path $IconSource))   { Fail "Icon not found: $IconSource" }

# --- 2. version -------------------------------------------------------------
Write-Step 2 'Package version'

$manifestLines = Get-Content -LiteralPath $ManifestPath
$versionLine   = $manifestLines | Where-Object { $_ -match '^\s*version\s*:' } | Select-Object -First 1
if (-not $versionLine) { Fail "No 'version:' line found in $ManifestPath" }

$badKeywords = Get-InvalidKeywords $manifestLines
if ($badKeywords.Count -gt 0) {
    Fail @"
manifest.yml has keywords the yak server will reject: $($badKeywords -join ', ')
Keywords may only contain letters, numbers, dashes and underscores -- no spaces.
Fix them in: $ManifestPath
  e.g. 'computational design' -> 'computational-design'
"@
}

$currentVersion = ($versionLine -replace '^\s*version\s*:\s*', '').Trim().Trim('"', "'")
Write-Host "  Current version: $currentVersion"

$semver = '^(0|[1-9]\d*)(\.(0|[1-9]\d*)){2,3}$'
$answer = Read-Host '  New version (press Enter to keep the current one)'
$newVersion = $currentVersion

if ($answer.Trim()) {
    $answer = $answer.Trim()
    if ($answer -notmatch $semver) {
        Fail "'$answer' is not a valid yak version. Use 3 or 4 numbers with no leading zeros, e.g. 1.0.0 or 1.0.0.1"
    }
    $newVersion = $answer
    $manifestLines = $manifestLines | ForEach-Object {
        if ($_ -match '^\s*version\s*:') { "version: $newVersion" } else { $_ }
    }
    Write-Utf8NoBom $ManifestPath $manifestLines
    Write-Host "  manifest.yml updated to $newVersion (remember to commit this)." -ForegroundColor Green
} else {
    if ($currentVersion -notmatch $semver) {
        Fail "The version already in manifest.yml ('$currentVersion') is not a valid yak version. Enter a valid one, e.g. 1.0.0"
    }
    Write-Host "  Keeping $currentVersion. Note: yak rejects a push of a version that is already published." -ForegroundColor DarkYellow
}

# --- 3. build ---------------------------------------------------------------
Write-Step 3 "Building the $Configuration configuration"

if (Test-Path $Staging) { Remove-Item -LiteralPath $Staging -Recurse -Force }
New-Item -ItemType Directory -Path $Staging -Force | Out-Null

# Forward slashes so the trailing separator MSBuild requires survives argument parsing.
$stagingMsBuild = (($Staging -replace '\\', '/').TrimEnd('/')) + '/'

# CopyLocalLockFileAssemblies puts the NuGet dependencies (Google.Apis.*,
# Newtonsoft.Json, ...) next to the .gha -- library builds skip them by default,
# and without them the packaged components fail to load in Rhino.
& $dotnet.Source build $ProjectPath -c $Configuration `
    "-p:OutputPath=$stagingMsBuild" `
    '-p:CopyLocalLockFileAssemblies=true' `
    --nologo -v:minimal
if ($LASTEXITCODE -ne 0) { Fail "Build failed (exit code $LASTEXITCODE). Nothing was packaged or published." }

$gha = Join-Path $Staging 'yunggh.gha'
if (-not (Test-Path $gha)) { Fail "Build succeeded but yunggh.gha was not produced in: $Staging" }
Write-Host '  Build succeeded.' -ForegroundColor Green

# --- 4. stage the package folder -------------------------------------------
Write-Step 4 "Staging files into '$PackageDir'"

Get-ChildItem -LiteralPath $PackageDir -File |
    Where-Object { @('.gha', '.dll', '.yak', '.pdb') -contains $_.Extension } |
    Remove-Item -Force

Copy-Item $gha -Destination $PackageDir -Force
Write-Host '  + yunggh.gha'

Get-ChildItem -LiteralPath $Staging -Filter '*.dll' |
    Where-Object { $ExcludedDlls -notcontains $_.Name } |
    ForEach-Object {
        Copy-Item $_.FullName -Destination $PackageDir -Force
        Write-Host "  + $($_.Name)"
    }

Copy-Item $IconSource -Destination (Join-Path $PackageDir 'yunggh.png') -Force
Write-Host '  + yunggh.png'

# --- 5. yak build -----------------------------------------------------------
Write-Step 5 'Creating the .yak package'

Push-Location $PackageDir
try {
    & $YakPath build --platform win
    $yakExit = $LASTEXITCODE
} finally {
    Pop-Location
}
if ($yakExit -ne 0) { Fail "yak build failed (exit code $yakExit). Nothing was published." }

$yakFile = Get-ChildItem -LiteralPath $PackageDir -Filter '*.yak' |
           Sort-Object LastWriteTime -Descending |
           Select-Object -First 1
if (-not $yakFile) { Fail 'yak build reported success but no .yak file was found.' }

$sizeMb = [math]::Round($yakFile.Length / 1MB, 2)
Write-Host ''
Write-Host "  Package: $($yakFile.Name)  ($sizeMb MB)" -ForegroundColor Green
Write-Host "  Folder : $PackageDir"
if ($yakFile.Name -notmatch 'rh8') {
    Write-Host "  WARNING: the filename does not contain 'rh8' -- check which Yak.exe ran." -ForegroundColor DarkYellow
}

# --- 6. login ---------------------------------------------------------------
Write-Step 6 'Checking yak credentials'

$yakConfig = Join-Path $env:APPDATA 'McNeel\yak.yml'
$expiry    = Get-YakTokenExpiry $yakConfig

# Log in now rather than after the confirmation prompt, so an expired token
# never turns a confirmed push into a failure. A token about to lapse mid-push
# counts as expired.
$needLogin = $true
if ($null -eq $expiry) {
    if (Test-Path $yakConfig) {
        Write-Host '  Cached credentials found but the expiry could not be read.'
    } else {
        Write-Host '  Not logged in.'
    }
} elseif ($expiry -le [datetimeoffset]::Now.AddMinutes(5)) {
    Write-Host "  Cached token expired $($expiry.ToLocalTime().ToString('yyyy-MM-dd HH:mm'))." -ForegroundColor DarkYellow
} else {
    Write-Host "  Logged in. Token valid until $($expiry.ToLocalTime().ToString('yyyy-MM-dd HH:mm'))."
    $needLogin = $false
}

if ($needLogin) {
    if (-not (Invoke-YakLogin $YakPath)) {
        Fail "yak login failed (exit code $LASTEXITCODE). The .yak file is still in the folder above."
    }
    $expiry = Get-YakTokenExpiry $yakConfig
    if ($expiry) {
        Write-Host "  Logged in. Token valid until $($expiry.ToLocalTime().ToString('yyyy-MM-dd HH:mm'))." -ForegroundColor Green
    } else {
        Write-Host '  Logged in.' -ForegroundColor Green
    }
}

# --- 7. confirm and push ----------------------------------------------------
Write-Step 7 'Publish to the Rhino Package Manager'

Write-Host ''
Write-Host '  About to publish:' -ForegroundColor Yellow
Write-Host '    package : yunggh'
Write-Host "    version : $newVersion"
Write-Host "    file    : $($yakFile.Name)"
Write-Host ''
Write-Host '  This is PERMANENT. Published versions cannot be deleted or' -ForegroundColor Yellow
Write-Host '  overwritten, only yanked.' -ForegroundColor Yellow
Write-Host ''

$confirm = Read-Host "  Type 'y' to publish, anything else to stop"
if ($confirm.Trim().ToLower() -ne 'y') {
    Write-Host ''
    Write-Host 'Push cancelled. The .yak file is ready if you want to push it later:' -ForegroundColor DarkYellow
    Write-Host "  cd '$PackageDir'"
    Write-Host "  & '$YakPath' push $($yakFile.Name)"
    exit 0
}

function Invoke-YakPush($yakPath, $packageDir, $fileName) {
    Push-Location $packageDir
    try {
        $output = & $yakPath push $fileName 2>&1
        $code   = $LASTEXITCODE
    } finally {
        Pop-Location
    }
    $output | ForEach-Object { Write-Host "  $_" }
    return [pscustomobject]@{ ExitCode = $code; Output = ($output -join "`n") }
}

$result = Invoke-YakPush $YakPath $PackageDir $yakFile.Name

# A token can be rejected server-side even when expires_at still looks fine
# (revoked, or a stale cache). Re-authenticate once and try again.
if ($result.ExitCode -ne 0 -and
    $result.Output -match 'cached token|yak login|unauthori[sz]ed|not logged in|authenticat') {

    Write-Host ''
    Write-Host '  The server rejected the cached credentials. Signing in again...' -ForegroundColor DarkYellow
    if (-not (Invoke-YakLogin $YakPath)) {
        Fail "yak login failed (exit code $LASTEXITCODE). The .yak file is still in '$PackageDir'."
    }
    Write-Host ''
    Write-Host '  Retrying the push...' -ForegroundColor Cyan
    $result = Invoke-YakPush $YakPath $PackageDir $yakFile.Name
}

if ($result.ExitCode -ne 0) {
    Fail @"
yak push failed (exit code $($result.ExitCode)).
The .yak file is still available, so you can retry without rebuilding:
  cd '$PackageDir'
  & '$YakPath' login
  & '$YakPath' push $($yakFile.Name)
"@
}

Write-Host ''
Write-Host "Published yunggh $newVersion to the Rhino Package Manager." -ForegroundColor Green
Write-Host 'It may take a few minutes to appear in Rhino. Verify with:'
Write-Host "  & '$YakPath' search yunggh --all --prerelease"
exit 0

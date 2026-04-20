param(
    [ValidateSet("all", "publish", "installer", "clean", "release")]
    [string]$Target = "all"
)

$ErrorActionPreference = "Stop"

$version = (Select-String -Path "src\HassLink\HassLink.csproj" -Pattern '<Version>([^<]+)').Matches[0].Groups[1].Value
$publishDir = "src\HassLink\bin\publish"
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

function Invoke-Publish {
    Write-Host "Publishing v$version..." -ForegroundColor Cyan
    dotnet publish src\HassLink\HassLink.csproj `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishReadyToRun=true `
        --output $publishDir
}

function Invoke-Installer {
    if (-not (Test-Path $iscc)) {
        Write-Error "Inno Setup not found at: $iscc"
    }
    Write-Host "Building installer v$version..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path dist | Out-Null
    & $iscc /DAppVersion=$version installer\setup.iss
}

function Invoke-Release {
    $tag = "v$version"
    $existing = git tag --list $tag
    if ($existing) {
        Write-Error "Tag $tag already exists. Bump <Version> in HassLink.csproj first."
    }
    Write-Host "Tagging $tag and pushing - release pipeline will build the installer." -ForegroundColor Cyan
    git tag $tag
    git push origin $tag
}

switch ($Target) {
    "publish"   { Invoke-Publish }
    "installer" { Invoke-Publish; Invoke-Installer }
    "all"       { Invoke-Publish; Invoke-Installer }
    "release"   { Invoke-Release }
    "clean"     {
        Write-Host "Cleaning..." -ForegroundColor Cyan
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue dist, $publishDir
    }
}

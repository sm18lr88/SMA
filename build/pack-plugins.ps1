# Packs the plugins listed in build\plugin-feed\catalog.json as NuGet packages and writes the static plugin feed into
# artifacts\feed: a NuGet v3 feed under nuget\ and the SMA catalog plugins.json. CI deploys that folder to GitHub Pages.
# Usage: pwsh build\pack-plugins.ps1 [-BaseUrl <feed URL>] [-Catalog <catalog.json>] [-AppPublishDir <published app>]
# Runs only foreground dotnet commands.
[CmdletBinding()]
param(
  # Public URL of artifacts\feed. NuGet needs absolute URLs, so a feed built for one URL does not work at another.
  [string] $BaseUrl = 'https://sm18lr88.github.io/SMA/',
  [string] $Catalog = (Join-Path (Split-Path -Parent $PSScriptRoot) 'build\plugin-feed\catalog.json'),
  # Output of build\pack.ps1 (artifacts\publish). Without it, the script publishes the app itself.
  [string] $AppPublishDir
)

$ErrorActionPreference = 'Stop'
$repo        = Split-Path -Parent $PSScriptRoot
$catalogFile = (Resolve-Path $Catalog).Path
$workDir     = Join-Path $repo 'artifacts\plugin-feed'
$packageDir  = Join-Path $workDir 'packages'
$feedDir     = Join-Path $repo 'artifacts\feed'
$interopDll  = 'SuperMemoAssistant.Interop.dll'

function Invoke-Dotnet {
  & dotnet @args | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed ($LASTEXITCODE)" }
}

Remove-Item $workDir, $feedDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $packageDir | Out-Null

$toolDir = Join-Path $workDir 'tool'
Invoke-Dotnet build (Join-Path $repo 'src\Tools\SuperMemoAssistant.PluginFeed') -c Release -o $toolDir -nologo
$tool = Join-Path $toolDir 'SuperMemoAssistant.PluginFeed.dll'

if (-not $AppPublishDir) {
  $AppPublishDir = Join-Path $workDir 'app'
  Invoke-Dotnet publish (Join-Path $repo 'src\Core\SuperMemoAssistant') -c Release -r win-x64 -o $AppPublishDir -nologo
}
if (-not (Test-Path (Join-Path $AppPublishDir 'PluginHost.exe'))) { throw "$AppPublishDir does not contain a published SMA" }

$entries  = @(Get-Content $catalogFile -Raw | ConvertFrom-Json)
$included = @()

foreach ($entry in $entries) {
  $name = $entry.PackageName

  if ($entry.Project) {
    $target = Join-Path $workDir "plugins\$name"
    Invoke-Dotnet publish (Join-Path $repo $entry.Project) -c Release -r win-x64 --self-contained false -o $target -nologo
    Get-ChildItem $target -Recurse -Include *.xml, *.pdb | Remove-Item -Force

    # PluginHost resolves assemblies missing from the package from the application folder, as for bundled plugins
    # (build\pack.ps1). The Interop assembly stays: SMA reads its version from the package to reject outdated plugins.
    foreach ($file in Get-ChildItem $target -File -Filter *.dll) {
      if ($file.Name -in $interopDll, "$name.dll") { continue }
      $appCopy = Join-Path $AppPublishDir $file.Name
      if ((Test-Path $appCopy) -and (Get-FileHash $appCopy).Hash -eq (Get-FileHash $file.FullName).Hash) {
        Remove-Item $file.FullName -Force
      }
    }

    Invoke-Dotnet $tool pack --catalog $catalogFile --id $name --source $target --out $packageDir
  }
  elseif ($entry.PackageUrl) {
    $download = Join-Path $packageDir "$name.nupkg"
    Invoke-WebRequest -Uri $entry.PackageUrl -OutFile $download
    if ((Get-FileHash $download -Algorithm SHA256).Hash -ne $entry.Sha256) { throw "$name does not match its Sha256 in $catalogFile" }
  }
  else {
    throw "$name needs either Project or PackageUrl in $catalogFile"
  }

  $included += $entry
}

$effectiveCatalog = Join-Path $workDir 'catalog.json'
ConvertTo-Json -InputObject $included -Depth 5 | Set-Content $effectiveCatalog -Encoding utf8

Invoke-Dotnet $tool feed --catalog $effectiveCatalog --packages $packageDir --base-url $BaseUrl --out $feedDir

Get-ChildItem $packageDir -Filter *.nupkg | Select-Object Name, @{ n = 'MB'; e = { [math]::Round($_.Length / 1MB, 1) } } | Format-Table | Out-Host

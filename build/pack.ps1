# Builds a Velopack release of SuperMemo Assistant into artifacts\releases (Setup.exe, full/delta packages, RELEASES feed).
# Usage: pwsh build\pack.ps1 [-Version 3.1.0] [-Channel win]
# Runs only foreground dotnet commands; it never launches the produced installer.
[CmdletBinding()]
param(
  [string] $Version,
  [string] $Channel = 'win',
  # Thumbprint of a code-signing certificate in the CurrentUser or LocalMachine store. Empty: the release is unsigned.
  [string] $SignThumbprint = $env:SMA_SIGN_THUMBPRINT
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

if (-not $Version) {
  $props = [xml](Get-Content (Join-Path $repo 'Directory.Build.props'))
  $Version = @($props.Project.PropertyGroup.Version | Where-Object { $_ })[0]
}

# Plugins that SMA ships in <app>\Plugins (see IPluginLocations.PluginBundledDir).
$bundledPlugins = @(
  'src\Plugins\SuperMemoAssistant.Plugins.Books\src\SuperMemoAssistant.Plugins.Books\SuperMemoAssistant.Plugins.Books.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.Dictionary\src\SuperMemoAssistant.Plugins.Dictionary\SuperMemoAssistant.Plugins.Dictionary.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.Formulation\src\SuperMemoAssistant.Plugins.Formulation\SuperMemoAssistant.Plugins.Formulation.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.ImageOcclusion\src\SuperMemoAssistant.Plugins.ImageOcclusion\SuperMemoAssistant.Plugins.ImageOcclusion.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.Import\src\SuperMemoAssistant.Plugins.Import\SuperMemoAssistant.Plugins.Import.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.LateX\src\SuperMemoAssistant.Plugins.LaTeX\SuperMemoAssistant.Plugins.LaTeX.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.LocalApi\src\SuperMemoAssistant.Plugins.LocalApi\SuperMemoAssistant.Plugins.LocalApi.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.PDF\src\SuperMemoAssistant.Plugins.PDF\SuperMemoAssistant.Plugins.PDF.csproj',
  'src\Plugins\SuperMemoAssistant.Plugins.Writing\src\SuperMemoAssistant.Plugins.Writing\SuperMemoAssistant.Plugins.Writing.csproj'
)

$publishDir = Join-Path $repo 'artifacts\publish'
$releaseDir = Join-Path $repo 'artifacts\releases'
Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $releaseDir | Out-Null

dotnet tool restore | Out-Host
dotnet publish src\Core\SuperMemoAssistant -c Release -r win-x64 -o $publishDir -nologo | Out-Host
if ($LASTEXITCODE -ne 0) { throw "publish failed ($LASTEXITCODE)" }

foreach ($project in $bundledPlugins) {
  $name   = [IO.Path]::GetFileNameWithoutExtension($project)
  $target = Join-Path $publishDir "Plugins\$name"
  # win-x64 keeps only the x64 native libraries (for example pdfium.dll) and puts them next to the plugin.
  dotnet publish $project -c Release -r win-x64 --self-contained false -o $target -nologo | Out-Host
  if ($LASTEXITCODE -ne 0) { throw "publish of $name failed ($LASTEXITCODE)" }
  Get-ChildItem $target -Recurse -Include *.xml, *.pdb | Remove-Item -Force
  if (-not (Test-Path (Join-Path $target "$name.dll"))) { throw "$name.dll is missing from $target" }

  # PluginHost falls back to the application folder (PluginLoader), so byte-identical copies are redundant.
  foreach ($file in Get-ChildItem $target -File -Filter *.dll) {
    $appCopy = Join-Path $publishDir $file.Name
    if ((Test-Path $appCopy) -and (Get-FileHash $appCopy).Hash -eq (Get-FileHash $file.FullName).Hash) {
      Remove-Item $file.FullName -Force
    }
  }
}

foreach ($required in 'SuperMemoAssistant.exe', 'PluginHost.exe', 'SuperMemoAssistant.Hooks.Agent.dll') {
  if (-not (Test-Path (Join-Path $publishDir $required))) { throw "$required is missing from the publish output" }
}

# vpk signs Setup.exe, Update.exe, and every binary of the package with signtool.
$signArgs = @()
if ($SignThumbprint) {
  $signArgs = @('--signParams', "/fd sha256 /sha1 $SignThumbprint /tr http://timestamp.sectigo.com /td sha256")
}

# --framework makes Setup.exe install the .NET 10 desktop runtime when it is missing.
dotnet vpk pack `
  --packId SuperMemoAssistant `
  --packVersion $Version `
  --packTitle 'SuperMemo Assistant' `
  --packAuthors 'SMA Community' `
  --packDir $publishDir `
  --mainExe SuperMemoAssistant.exe `
  --runtime win-x64 `
  --framework net10.0-x64-desktop `
  --channel $Channel `
  --outputDir $releaseDir @signArgs | Out-Host
if ($LASTEXITCODE -ne 0) { throw "vpk pack failed ($LASTEXITCODE)" }

Get-ChildItem $releaseDir | Select-Object Name, Length | Format-Table | Out-Host

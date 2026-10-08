# Releases and the plugin feed

[Back to the README](../README.md)

## Release

CI (`.github/workflows/build.yml`) builds and tests every push and pull request. Hosted runners have no SuperMemo, so the end-to-end tests skip there.

To publish a release, do these steps:

1. Run the end-to-end tests locally with `SMA_E2E=1`.
2. Create the tag with `git tag vX.Y.Z`, then push with `git push origin main --follow-tags`.
3. CI publishes the installer as a GitHub Release and deploys the plugin feed to GitHub Pages.

Installed copies of SMA find updates on the GitHub Releases. The Beta and Nightly channels also accept pre-releases.

The release job signs `Setup.exe` and every binary of the package that is not signed yet with the project's self-signed certificate (`build/signing/sma-codesign.cer`, timestamped by Sectigo). It reads the certificate from the repository secrets `SMA_SIGN_PFX_BASE64` and `SMA_SIGN_PFX_PASSWORD`; without them, for example in a fork, the release is unsigned. Windows SmartScreen still warns about a self-signed `Setup.exe`. The packages of the plugin feed are not signed.

## Plugin feed

"Browse plugins" in SMA lists, installs, and updates plugins from a static feed on the GitHub Pages site of this repository. The feed has two parts:

- The catalog `https://sm18lr88.github.io/SMA/plugins.json` lists the plugins that SMA shows.
- The NuGet v3 feed `https://sm18lr88.github.io/SMA/nuget/index.json` serves the packages.

A tag `vX.Y.Z` makes CI run `build\pack-plugins.ps1` and deploy `artifacts\feed`. The script packs each plugin in `build\plugin-feed\catalog.json`. Each package holds the published plugin folder under `lib\net10.0-windows10.0.19041\`. The package keeps `SuperMemoAssistant.Interop.dll`, because SMA reads its version to reject outdated plugins. The script removes other assemblies that are identical to the copies of the app.

The package version is the product version of the plugin assembly. This is `Version` in `Directory.Build.props`, unless the plugin project sets its own version. To publish a plugin update, increase that version before you push the tag. Each deployment replaces the whole site, so the feed holds only the current version of each plugin.

A plugin from the feed and a bundled plugin can have the same name. In this case, SMA starts the copy with the higher version. If the versions are equal, SMA starts the copy from the feed. After you install such a plugin, restart SMA.

To publish a third-party plugin, use one of these methods:

1. Open a pull request that adds the plugin to `build\plugin-feed\catalog.json`. Set `PackageUrl` to a prebuilt `.nupkg` file and `Sha256` to its hash. The package must have the layout above and no NuGet dependencies.
2. Host your own feed. Run `build\pack-plugins.ps1 -BaseUrl <your URL> -Catalog <your catalog>` and publish `artifacts\feed`. Users then set `Updates.PluginsUpdateUrl` and `Updates.PluginsUpdateNuGetUrls` in `Configs\Core\CoreCfg.json` to your URLs. SMA reads only one catalog, so your catalog must also list the other plugins that your users want.

To turn off the online catalog, set `Updates.EnablePluginsUpdates` to `false`.

Before the first tag, an owner of the repository must do these steps one time:

1. In **Settings > Pages**, set **Source** to **GitHub Actions**.
2. In **Settings > Environments > github-pages**, add the tag rule `v*` under **Deployment branches and tags**.

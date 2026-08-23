# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versions follow major.minor.hotfix (e.g. 1.2.3).

## [2.1.3] - 2026-08-24

### Added

- Automatic build-number tracking (4th version component), consolidated `Build\` output
  directory for all build artifacts.

## [2.1.2] - 2026-06-28

### Fixed

- The 2.1.1 fix only updated `Window.Title` (the taskbar/Alt-Tab title), which has no visible
  effect here since the window uses a custom-drawn title bar (`WindowStyle="None"` +
  `WindowChrome`). The actual on-screen title text was a separate `TextBlock` bound to
  `SelectedAccount.DisplayName` with a hardcoded `FallbackValue`/`TargetNullValue` of
  "Teron's Email Client" (using a typographic apostrophe, which made it easy to miss in a
  plain-text search) - completely independent of the `Window.Title` property. The window now
  sets both consistently from code-behind, with the version number included: the visible title
  bar always matches the taskbar title, showing "Teron Email Client v2.1.2" or, when an account
  is selected, "{account} - Teron Email Client v2.1.2".

## [2.1.1] - 2026-06-28

### Changed

- Dropped the possessive form: the app's display name is now "Teron Email Client (TEC)"
  instead of "Teron's Email Client (TEC)", matching the non-possessive naming used across this
  user's other apps.
- The main window's title bar, the single-instance-already-running dialog, the README, and the
  installer script all now read this name consistently. The title bar and dialog pull it from
  the assembly's `<Product>` metadata at runtime (`Services/AppInfo.cs`) instead of a separately
  hardcoded string, so they can't drift out of sync with the project file again.

## [2.1.0] - 2026-06-26

### Added

- An [Inno Setup](https://jrsoftware.org/isinfo.php) script (`Installer/TeronEmailClient.iss`)
  that packages the self-contained publish output into a proper Windows installer: Start Menu
  shortcuts, an optional desktop shortcut, a license page, and a normal uninstall entry in
  "Apps & features".
- Installer creation is wired directly into the publish pipeline via a `BuildInnoSetupInstaller`
  MSBuild target (`AfterTargets="Publish"`) in the `.csproj`, so running
  `dotnet publish -p:PublishProfile=win-x64` — or clicking **Publish** in Visual Studio with
  that profile selected — builds, publishes, *and* produces
  `bin\InstallerPackage\TeronEmailClientSetup-win-x64.exe` in one step. No separate tool
  invocation or manual script run is needed. If Inno Setup isn't installed, the step is skipped
  with an MSBuild warning rather than failing the publish.
- The app version is passed from the `.csproj` into the installer script as a preprocessor
  define, so the installer's version can't silently drift from the app's.

### Fixed

- The default install directory was `C:\Program Files\Teron's Email Client` — the apostrophe
  and space can trip up scripts/tools that don't quote paths. Changed to
  `C:\Program Files\TeronEmailClient`.

## [2.0.1] - 2026-06-26

### Fixed

- Resource images (`email.ico`, `gmail.png`, `outlook.png`) were moved into a `Resources/`
  folder; the `.csproj` and the window/title-bar icon references were updated automatically,
  but the hardcoded `Image` sources on the welcome screen and the "Add account" dialog were
  not, leaving the Gmail/Outlook logos blank. Updated all remaining references to
  `/Resources/...` and removed the now-unused `ServiceDefinition.IconPath` property.
- Removing the last remaining account left a stale, garbled frame of that account's last
  rendered page ghosted on screen over the welcome view. Caused by disposing the WebView2
  control in the same tick as switching to the empty/welcome state, before WPF had repainted
  the "now hidden" state — a known WebView2/WPF airspace timing issue. Fixed by updating the
  selected account *before* removing it from the list, and deferring the native control's
  teardown (`Children.Remove` + `Dispose`) to a Background-priority dispatcher callback.

## [2.0.0] - 2026-06-26

Full rewrite of the application, moving off .NET Framework 4.8.1/WinForms onto .NET 10/WPF,
with a redesigned shell and several behavioral changes.

### Changed

- Migrated the project from a .NET Framework 4.8.1 WinForms app (legacy `.csproj`,
  `packages.config`) to an SDK-style `net10.0-windows` WPF app with `PackageReference`s.
- Replaced the two-window flow (`Service_Select` then `Main_Form`) with a single shell window
  that has a persistent account sidebar, browser-style navigation toolbar, and a welcome/empty
  state for first-time setup.
- Multiple accounts are now supported simultaneously, each with its own isolated WebView2
  profile (separate cookies/session), and switching between them no longer reloads the page —
  the WebView2 instances stay alive in the background.
- Configuration moved from an XML file in `Documents\TeronEmailClient\config.xml` to JSON at
  `%LocalAppData%\TeronEmailClient\config.json`, and now stores a list of accounts instead of
  a single selected service URL.
- Replaced the icon-font (Segoe Fluent Icons/Segoe MDL2 Assets) glyphs originally used for the
  window chrome and toolbar with hand-authored vector icons, after finding that the relevant
  Private Use Area glyphs reliably render as missing-glyph boxes under .NET (Core) WPF's text
  engine on this machine, despite the fonts and glyphs resolving correctly under .NET Framework
  WPF.
- Replaced the ABV Mail quick-add tile with a single "Custom mailbox" option, and added real
  Gmail/Outlook logos to the account picker tiles.
- Replaced the unconditional "kill every other process with the same executable name" single
  instance check with a named-mutex-based check that just declines to start a second instance.

### Removed

- The unused `HtmlAgilityPack` dependency (referenced in the project file but never used in
  code).
- WinForms (`Main_Form`, `Service_Select`), `App.config`, and the old `packages.config`-based
  NuGet restore.

### Added

- Light/dark theme support, switchable from Settings.
- Self-contained, single-file publish profiles for `win-x64` and `win-x86`.
- Basic unhandled-exception logging to `%LocalAppData%\TeronEmailClient\error.log`.

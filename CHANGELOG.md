# Changelog

## 2.0.0 — .NET 10 / WPF rewrite

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

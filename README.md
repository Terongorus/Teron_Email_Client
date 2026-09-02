# Teron Email Client (TEC)

A lightweight, modern desktop shell for your webmail accounts. Add Gmail, Outlook, or any
custom webmail URL and switch between them instantly, side by side, the way Outlook 365's
account switcher works — without the overhead of a full native mail protocol implementation.

Built with **.NET 10** and **WPF**.

## Features

- **Multi-account sidebar** — add Gmail, Outlook, and custom webmail accounts; switch between
  them instantly without reloading, since each account keeps its own live, isolated
  [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/) instance and browser
  profile (separate cookies/session per account, so you can be signed into two Gmail accounts
  at once).
- **Fluent-styled shell** — custom title bar, account avatars, browser-style navigation
  (back/forward/reload/home), light and dark themes.
- **OAuth-friendly** — sign-in popups (Google/Microsoft account pickers, 2FA) are handled as
  real popup windows sharing the parent account's session, instead of being silently blocked.
- **Settings** — toggle "remember last account on startup", switch theme, and remove accounts
  (which also deletes that account's local browser profile data).

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (to build) or the .NET 10 Desktop
  Runtime (to run a framework-dependent build)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)
  (preinstalled on most current Windows systems)

## Building and running

```bash
dotnet build
dotnet run
```

## Publishing a standalone build

Self-contained, single-file publish profiles are included for both architectures, so the
result is a single `.exe` with no separate .NET runtime install required:

```bash
dotnet publish -p:PublishProfile=win-x64 -c Release
dotnet publish -p:PublishProfile=win-x86 -c Release
```

(Or, in Visual Studio: right-click the project → **Publish** → pick the `win-x64`/`win-x86`
profile.) The published app goes to `Build\Publish\TeronEmailClient\win-x64\` (or `win-x86`).

### Installer package

Publishing also builds a ready-to-distribute Windows installer automatically — no separate
step required. It uses [Inno Setup](https://jrsoftware.org/isinfo.php), so install it once
first:

```bash
winget install JRSoftware.InnoSetup
```

After that, every `dotnet publish -p:PublishProfile=win-x64` (or the Visual Studio Publish
button) also produces:

```text
Build\InstallerPackage\TeronEmailClientSetup-x64.exe
```

That single file is what you'd attach to a GitHub release. If Inno Setup isn't installed, this
step is skipped with a build warning — the publish itself still succeeds. See
`Installer/TeronEmailClient.iss` for the packaging script and the `BuildInnoSetupInstaller`
MSBuild target in `TeronEmailClient.csproj` for how it's wired into the publish pipeline.

## Configuration

Accounts and settings are stored as JSON at
`%LocalAppData%\TeronEmailClient\config.json`. Each account's isolated browser profile lives
under `%LocalAppData%\TeronEmailClient\Profiles\`.

## License

GPL-3.0 — see [LICENSE.txt](LICENSE.txt).

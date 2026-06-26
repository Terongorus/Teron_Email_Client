# Teron's Email Client (TEC)

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

Output goes to `bin\Release\net10.0-windows\publish\win-x64\` (or `win-x86`).

## Configuration

Accounts and settings are stored as JSON at
`%LocalAppData%\TeronEmailClient\config.json`. Each account's isolated browser profile lives
under `%LocalAppData%\TeronEmailClient\Profiles\`.

## License

GPL-3.0 — see [LICENSE.txt](LICENSE.txt).

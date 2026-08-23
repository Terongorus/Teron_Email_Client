# Phase 0 - Compliance Gap Remediation

A compliance audit of `Teron_Email_Client` against this user's standing personal conventions
found several gaps. This phase closes them in one pass. Starting point: `dev` branch, working
tree clean, `TeronEmailClient.csproj` hardcoding `<Version>2.1.2</Version>`, no
`Directory.Build.props`, no `/docs` folder, and `CHANGELOG.md` using a free-text-title header
style instead of Keep a Changelog.

## Stray fossil file removed

`TeronEmailClient.csproj.user` was sitting in the repo root, untracked (it's `*.user`-gitignored)
but containing a stale hardcoded path left over from before this app moved under the
`Teron_Applications` workspace root. Visual Studio regenerates this file on next load, so it was
simply deleted from disk - nothing to commit, nothing lost.

## Consolidated build output tree (BUILDER)

Added `Directory.Build.props` at the repo root so all build output - regular compile output,
publish output, and intermediate `obj` files - lands under one `Build\` tree instead of scattered
`bin\`/`obj\` folders:

- `Build\$(Configuration)\$(MSBuildProjectName)\` for normal build output
  (`Build\Debug\TeronEmailClient\`, `Build\Release\TeronEmailClient\`).
- `Build\Publish\$(MSBuildProjectName)\$(RuntimeIdentifier)\` for publish output.
- `Build\obj\$(MSBuildProjectName)\` for intermediate files.

`/Build/` was added to `.gitignore`.

This app also ships a DISSEMINATE installer pipeline (`Properties/PublishProfiles/*.pubxml`,
`Installer/TeronEmailClient.iss`, and a `BuildInnoSetupInstaller` MSBuild target in the `.csproj`).
`Directory.Build.props`'s own `PublishDir` formula is known (from prior projects) to silently
collapse the `$(RuntimeIdentifier)` segment to empty during an actual publish, so each pubxml's
own `<PublishDir>` was changed to an explicit, unambiguous path instead of relying on it:

- `win-x64.pubxml`: `bin\Publish\TeronEmailClient_Win_x64\` -> `Build\Publish\TeronEmailClient\win-x64\`
- `win-x86.pubxml`: `bin\Publish\TeronEmailClient_Win_x86\` -> `Build\Publish\TeronEmailClient\win-x86\`

No `dotnet publish` or installer build was run as part of this phase (out of scope) - the pubxml
changes were reviewed by inspection only.

**Gotcha hit during verification:** building Debug then Release back-to-back produced bogus
`CS0102`/`CS0111` duplicate-member errors (`AddAccountWindow`, `SettingsWindow`, `MainWindow` all
reporting fields/`InitializeComponent`/`IComponentConnector.Connect` defined more than once).
This is a known false alarm from stale generated XAML code-behind (`*.g.cs`) left in `Build\obj\`
after a config switch, not a real code regression. Fixed by deleting `Build\obj\` and rebuilding
clean, per the documented workaround - not treated as a real bug.

## Auto-incrementing build number (CODEX)

`TeronEmailClient.csproj` previously hardcoded `<Version>2.1.2</Version>` /
`<AssemblyVersion>2.1.2</AssemblyVersion>` / `<FileVersion>2.1.2</FileVersion>`. Replaced with:

- `<MajorMinorPatchVersion>2.1.3</MajorMinorPatchVersion>` (2.1.3 is a hotfix bump - build-tooling
  and versioning-identity work, not a user-facing change).
- A `BuildNumberFile` pointing at `BuildNumber.txt` (created, tracked in git, starting at `0`).
- An `IncrementBuildNumber` target (`BeforeTargets="BeforeBuild"`) that reads the previous build
  number, increments it, writes it back, and derives `AssemblyVersion`/`FileVersion`/`Version` as
  `$(MajorMinorPatchVersion).$(BuildNumber)` - a real 4th version component that advances on every
  build rather than being edited by hand.

`Installer/TeronEmailClient.iss`'s `MyAppVersion` fallback (`#define MyAppVersion "2.1.2"`) was
bumped to `"2.1.3"` to match.

## CHANGELOG.md reformatted to Keep a Changelog

Every existing `## ...` header used a free-text-title style (e.g.
`## 2.1.2 — Fix custom title bar not reflecting the display name fix`) instead of this user's
`## [X.Y.Z] - YYYY-MM-DD` convention. Rewrote all five historical headers, preserving every word of
body content underneath untouched. Dates were not guessed - each one was taken from
`git log --follow -- CHANGELOG.md`, matching the commit that actually introduced that section:

| Version | Date       | Source commit                             |
|---------|------------|--------------------------------------------|
| 2.1.2   | 2026-06-28 | `acb407b` - title bar fix                  |
| 2.1.1   | 2026-06-28 | `e4dbd50` - display name cleanup           |
| 2.1.0   | 2026-06-26 | `d840b7c` - installer packaging            |
| 2.0.1   | 2026-06-26 | `94c0467` - post-rewrite fixes             |
| 2.0.0   | 2026-06-26 | `b01aac2` - .NET 10 / WPF rewrite           |

Added the missing format preamble (Keep a Changelog link + the major.minor.hotfix note), and a
new top entry, `## [2.1.3] - 2026-08-24`, documenting the build-number tracking and consolidated
`Build\` output directory added in this phase.

## Verification

- `dotnet build TeronEmailClient.csproj -c Debug` - succeeded, 0 warnings, 0 errors. Output
  confirmed under `Build\Debug\TeronEmailClient\TeronEmailClient.dll`.
- `dotnet build TeronEmailClient.csproj -c Release` - hit the stale-`obj` gotcha described above
  on the first attempt; after deleting `Build\obj\` and rebuilding, succeeded with 0 warnings, 0
  errors. Output confirmed under `Build\Release\TeronEmailClient\TeronEmailClient.dll`.
- `BuildNumber.txt` was reset to `0` before the final commit - the verification builds above
  exercised and confirmed the increment mechanism locally, but the committed starting value is the
  clean `0` baseline the mechanism is meant to count up from.
- `dotnet publish` and the Inno Setup installer build were intentionally not run (out of scope for
  this phase).

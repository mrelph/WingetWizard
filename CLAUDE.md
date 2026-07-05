# CLAUDE.md

WingetWizard (Avalonia edition): a cross-platform .NET 6 / Avalonia 11 desktop GUI for the
Windows Package Manager (winget), with AI-assisted upgrade research via the Anthropic and
Perplexity APIs. Single project, no solution file, no test project.

## Commands

Requires the .NET 6 SDK (NOT currently installed on this Mac — see Gotchas).

- Build: `dotnet build WingetWizard.Avalonia.csproj`
- Run: `dotnet run --project WingetWizard.Avalonia.csproj`
- Publish (self-contained): `dotnet publish WingetWizard.Avalonia.csproj -c Release --self-contained true -r win-x64` (also `linux-x64`, `osx-x64`; pubxml profiles for win-x64/x86/arm64 in `Properties/PublishProfiles/`)
- Tests: none exist — do not invent a test command.

## Architecture

- Entry point: `AvaloniaProgram.cs` → `AvaloniaApp.axaml.cs` (builds the
  `Microsoft.Extensions.DependencyInjection` container, registers services + viewmodels) → `MainWindow`.
- MVVM via CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`).
- `Views/*.axaml` — pages (Dashboard, Packages, Updates, AIResearch, Settings, SearchDialog)
  swapped inside `MainWindow`; each pairs with a class in `ViewModels/`.
- `Services/` — interface + implementation pairs, injected via DI:
  - `PackageService` runs winget by shelling out to `powershell.exe` (whitelist-validated commands).
  - `AIService` calls `api.anthropic.com/v1/messages` (Claude) and `api.perplexity.ai` (model `sonar`);
    two-stage flow: Perplexity research → Claude formatting.
  - `SettingsService` persists `settings.json` next to the executable (holds API keys; gitignored).
- `Models/`, `Converters/`, `Utils/`, `Styles/` (design tokens in `Styles/DesignTokens.axaml`).

## Conventions

- New services: define an `I*` interface, implement, register in the DI container in `AvaloniaApp.axaml.cs`.
- Use design tokens / component styles from `Styles/` rather than inline colors or ad-hoc styles.
- Modern Avalonia UI only — do not fall back to WinForms/WPF patterns (see `.amazonq/rules/ModernApp.md`).
- AI model IDs are hardcoded in `ViewModels/SettingsViewModel.cs` (default `claude-sonnet-4-20250514`)
  and read in `Services/AIService.cs`. Never invent model IDs; if changing models, update both files.
- Compiled bindings are OFF (`AvaloniaUseCompiledBindingsByDefault=false`); XAML bindings are
  reflection-based, so binding errors surface at runtime, not compile time.

## Gotchas

- **Windows-only at runtime**: the UI runs cross-platform, but `PackageService` requires
  `powershell.exe` + winget, so all package operations fail on macOS/Linux. Verify winget behavior
  on a Windows machine.
- **No .NET SDK on this Mac**: `dotnet` is not installed here — you cannot build or run locally
  without installing the .NET 6 SDK first. Don't claim builds pass without running one.
- **Duplicate repos**: this repo and `/Volumes/CodingProjects/UpgradeApp` both push to
  `github.com/mrelph/WingetWizard` (this repo tracks `main`; UpgradeApp tracks `master`).
  `/Volumes/CodingProjects/WingetWizard_Working` is a non-git copy. Confirm the target branch
  before any push.
- **Stale docs**: `DEPLOYMENT.txt` describes the older WinForms `UpgradeApp.exe` project, not this
  one. README status claims are aspirational — verify against code.
- **Secrets**: API keys live in runtime `settings.json` (gitignored) — never commit or hardcode keys.
- .NET 6 is end-of-life; expect NU1903-style package vulnerability warnings when building.
- `WingetWizard_Avalonia_Debug.log` is committed at repo root — don't treat it as current output.

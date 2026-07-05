# WingetWizard (WinForms) — CLAUDE.md

> **Multi-directory note:** This repo shares the remote https://github.com/mrelph/WingetWizard
> with `/Volumes/CodingProjects/WinGetModern`, but they are **different apps with unrelated git
> histories**: this directory is the .NET 6 **Windows Forms** app on branch `master`; WinGetModern
> is an **Avalonia** rewrite on branch `main` (the remote's default branch). Both were last
> committed 2026-06-06 — both are live; do not treat this one as stale. `WingetWizard_Working`
> is a non-git Avalonia scratch copy (untouched since Aug 2025) — ignore it. Never push `master`
> work to `main` or vice versa.

AI-enhanced Windows package manager: a WinForms UI over `winget` with AI upgrade research via
Anthropic Claude API, AWS Bedrock, and Perplexity.

## Commands

```bash
dotnet build -c Release                 # build
dotnet publish -c Release --self-contained true -r win-x64 -p:PublishSingleFile=true  # ship (~138MB exe)
build-and-run-v2.bat                    # Windows: clean + restore + build + launch
```

There is no test project and no linter config. The app targets `net6.0-windows` (WinForms,
`win-x64`); `EnableWindowsTargeting` is set so it compiles on macOS/Linux, but it only *runs*
on Windows 10/11 with winget installed.

## Architecture

- `MainForm.cs` (~160KB) — entire UI **and** the `Main()` entry point (`[STAThread]` at top of
  file). There is no `Program.cs`, despite what `docs/PROJECT_STRUCTURE.md` claims.
- `Services/` — constructor-injected service classes, one concern each:
  - `PackageService` / `PackageDiscoveryService` — winget list/upgrade/search/install
  - `AIService` — two-stage research (Perplexity gathers data → primary LLM writes report),
    with automatic Anthropic↔Bedrock fallback
  - `BedrockService`, `BedrockModelDiscoveryService` — AWS Bedrock + dynamic model listing
  - `SecureSettingsService` (DPAPI-encrypted keys), `SettingsService`, `ConfigurationValidationService`
  - `CachingService`, `VirtualizationService`, `SearchFilterService`, `HealthCheckService`,
    `PerformanceMetricsService`, `ReportService`
- `Models/` — `UpgradableApp`, `PackageSearchResult`, `HealthCheckResult`
- `Utils/AppConstants.cs` — all AI model IDs, timeouts, and magic strings live here; add new
  model IDs here, never inline.

## Conventions

- Keep business logic in `Services/`; `MainForm.cs` should only orchestrate UI.
- AI model IDs: use the constants in `Utils/AppConstants.cs`; do not invent model names.
- Config: `config.json` (gitignored) holds API keys locally; `config.json.example` is the
  committed template. Env vars `ANTHROPIC_API_KEY` / `BEDROCK_API_KEY` / `PERPLEXITY_API_KEY`
  also work.
- Nullable warnings CS8600–CS8625 are suppressed project-wide; don't rely on nullability checks.
- `.amazonq/rules/BeBrief.md` asks assistants for brief post-task summaries — honor it.

## Gotchas

- **Secrets on disk:** `config.json`, `MarkConfig.json`, and
  `bin/Release/.../publish/config.json` contain live API keys. They are gitignored (never
  committed) — keep it that way; the `.gitignore` blocks all `*.json` by default for this reason.
  Never print or commit their contents.
- Branch `master` here vs remote default `main`: plain `git push`/PR tooling may target `main`
  by mistake — always push explicitly to `origin master`.
- `docs/` is partially stale (references `Program.cs`, older service lists); trust the code over
  the docs.
- winget operations shell out to `winget.exe` — anything touching `PackageService` is
  untestable off-Windows.

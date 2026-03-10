# Repository Guidelines

## Project Structure & Module Organization
`core/` contains the Rust IME engine. Keep implementation code in `core/src/` and integration-style coverage in `core/tests/`. `platforms/windows/GoMuot/` contains the Windows tray app, with native interop in `Core/`, registry-backed settings in `Services/`, and UI forms in `Views/`. Use `scripts/setup/` for machine bootstrap, `scripts/build/` for packaging, `docs/` for install and runtime notes, and `.github/workflows/` for release automation.

## Build, Test, and Development Commands
Run Rust work from `core/`:

- `cargo build` builds the IME library for the current host.
- `cargo test` runs the Rust test suite in `core/tests/`.
- `cargo test --test integration_test` runs a focused integration pass.

Build the Windows app from the repo root on Windows:

- `powershell -ExecutionPolicy Bypass -File scripts/setup/windows.ps1` installs or verifies Windows prerequisites.
- `powershell -ExecutionPolicy Bypass -File scripts/build/windows.ps1 -Clean` builds the Rust DLL, publishes the WinForms app, and creates `platforms/windows/GoMuot-<version>-win-x64.zip`.
- `bash scripts/build/windows.sh` is a wrapper that forwards to the PowerShell build script when invoked from Git Bash or WSL on Windows.

## Coding Style & Naming Conventions
Follow existing language defaults instead of introducing local variants. Rust uses standard `rustfmt` formatting, snake_case functions, and focused modules such as `engine`, `input`, and `updater`. C# files use four-space indentation, file-scoped namespaces, PascalCase type names, and camelCase private fields with a leading underscore (for example `_hookId`). Keep interop and keyboard-hook changes narrowly scoped and document non-obvious Win32 behavior inline.

## Testing Guidelines
Rust tests are the primary automated safety net. Add coverage in `core/tests/` and use descriptive snake_case names ending in `_test.rs` where that pattern already exists. Prefer targeted regression tests for reported typing bugs. For Windows UI or hook changes, follow the manual checklist in `docs/windows-runtime-checklist.md` and note the environments you verified.

## Commit & Pull Request Guidelines
Recent history favors short, imperative subjects, usually in Conventional Commit form like `fix(windows): use safer Chromium input dispatch`. Keep commits scoped to one concern. PRs should explain user-visible impact, list commands run, link related issues, and include screenshots when changing tray, onboarding, or other Windows UI surfaces. If packaging or signing behavior changes, call that out explicitly.

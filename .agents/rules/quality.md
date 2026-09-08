# Today Wallpaper — Quality & Process Rules

These rules are always active and apply to every change made to the project,
regardless of how small the change is.

---

## Documentation Rule

**After every change that adds, removes, or modifies user-facing behavior,
the `README.md` at the repository root must be reviewed and updated.**

- If the change introduces a new feature, configuration option, or setting: document it.
- If the change alters existing behavior: update the description accordingly.
- If the change is purely internal (refactor, test, CI): README update is not required.

The README must always reflect the current actual capabilities of the app.
It is the primary reference for users discovering the project in the Microsoft Store
or on GitHub. An outdated README is treated as an incomplete change.

Minimum README sections to keep current:

| Section              | What to keep updated                              |
| -------------------- | ------------------------------------------------- |
| Features             | All user-visible capabilities                     |
| Configuration        | All settings (refreshIntervalHours, paletteMode, etc.) |
| Wallpaper Styles     | All available generator styles                    |
| Palette Customization | palettes.json location, format, and constraints  |
| History & Pinning    | How the history works and how to pin wallpapers   |
| Pause / Restore      | How to regain control of the desktop              |
| Requirements         | Windows version, .NET runtime, permissions        |

---

## Code Coverage Rule

**Every change that touches production code in `TodayWallpaper.Core` or
`TodayWallpaper.Worker` must be accompanied by unit tests.**

The project must maintain a minimum of **90% line coverage** across
`TodayWallpaper.Core` and `TodayWallpaper.Worker` at all times.

### Test Project

Tests live in `TodayWallpaper.Tests/` (xUnit).

```
TodayWallpaper.Tests/
├── Weather/         <- Tests for weather fetching & WMO code mapping
├── Palette/         <- Tests for palette loading, validation, jitter
├── Generators/      <- Tests for each IWallpaperGenerator (output sanity checks)
├── History/         <- Tests for ring buffer, pinning, eviction logic
└── Wallpaper/       <- Tests for WallpaperSetter (mocked P/Invoke)
```

### Coverage Command

Run coverage before marking any task as done:

```powershell
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet tool run reportgenerator -reports:coverage/**/coverage.cobertura.xml -targetdir:coverage/report
```

Coverage must be verified against the generated `coverage/report/index.html`.

### Coverage Rules

- **New public class or method** → must have at least one corresponding test.
- **Bug fix** → must include a regression test that fails before the fix and passes after.
- **`TodayWallpaper.App` (WPF)** → UI code is excluded from the 90% requirement,
  but tray menu logic and settings serialization must still be tested.
- **P/Invoke calls** (e.g., `WallpaperSetter`, `GetSystemMetrics`) → must be hidden
  behind an interface and the interface mocked in tests. Never test P/Invoke directly.
- Coverage below 90% blocks the task from being marked complete.

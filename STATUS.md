# Sy-Reality! Status

Last updated: 2026-10-28 (audit re-verification; foundation build session
was 2026-10-23).

## What this is

A fresh rebuild of Franklin's VR karaoke project ("Sy-Reality! a VR Karaoke
Machine, Unity (C#), Meta Quest SDK, Audio Streaming, Meta Quest 3" per his
resume). No prior Sy-Reality code existed to preserve: the two private Unity
repos on his account are untouched Unity 6 starter templates, so this
project was built new in `~/workspace/portfolio/sy-reality/`, honestly
scoped against the resume description.

## Verified

- Pure C# core compiles standalone (.NET 8 SDK, zero UnityEngine
  references): song data model and validation, `LyricSyncEngine`,
  `PlaybackClock` (drift correction), `PerformanceStateMachine`,
  scoring seam (`IPitchScorer`/`NullScorer`, `ParticipationTracker`).
- Headless tests: 30 of 30 passed, re-run 2026-10-28 via `tools/CoreTests`,
  an NUnitLite in-process runner compiling the same sources as the Unity
  EditMode suites (NUnit 3.14, matching Unity's bundled version).
  Exact commands run in this workspace:

  ```bash
  export PATH="$HOME/workspace/tools/dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    NUGET_PACKAGES="$HOME/workspace/tools/nuget" TMPDIR="$HOME/workspace/tools/tmp"
  dotnet build tools/CoreTests
  dotnet tools/CoreTests/bin/Debug/net8.0/CoreTests.dll
  ```

  Result: "Test Count: 30, Passed: 30, Failed: 0". The standard
  `dotnet test`/VSTest path was abandoned in this sandbox: its testhost
  could not start. NUnitLite runs in-process and is reliable. Two real bugs
  were caught by this loop in the foundation session: an unassigned
  variable path in the sync engine's syllable highlight, and a broken drive
  sequence in a state machine test.
- Bundled song JSON structurally validated against the core rules (line
  ordering, syllable bounds within line bounds, syllable text concatenates
  exactly to line text): twinkle-twinkle (6 lines, 36s) and
  original-placeholder (2 lines, 16s) both pass.
- GitHub repo `VulpineCodes/sy-reality` exists (created 2026-10-23). This
  checkout has no remote configured, so whether local commits have been
  pushed was not verified here. Local HEAD before this audit's commit was
  `d412d01`.

## Unverified: everything Unity-side

- The project has never been opened in Unity. Unity is not installed in
  this environment, so: no import, no compile of the Unity layer
  (`SyReality.Unity`, `SyReality.Editor`), no scene build, no play mode, no
  device build. The Unity layer code was written carefully against the
  Unity 6 API but must be treated as uncompiled until first import.
- The identical EditMode test sources have not yet run in Unity's Test
  Framework (they passed headlessly; the in editor run is a Phase 0 exit
  criterion).
- `Packages/manifest.json` versions are initial pins for Unity 6000.3
  (OpenXR 1.14.2, XRI 3.0.8, Input System 1.11.2, TMP 3.0.7, Test
  Framework 1.4.5). Unity may adjust on first resolve; confirm and commit
  the resulting `packages-lock.json` after first import.
- `ProjectSettings/ProjectVersion.txt` pins 6000.3.23f1 (matching Franklin's
  template repos) without a revision hash; Unity will rewrite it on open.
- No Quest 3 build exists. No performance figures, no screenshots, no
  gameplay footage exist. None are claimed anywhere.

## Honest scope vs the resume description

| Resume-era claim | This repo |
| --- | --- |
| Lyric sync | Implemented in core, tested headlessly |
| Audio streaming | Partial. AudioSource playback plus StreamingAssets loading is written in the Unity layer and unverified. No network streaming |
| Live recording | Not implemented, roadmap Phase 3 |
| Multiplayer | Not implemented, roadmap Phase 3 |
| Pitch/vocal scoring | Not implemented, interface seam plus NullScorer only, never faked |

## Environment notes (for whoever continues)

- .NET SDK 8.0.426 is installed user-locally at `~/workspace/tools/dotnet`
  (not on the default PATH in this VM; the commands above add it). It was
  installed during the foundation session. The dotnet-install script prints
  a spurious "Extraction failed" message at the end in this sandbox; the
  SDK works. `NUGET_PACKAGES` was pointed at `~/workspace/tools/nuget` and
  `TMPDIR` at `~/workspace/tools/tmp`.
- Unity and a Quest 3 are required for all Phase 0 exit criteria.

## Next steps

1. Open the project in Unity 6000.3, run **SyReality, Build MainStage
   Scene**, run EditMode tests in the editor, fix any first import issues,
   and commit `packages-lock.json`. Push local commits to
   `VulpineCodes/sy-reality` once the in editor state is confirmed.
2. Play the silent test mode end to end in the editor; verify lyric timing
   against the Twinkle timings.
3. Phase 1 (roadmap): XRI XR Origin rig, Quest 3 build, on device checks.

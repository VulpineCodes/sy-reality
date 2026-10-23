# Sy-Reality! — Status

Last updated: 2026-10-23 (foundation build session).

## What this is

A fresh rebuild of Franklin's VR karaoke project ("Sy-Reality! a VR Karaoke
Machine — Unity (C#), Meta Quest SDK, Audio Streaming, Meta Quest 3" per his
resume). **No prior Sy-Reality code existed to preserve**: the two private
Unity repos on his account are untouched Unity 6 starter templates, so this
project was built new in `~/workspace/portfolio/sy-reality/`, honestly scoped
against the resume description.

## Verified

- **Pure C# core compiles standalone** (.NET 8 SDK, zero UnityEngine
  references): song data model + validation, `LyricSyncEngine`,
  `PlaybackClock` (drift correction), `PerformanceStateMachine`,
  scoring seam (`IPitchScorer`/`NullScorer`, `ParticipationTracker`).
- **Headless tests: 30/30 passed** (2026-10-23) via `tools/CoreTests` —
  an NUnitLite in-process runner compiling the *same* sources as the Unity
  EditMode suites (NUnit 3.14, matching Unity's bundled version).
  Command: `dotnet build tools/CoreTests && dotnet tools/CoreTests/bin/Debug/net8.0/CoreTests.dll`.
  (The standard `dotnet test`/VSTest path was abandoned in this sandbox:
  its testhost could not start; NUnitLite runs in-process and is reliable.
  Two real bugs were caught by this loop: an unassigned-variable path in
  the sync engine's syllable highlight, and a broken drive sequence in a
  state-machine test.)
- **Bundled song JSON structurally validated** against the core rules
  (line ordering, syllable ⊇ line bounds, syllable text concatenates exactly
  to line text): twinkle-twinkle (6 lines, 36s) and original-placeholder
  (2 lines, 16s) both pass.
- Git history (local): scaffold+core → Unity layer → content+tests →
  headless-runner fix. Not pushed (no GitHub repo exists for it yet).

## Unverified — everything Unity-side

- The project has **never been opened in Unity**. Unity is not installed in
  this environment, so: no import, no compile of the Unity layer
  (`SyReality.Unity`, `SyReality.Editor`), no scene build, no play mode, no
  device build. The Unity-layer code was written carefully against the
  Unity 6 API but must be treated as **uncompiled** until first import.
- The identical EditMode test sources have **not** yet run in Unity's Test
  Framework (they passed headlessly; in-editor run is a Phase 0 exit
  criterion).
- `Packages/manifest.json` versions are initial pins for Unity 6000.3
  (OpenXR 1.14.2, XRI 3.0.8, Input System 1.11.2, TMP 3.0.7, Test
  Framework 1.4.5…) — Unity may adjust on first resolve; confirm and commit
  the resulting `packages-lock.json` after first import.
- `ProjectSettings/ProjectVersion.txt` pins 6000.3.23f1 (matching Franklin's
  template repos) without a revision hash; Unity will rewrite it on open.
- No performance figures, no screenshots, no gameplay footage exist. None
  are claimed anywhere.

## Honest scope vs the resume description

| Resume-era claim | This repo |
| --- | --- |
| Lyric sync | ✅ Implemented in core, tested |
| Audio streaming | ◐ AudioSource playback + StreamingAssets loading implemented (Unity layer, unverified); no network streaming |
| Live recording | ❌ Not implemented — roadmap Phase 3 |
| Multiplayer | ❌ Not implemented — roadmap Phase 3 |
| Pitch/vocal scoring | ❌ Not implemented — interface seam + NullScorer only; never faked |

## Environment notes (for whoever continues)

- .NET SDK 8.0.426 was installed user-locally at `~/workspace/tools/dotnet`
  during the foundation session (the dotnet-install script prints a spurious
  "Extraction failed" at the end in this sandbox; the SDK works — NUGET_PACKAGES
  was pointed at `~/workspace/tools/nuget`, TMPDIR at `~/workspace/tools/tmp`).
- Unity and a Quest 3 are required for all Phase 0 exit criteria.

## Next steps

1. Franklin (or a session with Unity): create the GitHub repo
   `sy-reality` (the integration cannot create repos), open the project in
   Unity 6000.3, run **SyReality → Build MainStage Scene**, run EditMode
   tests in-editor, fix any first-import issues, commit `packages-lock.json`.
2. Play the silent test mode end-to-end in the editor; verify lyric timing
   against the Twinkle timings.
3. Phase 1 (roadmap): XRI XR Origin rig, Quest 3 build, on-device checks.

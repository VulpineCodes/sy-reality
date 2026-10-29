# MainStage scene

The scene is **generated from code**. Do not hand-edit binary scene YAML.
To (re)build it: Unity menu **SyReality → Build MainStage Scene**
(`Assets/SyReality/Editor/SceneBuilder.cs`). It creates
`Assets/Scenes/MainStage.unity`, the song-button prefab, and the three
environment profile assets under `Assets/SyReality/Data/`.

## Composition the builder produces

| Object | Components / children | Purpose |
| --- | --- | --- |
| `Key Light` | Directional Light | Retuned per environment profile by EnvironmentDirector |
| `XR Rig (Placeholder)` | `Camera Offset` → `Main Camera` (tagged MainCamera, AudioListener), `Left/Right Controller` cubes | Stand-in rig. Replace with XRI's **XR Origin (XR Rig)** prefab for device work; everything else is rig-agnostic |
| `Stages` | `Stage_NeonClub`, `Stage_SunsetRooftop`, `Stage_CosmicStage`, each with `Stage Floor`, `Backdrop`, `Particles_<preset>` | One root per environment; EnvironmentDirector activates exactly one |
| `Managers` | PerformanceManager; child `AudioEngine` (+AudioSource); child `EnvironmentDirector` | Session flow, drift-corrected clock, audio, environments |
| `Lyric Canvas` | World-space Canvas (900×420 at z≈3.2) with LyricDisplay, `Previous/Current/Next Line` TMP texts, Progress Bar (Filled Image), child `Song Library Panel` → `Song List` (VerticalLayoutGroup) + SongLibraryUI | Karaoke readout; readability-sized type (current line 64pt in canvas units) |

## Wiring the builder performs (verify after build)

- `PerformanceManager.audioEngine / lyricDisplay / songLibraryUI` → the built objects.
- `SongLibraryUI.performanceManager / listRoot / panelRoot / songButtonPrefab` → manager, `Song List`, panel, generated prefab.
- `EnvironmentDirector.stagesRoot / keyLight / profiles[3]` → `Stages`, `Key Light`, the three generated `Environment_Stage_*.asset` profiles.
- `LyricDisplay.previousLineText / currentLineText / nextLineText / progressBar` → canvas texts + fill image.

## Manual checklist after first import (~10 min)

1. If TMP texts show missing-font boxes, assign a TextMeshPro font asset
   (Window → TextMeshPro → Font Asset Creator, or the default LiberationSans
   SDF included with TMP Essentials) to the four TMP components.
2. Import TMP Essentials and XR Interaction Toolkit samples if prompted.
3. Project Settings → XR Plug-in Management: enable **OpenXR** for Android;
   set the OpenXR interaction profile to Meta Quest Touch. Confirm the
   rendering pipeline choice (built-in RP is the current scaffold default;
   migrating to URP is a roadmap decision, see docs/ROADMAP.md).
4. Press Play in the editor: state starts at Lobby → call
   `PerformanceManager.OpenSongSelect()` from the Song Library flow (or the
   Inspector context) → pick *Twinkle Twinkle Little Star* → countdown →
   lyrics advance in silent test mode (no audio file bundled by design).

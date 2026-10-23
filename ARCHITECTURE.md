# Sy-Reality! — Architecture

## Design principle

Everything deterministic lives in a **pure C# core with zero UnityEngine
dependencies** (`SyReality.Core`, `noEngineReferences: true`). The Unity layer
is a thin binding: it renders what the core decides and forwards intents. This
is what makes the karaoke timing logic unit-testable headlessly (plain .NET)
and in Unity's Test Framework with the *same* test sources.

```mermaid
flowchart LR
    subgraph Core["SyReality.Core (pure C#, no UnityEngine)"]
        SD[SongDefinition / LyriRiver / SyllableCue]
        LSE[LyricSyncEngine]
        PC[PlaybackClock<br/>ITimeSource injected]
        SM[PerformanceStateMachine]
        SC[IPitchScorer / NullScorer<br/>ParticipationTracker]
    end
    subgraph UnityLayer["SyReality.Unity (MonoBehaviours)"]
        PM[PerformanceManager]
        AE[AudioEngine<br/>AudioSource wrapper]
        LD[LyricDisplay<br/>TextMeshPro]
        SLU[SongLibraryUI]
        ED[EnvironmentDirector<br/>+ EnvironmentProfile assets]
        SJL[SongLibraryLoader<br/>JsonUtility DTOs]
    end
    JSON[(Resources/Songs/*.json)] --> SJL --> SD
    SD --> LSE
    PM --> SM
    PM --> PC
    PM --> LSE
    AE -->|actual audio time| PC
    PC -->|position| LSE
    LSE -->|LyricFrame| LD
    SLU -->|SelectSong intent| PM
    ED --> PM
```

## Module map

| Module | Responsibility |
| --- | --- |
| `Core/SongData.cs` | Data model + structural validation (ordered, non-overlapping lines/syllables, within duration, rights note carried on every song) |
| `Core/LyricSyncEngine.cs` | Stateless function of time → `LyricFrame` (active line, progress, next line, syllable + char-level highlight). Binary search over lines |
| `Core/PlaybackClock.cs` | Position with play/pause/resume/restart/seek over an injectable `ITimeSource`; drift measurement + threshold snap-correction against reported audio time |
| `Core/PerformanceStateMachine.cs` | Explicit transition table; invalid transitions rejected, logged, and surfaced as events |
| `Core/Scoring.cs` | Optional-extension seam: `IPitchScorer`, honest `NullScorer`, and timing-based `ParticipationTracker` (line completion — never presented as vocal quality) |
| `Unity/PerformanceManager.cs` | Owns core objects; intents (`OpenSongSelect`, `SelectSong`, `PausePerformance`, …) are the only way flow changes; per-frame drift feed + frame render |
| `Unity/AudioEngine.cs` | AudioSource wrapper; loads user-licensed files from StreamingAssets/Audio; silent test mode when no clip |
| `Unity/LyricDisplay.cs` | TMP rendering: previous/current/next lines, rich-text highlight split at the engine's char count, progress bar |
| `Unity/EnvironmentDirector.cs` | Applies `EnvironmentProfile` ScriptableObjects (3 stages: Neon Club, Sunset Rooftop, Cosmic Stage) — environments as data |
| `Editor/SceneBuilder.cs` | Generates MainStage scene + profiles + prefab from code; the scene is reproducible from the repo |

## Timing & synchronization design

Karaoke sync fails in two ways: the *clock* being wrong, and the *lookup*
being wrong. They are solved separately:

1. **Lookup** — `LyricSyncEngine` is stateless: frame = f(song, t). Seek,
   pause, and restart need no special cases; there is no accumulated state to
   desynchronize.
2. **Clock** — `PlaybackClock` advances from an injected time source, but the
   **audio is the audible truth**. Each frame during Performing, the manager
   reports the AudioSource's actual `time`; drift beyond 0.15s snaps the
   clock base to the audio position and increments an observable correction
   counter. MP3 encoder delay and device audio latency show up as drift and
   are absorbed by the same mechanism.
3. **Countdown honesty** — the performance clock starts at 0 in the same frame
   audio playback starts (after the countdown coroutine), so the countdown can
   never eat into song time.

## Performance state diagram

```mermaid
stateDiagram-v2
    Lobby --> SongSelect
    SongSelect --> Lobby
    SongSelect --> Countdown
    Countdown --> Performing
    Countdown --> SongSelect
    Performing --> Paused
    Paused --> Performing
    Paused --> SongSelect
    Paused --> Lobby
    Performing --> Results
    Results --> Lobby
    Results --> SongSelect
    Results --> Countdown
```

Any transition not drawn here is rejected by the state machine (with a log
line and a `TransitionRejected` event the UI can surface).

## Testing strategy

- `Assets/SyReality/Tests/EditMode/` — NUnit suites for the sync engine
  (boundaries, gaps, seek, syllable highlight), the clock (pause freeze, seek,
  drift threshold + snap), the state machine (happy path + invalid
  transitions), and scoring honesty (NullScorer never fabricates).
- `tools/CoreTests/` — a plain .NET test project that compiles the **same**
  core + test sources, so the logic can be verified on any machine with the
  .NET SDK, including CI without a Unity license. STATUS.md records which
  runner produced which results.

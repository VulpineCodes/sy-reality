# Sy-Reality! — Roadmap

Honest sequencing: a complete vertical slice first, polish second, optional
extensions last. Nothing below is claimed done until STATUS.md says it was
verified (in Unity, on hardware where relevant).

## Phase 0 — Foundation (this repository state)
- Pure C# core: song data model + validation, lyric sync engine, playback
  clock with drift correction, performance state machine, scoring seam.
- Unity layer bindings, code-built MainStage scene, one public-domain test
  song + one original placeholder, EditMode test suites.
- **Exit criteria:** project imported in Unity 6000.3, scene builds, tests run
  in the Test Runner, silent test mode performs Twinkle end-to-end in the
  editor. *(Pending — needs a machine with Unity; see STATUS.md.)*

## Phase 1 — Vertical slice on Quest 3
- Replace the placeholder rig with XR Interaction Toolkit's XR Origin;
  OpenXR + Meta Quest Touch profile; controller-driven song select,
  pause/resume via XRI actions calling PerformanceManager intents.
- On-device lyric readability pass (text size, contrast, canvas distance),
  comfort: stationary stage, no artificial locomotion, fixed horizon.
- One environment (Neon Club) fully dressed: stage geometry, lighting,
  particles, spatial audio for UI feedback.
- **Exit criteria:** complete Lobby → SongSelect → Countdown → Performing →
  Paused → Results loop on a physical Quest 3, recorded honestly with any
  frame-rate notes from the Unity Profiler.

## Phase 2 — Environments & polish
- Sunset Rooftop and Cosmic Stage dressed to the same bar.
- Environment switching from SongSelect; per-song environment preference.
- Audience silhouettes / reactive lighting driven by participation state
  (visual feedback only — no fake "crowd reaction to your singing quality").
- Song library growth with original/licensed content only; a small authoring
  guide for timing lyrics.

## Phase 3 — Optional extensions (each gated on feasibility evidence)
- **Microphone input + pitch detection:** Quest mic permission, FFT /
  autocorrelation pitch tracker behind `IPitchScorer`; accuracy validated
  against known tones before any score is shown to users.
- **Live recording:** local capture of a performance (audio + results card);
  storage/privacy design first.
- **Multiplayer:** the resume-era concept. Requires a networking decision
  (e.g. NGO/Photon), identity, and moderation thought — deliberately last.
- **URP migration** if profiling on device justifies it.

## Non-goals
- Distributing copyrighted recordings or lyrics. Ever.
- Claiming pitch accuracy, performance figures, or features that have not
  been implemented and measured.

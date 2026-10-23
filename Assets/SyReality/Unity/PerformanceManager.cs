using System.Collections;
using SyReality.Core;
using UnityEngine;

namespace SyReality.Unity
{
    /// <summary>
    /// Binds the pure core to the engine: owns the state machine, playback clock,
    /// sync engine and participation tracker; drives the audio engine and the
    /// lyric display. All flow changes go through intent methods (OpenSongSelect,
    /// SelectSong, …) so invalid transitions are rejected by the core state
    /// machine rather than corrupting the session.
    ///
    /// XR note: this manager is input-agnostic. XR Interaction Toolkit actions or
    /// controller buttons call the same intent methods as the debug keyboard
    /// shortcuts in Update() — wire XRI select/activate actions to them in the
    /// rig prefab.
    /// </summary>
    public sealed class PerformanceManager : MonoBehaviour
    {
        [SerializeField] private AudioEngine audioEngine;
        [SerializeField] private LyricDisplay lyricDisplay;
        [SerializeField] private SongLibraryUI songLibraryUI;
        [SerializeField] private float countdownSeconds = 3f;

        public PerformanceStateMachine StateMachine { get; private set; }
        public PlaybackClock Clock { get; private set; }
        public LyricSyncEngine Sync { get; private set; }
        public ParticipationTracker Participation { get; private set; }
        public SongDefinition CurrentSong { get; private set; }

        /// <summary>Pitch scoring seam — NullScorer until a real detector exists.</summary>
        public IPitchScorer PitchScorer { get; set; } = new NullScorer();

        private int _lastCompletedLine = -1;

        private void Awake()
        {
            StateMachine = new PerformanceStateMachine(msg => Debug.Log(msg));
            Clock = new PlaybackClock(new UnityTimeSource());
            StateMachine.StateChanged += OnStateChanged;
        }

        // ---- Intents (call these from UI / XR actions) ----

        public void OpenSongSelect()
        {
            if (StateMachine.TryTransition(PerformanceState.SongSelect))
                songLibraryUI?.Show();
        }

        public void BackToLobby()
        {
            StateMachine.TryTransition(PerformanceState.Lobby);
        }

        public void SelectSong(SongDefinition song)
        {
            if (song == null) return;
            if (StateMachine.State != PerformanceState.SongSelect) return;
            CurrentSong = song;
            Sync = new LyricSyncEngine(song);
            Participation = new ParticipationTracker(song.Lines.Count);
            _lastCompletedLine = -1;
            songLibraryUI?.Hide();
            if (StateMachine.TryTransition(PerformanceState.Countdown))
                StartCoroutine(CountdownThenPerform());
        }

        public void PausePerformance()
        {
            if (!StateMachine.TryTransition(PerformanceState.Paused)) return;
            Clock.Pause();
            audioEngine?.Pause();
        }

        public void ResumePerformance()
        {
            if (!StateMachine.TryTransition(PerformanceState.Performing)) return;
            Clock.Resume();
            audioEngine?.Resume();
        }

        public void EndPerformanceEarly()
        {
            if (StateMachine.State == PerformanceState.Performing ||
                StateMachine.State == PerformanceState.Paused)
                FinishPerformance();
        }

        public void QuitToSongSelect()
        {
            StopPerformanceAudio();
            if (StateMachine.TryTransition(PerformanceState.SongSelect))
                songLibraryUI?.Show();
        }

        public void SingAgain()
        {
            if (CurrentSong == null) return;
            if (StateMachine.TryTransition(PerformanceState.Countdown))
            {
                Participation = new ParticipationTracker(CurrentSong.Lines.Count);
                _lastCompletedLine = -1;
                StartCoroutine(CountdownThenPerform());
            }
        }

        // ---- Internals ----

        private IEnumerator CountdownThenPerform()
        {
            yield return StartCoroutine(
                audioEngine.LoadFromStreamingAssets(CurrentSong.AudioFileName));
            // Countdown is wall-clock UI time; the performance clock starts at 0
            // exactly when audio starts, which is what keeps sync honest.
            yield return new WaitForSeconds(countdownSeconds);
            if (StateMachine.State != PerformanceState.Countdown) yield break; // cancelled
            if (!StateMachine.TryTransition(PerformanceState.Performing)) yield break;
            Clock.Play(0);
            audioEngine?.Play(0);
        }

        private void Update()
        {
            if (StateMachine.State != PerformanceState.Performing || Sync == null) return;

            // Drift correction: audio time is the audible truth.
            if (audioEngine != null && audioEngine.HasClip && audioEngine.IsPlaying)
                Clock.ReportAudioTime(audioEngine.AudioTimeSeconds);

            double t = Clock.PositionSeconds;
            var frame = Sync.GetFrame(t);

            // Participation: a line counts as completed when playback passes its end.
            if (_lastCompletedLine < frame.LineIndex - 1)
            {
                for (int i = _lastCompletedLine + 1; i < frame.LineIndex; i++)
                    Participation?.MarkLineCompleted(i);
                _lastCompletedLine = frame.LineIndex - 1;
            }

            string previous = frame.LineIndex > 0
                ? CurrentSong.Lines[frame.LineIndex - 1].Text
                : string.Empty;
            lyricDisplay?.Show(frame, previous);

            bool audioDone = audioEngine != null && audioEngine.HasClip &&
                              !audioEngine.IsPlaying && t > 1.0;
            if (t >= CurrentSong.DurationSeconds || audioDone)
                FinishPerformance();

            // Debug / desktop fallback controls (XR actions call the same intents).
            if (Input.GetKeyDown(KeyCode.P)) PausePerformance();
        }

        private void FinishPerformance()
        {
            // The song ran its course (or ended early from pause): every line the
            // playback position passed counts toward participation completion.
            if (Participation != null && CurrentSong != null)
            {
                double t = Clock.PositionSeconds;
                for (int i = _lastCompletedLine + 1; i < CurrentSong.Lines.Count; i++)
                    if (t >= CurrentSong.Lines[i].EndSeconds)
                        Participation.MarkLineCompleted(i);
            }
            StopPerformanceAudio();
            Clock.Stop();
            lyricDisplay?.Clear();
            StateMachine.TryTransition(PerformanceState.Results);
            var pitch = PitchScorer.ScoreLine(
                CurrentSong.Lines[CurrentSong.Lines.Count - 1],
                System.Array.Empty<float>());
            Debug.Log($"[SyReality] Performance finished. Lines completed: " +
                      $"{Participation?.CompletedLineCount}/{Participation?.TotalLines} " +
                      $"({Participation?.CompletionPercent}%). Pitch: {pitch.Note}");
        }

        private void StopPerformanceAudio() => audioEngine?.Stop();

        private void OnStateChanged(PerformanceState from, PerformanceState to)
        {
            if (to == PerformanceState.Lobby)
            {
                StopPerformanceAudio();
                Clock.Stop();
                lyricDisplay?.Clear();
            }
        }
    }
}

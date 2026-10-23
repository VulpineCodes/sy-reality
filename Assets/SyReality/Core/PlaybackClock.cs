using System;

namespace SyReality.Core
{
    /// <summary>Injectable time source so the clock is deterministic in tests.</summary>
    public interface ITimeSource
    {
        double NowSeconds { get; }
    }

    /// <summary>Test/manual time source: time only moves when Advance/Set is called.</summary>
    public sealed class ManualTimeSource : ITimeSource
    {
        public double NowSeconds { get; private set; }
        public ManualTimeSource(double startSeconds = 0) => NowSeconds = startSeconds;
        public void Advance(double deltaSeconds) => NowSeconds += deltaSeconds;
        public void Set(double seconds) => NowSeconds = seconds;
    }

    public enum ClockState { Stopped, Playing, Paused }

    /// <summary>
    /// Playback position clock with play / pause / resume / restart / seek.
    ///
    /// Design: position = basePosition + (now - playStartedAt) while Playing; frozen
    /// at basePosition otherwise. The Unity layer feeds the AudioSource's reported
    /// time into <see cref="ReportAudioTime"/>; when the drift exceeds the threshold
    /// the clock snaps its base to the audio time (audio is the audible truth) and
    /// counts the correction so drift behaviour is observable and testable.
    /// </summary>
    public sealed class PlaybackClock
    {
        private readonly ITimeSource _time;
        private double _basePosition;
        private double _playStartedAt;

        public ClockState State { get; private set; } = ClockState.Stopped;
        public double DriftThresholdSeconds { get; set; } = 0.15;
        public bool AutoCorrectDrift { get; set; } = true;
        /// <summary>Most recent (clock position − audio position), seconds.</summary>
        public double LastDriftSeconds { get; private set; }
        public int DriftCorrectionCount { get; private set; }

        public PlaybackClock(ITimeSource timeSource)
        {
            _time = timeSource ?? throw new ArgumentNullException(nameof(timeSource));
        }

        public double PositionSeconds =>
            State == ClockState.Playing
                ? _basePosition + (_time.NowSeconds - _playStartedAt)
                : _basePosition;

        public void Play(double fromSeconds = 0)
        {
            _basePosition = Math.Max(0, fromSeconds);
            _playStartedAt = _time.NowSeconds;
            State = ClockState.Playing;
        }

        public void Pause()
        {
            if (State != ClockState.Playing) return;
            _basePosition = PositionSeconds; // freeze
            State = ClockState.Paused;
        }

        public void Resume()
        {
            if (State != ClockState.Paused) return;
            _playStartedAt = _time.NowSeconds;
            State = ClockState.Playing;
        }

        public void Stop()
        {
            _basePosition = 0;
            State = ClockState.Stopped;
        }

        public void Restart() => Play(0);

        public void Seek(double positionSeconds)
        {
            _basePosition = Math.Max(0, positionSeconds);
            if (State == ClockState.Playing) _playStartedAt = _time.NowSeconds;
        }

        /// <summary>
        /// Report the audio engine's actual playback position. Returns the measured
        /// drift. When AutoCorrectDrift is on and drift exceeds the threshold, the
        /// clock re-bases onto the audio position.
        /// </summary>
        public double ReportAudioTime(double audioPositionSeconds)
        {
            double drift = PositionSeconds - audioPositionSeconds;
            LastDriftSeconds = drift;
            if (AutoCorrectDrift && State == ClockState.Playing &&
                Math.Abs(drift) > DriftThresholdSeconds)
            {
                _basePosition = Math.Max(0, audioPositionSeconds);
                _playStartedAt = _time.NowSeconds;
                DriftCorrectionCount++;
            }
            return drift;
        }
    }
}

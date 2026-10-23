using NUnit.Framework;
using SyReality.Core;

namespace SyReality.Tests
{
    public sealed class PlaybackClockTests
    {
        private ManualTimeSource _time;
        private PlaybackClock _clock;

        [SetUp]
        public void SetUp()
        {
            _time = new ManualTimeSource(100); // arbitrary epoch
            _clock = new PlaybackClock(_time);
        }

        [Test]
        public void Play_AdvancesWithTimeSource()
        {
            _clock.Play();
            _time.Advance(3.5);
            Assert.AreEqual(3.5, _clock.PositionSeconds, 1e-9);
            Assert.AreEqual(ClockState.Playing, _clock.State);
        }

        [Test]
        public void Pause_FreezesPosition_ResumeContinues()
        {
            _clock.Play();
            _time.Advance(2);
            _clock.Pause();
            _time.Advance(10);
            Assert.AreEqual(2.0, _clock.PositionSeconds, 1e-9);
            _clock.Resume();
            _time.Advance(1);
            Assert.AreEqual(3.0, _clock.PositionSeconds, 1e-9);
        }

        [Test]
        public void Seek_WhilePlaying_ContinuesFromNewPosition()
        {
            _clock.Play();
            _time.Advance(2);
            _clock.Seek(20);
            Assert.AreEqual(20.0, _clock.PositionSeconds, 1e-9);
            _time.Advance(1);
            Assert.AreEqual(21.0, _clock.PositionSeconds, 1e-9);
        }

        [Test]
        public void Seek_WhilePaused_StaysFrozenAtSeekPosition()
        {
            _clock.Play();
            _time.Advance(2);
            _clock.Pause();
            _clock.Seek(7);
            _time.Advance(5);
            Assert.AreEqual(7.0, _clock.PositionSeconds, 1e-9);
        }

        [Test]
        public void Restart_ReturnsToZeroAndPlays()
        {
            _clock.Play(30);
            _time.Advance(5);
            _clock.Restart();
            Assert.AreEqual(0.0, _clock.PositionSeconds, 1e-9);
            _time.Advance(1);
            Assert.AreEqual(1.0, _clock.PositionSeconds, 1e-9);
        }

        [Test]
        public void Drift_WithinThreshold_IsReportedNotCorrected()
        {
            _clock.Play();
            _time.Advance(10);
            double drift = _clock.ReportAudioTime(9.95); // 0.05s drift < 0.15 threshold
            Assert.AreEqual(0.05, drift, 1e-9);
            Assert.AreEqual(0, _clock.DriftCorrectionCount);
            Assert.AreEqual(10.0, _clock.PositionSeconds, 1e-9);
        }

        [Test]
        public void Drift_BeyondThreshold_SnapsToAudioTime()
        {
            _clock.Play();
            _time.Advance(10);
            _clock.ReportAudioTime(9.5); // 0.5s drift > threshold
            Assert.AreEqual(1, _clock.DriftCorrectionCount);
            Assert.AreEqual(9.5, _clock.PositionSeconds, 1e-9);
            _time.Advance(1);
            Assert.AreEqual(10.5, _clock.PositionSeconds, 1e-9);
        }

        [Test]
        public void Drift_AutoCorrectDisabled_NeverSnaps()
        {
            _clock.AutoCorrectDrift = false;
            _clock.Play();
            _time.Advance(10);
            _clock.ReportAudioTime(5);
            Assert.AreEqual(0, _clock.DriftCorrectionCount);
            Assert.AreEqual(10.0, _clock.PositionSeconds, 1e-9);
        }
    }
}

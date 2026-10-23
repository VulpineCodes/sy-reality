using System.Collections.Generic;
using NUnit.Framework;
using SyReality.Core;

namespace SyReality.Tests
{
    public sealed class PerformanceStateMachineTests
    {
        private PerformanceStateMachine _sm;
        private List<string> _rejections;

        [SetUp]
        public void SetUp()
        {
            _rejections = new List<string>();
            _sm = new PerformanceStateMachine();
            _sm.TransitionRejected += (from, to) => _rejections.Add($"{from}->{to}");
        }

        private void DriveToPerforming()
        {
            Assert.IsTrue(_sm.TryTransition(PerformanceState.SongSelect));
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Countdown));
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Performing));
        }

        [Test]
        public void HappyPath_LobbyToResultsAndBack()
        {
            DriveToPerforming();
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Paused));
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Performing));
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Results));
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Lobby));
            Assert.AreEqual(PerformanceState.Lobby, _sm.State);
            Assert.IsEmpty(_rejections);
        }

        [Test]
        public void InvalidTransition_IsRejectedAndLogged_StateUnchanged()
        {
            Assert.IsFalse(_sm.TryTransition(PerformanceState.Performing)); // Lobby -/-> Performing
            Assert.AreEqual(PerformanceState.Lobby, _sm.State);
            Assert.AreEqual(new[] { "Lobby->Performing" }, _rejections);
        }

        [Test]
        public void CannotPauseOutsidePerformance()
        {
            Assert.IsFalse(_sm.TryTransition(PerformanceState.Paused));
            _sm.TryTransition(PerformanceState.SongSelect);
            Assert.IsFalse(_sm.TryTransition(PerformanceState.Paused));
            Assert.AreEqual(PerformanceState.SongSelect, _sm.State);
        }

        [Test]
        public void CountdownCancel_ReturnsToSongSelect()
        {
            _sm.TryTransition(PerformanceState.SongSelect);
            _sm.TryTransition(PerformanceState.Countdown);
            Assert.IsTrue(_sm.TryTransition(PerformanceState.SongSelect));
        }

        [Test]
        public void QuitFromPause_ToSongSelectOrLobby()
        {
            DriveToPerforming();
            _sm.TryTransition(PerformanceState.Paused);
            Assert.IsTrue(_sm.TryTransition(PerformanceState.SongSelect));
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Lobby));

            DriveToPerforming();
            _sm.TryTransition(PerformanceState.Paused);
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Lobby));
        }

        [Test]
        public void SingAgain_FromResults_GoesToCountdown()
        {
            DriveToPerforming();
            _sm.TryTransition(PerformanceState.Results);
            Assert.IsTrue(_sm.TryTransition(PerformanceState.Countdown));
        }

        [Test]
        public void StateChanged_EventFiresWithFromAndTo()
        {
            PerformanceState? seenFrom = null, seenTo = null;
            _sm.StateChanged += (f, t) => { seenFrom = f; seenTo = t; };
            _sm.TryTransition(PerformanceState.SongSelect);
            Assert.AreEqual(PerformanceState.Lobby, seenFrom);
            Assert.AreEqual(PerformanceState.SongSelect, seenTo);
        }
    }
}

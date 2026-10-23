using System;
using System.Collections.Generic;

namespace SyReality.Core
{
    public enum PerformanceState
    {
        Lobby,
        SongSelect,
        Countdown,
        Performing,
        Paused,
        Results
    }

    /// <summary>
    /// Explicit state machine for the performance flow:
    ///
    ///   Lobby → SongSelect → Countdown → Performing ⇄ Paused → Results → Lobby
    ///
    /// Every allowed transition is in the table below; anything else is rejected
    /// (returns false, raises <see cref="TransitionRejected"/>, and is logged).
    /// The Unity layer drives this via PerformanceManager intents; UI never sets
    /// state directly.
    /// </summary>
    public sealed class PerformanceStateMachine
    {
        private static readonly IReadOnlyDictionary<PerformanceState, PerformanceState[]> Allowed =
            new Dictionary<PerformanceState, PerformanceState[]>
            {
                [PerformanceState.Lobby] = new[] { PerformanceState.SongSelect },
                [PerformanceState.SongSelect] = new[]
                {
                    PerformanceState.Lobby,      // back
                    PerformanceState.Countdown,  // start selected song
                },
                [PerformanceState.Countdown] = new[]
                {
                    PerformanceState.Performing, // countdown finished
                    PerformanceState.SongSelect, // cancelled
                },
                [PerformanceState.Performing] = new[]
                {
                    PerformanceState.Paused,
                    PerformanceState.Results,    // song finished / ended early
                },
                [PerformanceState.Paused] = new[]
                {
                    PerformanceState.Performing, // resume
                    PerformanceState.SongSelect, // quit to song select
                    PerformanceState.Lobby,      // quit to lobby
                },
                [PerformanceState.Results] = new[]
                {
                    PerformanceState.Lobby,      // return
                    PerformanceState.SongSelect, // choose another
                    PerformanceState.Countdown,  // sing it again
                },
            };

        private readonly Action<string> _log;

        public PerformanceState State { get; private set; } = PerformanceState.Lobby;

        public event Action<PerformanceState, PerformanceState> StateChanged;
        public event Action<PerformanceState, PerformanceState> TransitionRejected;

        public PerformanceStateMachine(Action<string> log = null)
        {
            _log = log ?? (_ => { });
        }

        public bool CanTransitionTo(PerformanceState to) =>
            Array.IndexOf(Allowed[State], to) >= 0;

        public bool TryTransition(PerformanceState to)
        {
            if (!CanTransitionTo(to))
            {
                _log($"[StateMachine] Rejected transition {State} -> {to}.");
                TransitionRejected?.Invoke(State, to);
                return false;
            }
            var from = State;
            State = to;
            _log($"[StateMachine] {from} -> {to}.");
            StateChanged?.Invoke(from, to);
            return true;
        }
    }
}

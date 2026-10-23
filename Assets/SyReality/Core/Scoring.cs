using System;
using System.Collections.Generic;

namespace SyReality.Core
{
    // OPTIONAL EXTENSION — pitch scoring.
    //
    // Sy-Reality! does NOT score pitch today. This module defines the seam where a
    // real pitch detector (FFT/autocorrelation over microphone input) would plug
    // in, plus an honest NullScorer so the rest of the system can be built and
    // tested against the interface without pretending accuracy that does not exist.
    // Do not present results from NullScorer as singing scores.

    public readonly struct PitchScoreResult
    {
        /// <summary>False when no real scoring was performed.</summary>
        public bool Available { get; }
        /// <summary>0..100 when Available; meaningless otherwise.</summary>
        public double Score { get; }
        public string Note { get; }

        public PitchScoreResult(bool available, double score, string note)
        {
            Available = available;
            Score = score;
            Note = note ?? string.Empty;
        }
    }

    public interface IPitchScorer
    {
        /// <summary>Score one performed line from recorded pitch samples (Hz;
        /// 0 or negative = unvoiced). Implementations must set Available=false
        /// when the samples are insufficient rather than guessing.</summary>
        PitchScoreResult ScoreLine(LyriRiver line, IReadOnlyList<float> pitchSamplesHz);
    }

    /// <summary>Placeholder that never fabricates a score.</summary>
    public sealed class NullScorer : IPitchScorer
    {
        public PitchScoreResult ScoreLine(LyriRiver line, IReadOnlyList<float> pitchSamplesHz) =>
            new PitchScoreResult(false, 0,
                "Pitch scoring is not enabled in this build (roadmap extension).");
    }

    /// <summary>
    /// Timing-based participation feedback — NOT vocal scoring. Tracks which lines
    /// were actively performed through (the Unity layer marks a line as sung when
    /// the performer stays in the performance until the line completes, and can
    /// later require microphone energy where available). Results are framed as
    /// "lines completed", never as singing quality.
    /// </summary>
    public sealed class ParticipationTracker
    {
        private readonly HashSet<int> _completedLines = new HashSet<int>();
        private readonly int _totalLines;

        public ParticipationTracker(int totalLines)
        {
            if (totalLines <= 0) throw new ArgumentOutOfRangeException(nameof(totalLines));
            _totalLines = totalLines;
        }

        public void MarkLineCompleted(int lineIndex)
        {
            if (lineIndex >= 0 && lineIndex < _totalLines) _completedLines.Add(lineIndex);
        }

        public int CompletedLineCount => _completedLines.Count;
        public int TotalLines => _totalLines;
        public double CompletionRatio =>
            _totalLines == 0 ? 0 : (double)_completedLines.Count / _totalLines;
        public int CompletionPercent => (int)Math.Round(CompletionRatio * 100);
    }
}

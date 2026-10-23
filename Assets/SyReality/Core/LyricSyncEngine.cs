using System;

namespace SyReality.Core
{
    /// <summary>One rendered frame of lyric state at a given playback time.</summary>
    public readonly struct LyricFrame
    {
        /// <summary>Index of the active line, or -1 when no line is active
        /// (intro, instrumental gap, or after the last line).</summary>
        public int LineIndex { get; }
        public LyriRiver CurrentLine { get; }
        public LyriRiver NextLine { get; }
        /// <summary>0..1 progress through the active line; 0 when no line is active.</summary>
        public double LineProgress { get; }
        /// <summary>Index of the highlighted syllable within the current line,
        /// or -1 when the line has no syllable cues or none is active.</summary>
        public int SyllableIndex { get; }
        /// <summary>Character offset within the current line's text up to which the
        /// highlight has reached (syllable-based when cues exist, otherwise a linear
        /// interpolation across the line, so highlighting still works without cues).</summary>
        public int HighlightCharCount { get; }
        public bool IsLineActive => LineIndex >= 0;

        public LyricFrame(int lineIndex, LyriRiver current, LyriRiver next,
            double lineProgress, int syllableIndex, int highlightCharCount)
        {
            LineIndex = lineIndex;
            CurrentLine = current;
            NextLine = next;
            LineProgress = lineProgress;
            SyllableIndex = syllableIndex;
            HighlightCharCount = highlightCharCount;
        }

        public static readonly LyricFrame Inactive = new LyricFrame(-1, null, null, 0, -1, 0);
    }

    /// <summary>
    /// Pure, stateless lyric synchronization. Given a playback time (from
    /// <see cref="PlaybackClock"/>, already drift-corrected), returns exactly which
    /// line/syllable should be shown. Being stateless makes seek and pause trivially
    /// correct: the frame is a function of time only.
    /// </summary>
    public sealed class LyricSyncEngine
    {
        private readonly SongDefinition _song;

        public LyricSyncEngine(SongDefinition song)
        {
            _song = song ?? throw new ArgumentNullException(nameof(song));
            if (!_song.TryValidate(out string error))
                throw new ArgumentException($"Invalid song definition: {error}", nameof(song));
        }

        public SongDefinition Song => _song;

        public LyricFrame GetFrame(double timeSeconds)
        {
            var lines = _song.Lines;
            // Binary search for the last line whose start <= time.
            int lo = 0, hi = lines.Count - 1, candidate = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (lines[mid].StartSeconds <= timeSeconds) { candidate = mid; lo = mid + 1; }
                else hi = mid - 1;
            }

            if (candidate < 0)
                return new LyricFrame(-1, null, lines.Count > 0 ? lines[0] : null, 0, -1, 0);

            var line = lines[candidate];
            var next = candidate + 1 < lines.Count ? lines[candidate + 1] : null;

            if (timeSeconds >= line.EndSeconds)
                // Inside a gap (instrumental) or past the end: no active line,
                // but still surface what comes next.
                return new LyricFrame(-1, null, next, 0, -1, 0);

            double progress = line.DurationSeconds <= 0
                ? 1.0
                : (timeSeconds - line.StartSeconds) / line.DurationSeconds;
            progress = Math.Min(1.0, Math.Max(0.0, progress));

            int syllableIndex = -1;
            int highlightChars;
            if (line.HasSyllableCues)
            {
                // Highlight everything up to and including the active syllable;
                // before the first cue / in a cue gap, only fully-sung syllables.
                int sungChars = 0;
                int activeEnd = -1;
                for (int i = 0; i < line.Syllables.Count; i++)
                {
                    var cue = line.Syllables[i];
                    if (timeSeconds >= cue.StartSeconds && timeSeconds < cue.EndSeconds)
                    {
                        syllableIndex = i;
                        activeEnd = sungChars + cue.Text.Length;
                        break;
                    }
                    if (timeSeconds >= cue.EndSeconds) sungChars += cue.Text.Length;
                }
                highlightChars = Math.Min(
                    activeEnd >= 0 ? activeEnd : sungChars, line.Text.Length);
            }
            else
            {
                highlightChars = (int)Math.Round(progress * line.Text.Length);
            }

            return new LyricFrame(candidate, line, next, progress, syllableIndex, highlightChars);
        }

        /// <summary>Index of the line active at <paramref name="timeSeconds"/>, or -1.</summary>
        public int LineIndexAt(double timeSeconds) => GetFrame(timeSeconds).LineIndex;
    }
}

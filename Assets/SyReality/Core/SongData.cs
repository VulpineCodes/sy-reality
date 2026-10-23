using System;
using System.Collections.Generic;

namespace SyReality.Core
{
    /// <summary>
    /// One timed syllable (or word fragment) inside a lyric line.
    /// Times are seconds relative to the start of the song.
    /// </summary>
    public sealed class SyllableCue
    {
        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public string Text { get; }

        public SyllableCue(double startSeconds, double endSeconds, string text)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Text = text ?? string.Empty;
        }
    }

    /// <summary>One timed line of lyrics. Times are seconds from song start.</summary>
    public sealed class LyriRiver
    {
        public double StartSeconds { get; }
        public double EndSeconds { get; }
        public string Text { get; }
        public IReadOnlyList<SyllableCue> Syllables { get; }

        public LyriRiver(double startSeconds, double endSeconds, string text,
            IReadOnlyList<SyllableCue> syllables = null)
        {
            StartSeconds = startSeconds;
            EndSeconds = endSeconds;
            Text = text ?? string.Empty;
            Syllables = syllables ?? Array.Empty<SyllableCue>();
        }

        public double DurationSeconds => EndSeconds - StartSeconds;
        public bool HasSyllableCues => Syllables.Count > 0;
    }

    /// <summary>
    /// Data-driven definition of one karaoke song. The core is serialization-agnostic:
    /// the Unity layer parses JSON (JsonUtility) into DTOs and constructs these objects.
    /// Content rule: only public-domain, original, or licensed songs may ship as data.
    /// </summary>
    public sealed class SongDefinition
    {
        public string Id { get; }
        public string Title { get; }
        public string Artist { get; }
        /// <summary>Provenance note, e.g. "Traditional — public domain". Required so
        /// bundled content always carries its licensing basis.</summary>
        public string RightsNote { get; }
        public double DurationSeconds { get; }
        /// <summary>1 (easy) – 5 (hard). Informational only.</summary>
        public int Difficulty { get; }
        /// <summary>File name of the audio inside StreamingAssets/Audio, or "" for
        /// the silent / metronome test mode.</summary>
        public string AudioFileName { get; }
        public IReadOnlyList<LyriRiver> Lines { get; }

        public SongDefinition(string id, string title, string artist, string rightsNote,
            double durationSeconds, int difficulty, string audioFileName,
            IReadOnlyList<LyriRiver> lines)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Artist = artist ?? string.Empty;
            RightsNote = rightsNote ?? string.Empty;
            DurationSeconds = durationSeconds;
            Difficulty = difficulty;
            AudioFileName = audioFileName ?? string.Empty;
            Lines = lines ?? Array.Empty<LyriRiver>();
        }

        /// <summary>Structural validation used by loaders and tests. Returns false and
        /// sets <paramref name="error"/> on the first problem found.</summary>
        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(Id)) { error = "Song id is empty."; return false; }
            if (string.IsNullOrWhiteSpace(Title)) { error = "Song title is empty."; return false; }
            if (DurationSeconds <= 0) { error = "Duration must be positive."; return false; }
            if (Lines.Count == 0) { error = "Song has no lyric lines."; return false; }

            double previousEnd = 0;
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                if (line.EndSeconds <= line.StartSeconds)
                { error = $"Line {i} ends at or before it starts."; return false; }
                if (line.StartSeconds < previousEnd - 1e-9)
                { error = $"Line {i} overlaps the previous line or is out of order."; return false; }
                if (line.EndSeconds > DurationSeconds + 1e-9)
                { error = $"Line {i} ends after the song duration."; return false; }
                previousEnd = line.EndSeconds;

                double previousSyllableEnd = line.StartSeconds;
                for (int s = 0; s < line.Syllables.Count; s++)
                {
                    var cue = line.Syllables[s];
                    if (cue.StartSeconds < previousSyllableEnd - 1e-9 ||
                        cue.EndSeconds > line.EndSeconds + 1e-9 ||
                        cue.EndSeconds <= cue.StartSeconds)
                    { error = $"Line {i} syllable {s} is outside the line or out of order."; return false; }
                    previousSyllableEnd = cue.EndSeconds;
                }
            }
            error = null;
            return true;
        }
    }
}

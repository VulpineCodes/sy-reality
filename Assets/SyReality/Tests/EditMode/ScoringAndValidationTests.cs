using System.Collections.Generic;
using NUnit.Framework;
using SyReality.Core;

namespace SyReality.Tests
{
    public sealed class ScoringAndValidationTests
    {
        [Test]
        public void NullScorer_NeverFabricatesAScore()
        {
            var scorer = new NullScorer();
            var result = scorer.ScoreLine(
                TestSongs.ThreeLineSong().Lines[0], new List<float> { 220f, 221f });
            Assert.IsFalse(result.Available);
            StringAssert.Contains("not enabled", result.Note);
        }

        [Test]
        public void Participation_CountsDistinctCompletedLines()
        {
            var tracker = new ParticipationTracker(3);
            tracker.MarkLineCompleted(0);
            tracker.MarkLineCompleted(0); // duplicate: still one line
            tracker.MarkLineCompleted(2);
            tracker.MarkLineCompleted(99); // out of range: ignored
            Assert.AreEqual(2, tracker.CompletedLineCount);
            Assert.AreEqual(67, tracker.CompletionPercent);
        }

        [Test]
        public void BundledStyleSong_Validates()
        {
            Assert.IsTrue(TestSongs.ThreeLineSong().TryValidate(out _));
        }

        [Test]
        public void Song_LineBeyondDuration_FailsValidation()
        {
            var song = new SongDefinition("x", "X", "Tests", "fixture", 5, 1, "",
                new List<LyriRiver> { new LyriRiver(0, 6, "too long") });
            Assert.IsFalse(song.TryValidate(out string error));
            StringAssert.Contains("after the song duration", error);
        }

        [Test]
        public void Song_SyllableOutsideLine_FailsValidation()
        {
            var song = new SongDefinition("x", "X", "Tests", "fixture", 10, 1, "",
                new List<LyriRiver>
                {
                    new LyriRiver(0, 4, "line", new List<SyllableCue>
                    {
                        new SyllableCue(3, 5, "late"), // ends after the line
                    }),
                });
            Assert.IsFalse(song.TryValidate(out _));
        }
    }
}

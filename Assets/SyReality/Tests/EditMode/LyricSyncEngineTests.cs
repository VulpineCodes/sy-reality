using NUnit.Framework;
using SyReality.Core;

namespace SyReality.Tests
{
    public sealed class LyricSyncEngineTests
    {
        private LyricSyncEngine _engine;

        [SetUp]
        public void SetUp() => _engine = new LyricSyncEngine(TestSongs.ThreeLineSong());

        [Test]
        public void BeforeFirstLine_NoActiveLine_NextIsFirst()
        {
            // Song starts at 0, so use a negative time (pre-roll).
            var frame = _engine.GetFrame(-0.5);
            Assert.IsFalse(frame.IsLineActive);
            Assert.AreEqual("Alpha line", frame.NextLine.Text);
        }

        [Test]
        public void AtLineStart_LineIsActive_WithZeroProgress()
        {
            var frame = _engine.GetFrame(0);
            Assert.AreEqual(0, frame.LineIndex);
            Assert.AreEqual(0.0, frame.LineProgress, 1e-9);
        }

        [Test]
        public void MidLine_ProgressIsProportional()
        {
            var frame = _engine.GetFrame(2);
            Assert.AreEqual(0, frame.LineIndex);
            Assert.AreEqual(0.5, frame.LineProgress, 1e-9);
        }

        [Test]
        public void AtBoundary_NextLineBecomesActive()
        {
            var frame = _engine.GetFrame(4);
            Assert.AreEqual(1, frame.LineIndex);
            Assert.AreEqual("Bravo line", frame.CurrentLine.Text);
            Assert.AreEqual("Charlie", frame.NextLine.Text);
        }

        [Test]
        public void InInstrumentalGap_NoActiveLine_NextIsUpcoming()
        {
            var frame = _engine.GetFrame(9);
            Assert.IsFalse(frame.IsLineActive);
            Assert.AreEqual("Charlie", frame.NextLine.Text);
        }

        [Test]
        public void AfterLastLine_NoActiveLine_NoNext()
        {
            var frame = _engine.GetFrame(14.5);
            Assert.IsFalse(frame.IsLineActive);
            Assert.IsNull(frame.NextLine);
        }

        [Test]
        public void SeekBackwards_ReturnsEarlierLine()
        {
            Assert.AreEqual(2, _engine.GetFrame(12).LineIndex);
            Assert.AreEqual(0, _engine.GetFrame(1).LineIndex); // stateless: seek just works
        }

        [Test]
        public void SyllableCues_HighlightAdvancesByCue()
        {
            var first = _engine.GetFrame(5);
            Assert.AreEqual(0, first.SyllableIndex);
            Assert.AreEqual("Bravo ".Length, first.HighlightCharCount);

            var second = _engine.GetFrame(7);
            Assert.AreEqual(1, second.SyllableIndex);
            Assert.AreEqual("Bravo line".Length, second.HighlightCharCount);
        }

        [Test]
        public void NoSyllableCues_HighlightInterpolatesLinearly()
        {
            var frame = _engine.GetFrame(2); // halfway through "Alpha line" (10 chars)
            Assert.AreEqual(-1, frame.SyllableIndex);
            Assert.AreEqual(5, frame.HighlightCharCount);
        }

        [Test]
        public void InvalidSong_OverlappingLines_Throws()
        {
            var bad = new SongDefinition("bad", "Bad", "Tests", "fixture", 10, 1, "",
                new System.Collections.Generic.List<LyriRiver>
                {
                    new LyriRiver(0, 6, "one"),
                    new LyriRiver(5, 9, "overlap"),
                });
            Assert.Throws<System.ArgumentException>(() => new LyricSyncEngine(bad));
        }
    }
}

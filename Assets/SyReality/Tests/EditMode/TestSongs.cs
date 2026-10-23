using System.Collections.Generic;
using SyReality.Core;

namespace SyReality.Tests
{
    /// <summary>Shared fixtures: a small deterministic song with known timings.
    /// Lines: [0,4) "Alpha line", [4,8) "Bravo line" (syllable cues), [10,14) "Charlie"
    /// — note the 8–10s instrumental gap.</summary>
    public static class TestSongs
    {
        public static SongDefinition ThreeLineSong()
        {
            var lines = new List<LyriRiver>
            {
                new LyriRiver(0, 4, "Alpha line"),
                new LyriRiver(4, 8, "Bravo line", new List<SyllableCue>
                {
                    new SyllableCue(4, 6, "Bravo "),
                    new SyllableCue(6, 8, "line"),
                }),
                new LyriRiver(10, 14, "Charlie"),
            };
            return new SongDefinition("test-song", "Test Song", "Tests",
                "Original test fixture", 14, 1, "", lines);
        }
    }
}

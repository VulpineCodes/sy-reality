using System;
using System.Collections.Generic;
using SyReality.Core;
using UnityEngine;

namespace SyReality.Unity
{
    // JSON DTOs matching the documented song file schema (see README "Song data
    // format"). Kept separate from the core model so the core stays
    // serialization-agnostic; JsonUtility fills these, ToDefinition() maps them.

    [Serializable]
    public sealed class SyllableCueDto
    {
        public double start;
        public double end;
        public string text;
    }

    [Serializable]
    public sealed class LyriRiverDto
    {
        public double start;
        public double end;
        public string text;
        public List<SyllableCueDto> syllables = new List<SyllableCueDto>();
    }

    [Serializable]
    public sealed class SongDto
    {
        public string id;
        public string title;
        public string artist;
        public string rightsNote;
        public double durationSeconds;
        public int difficulty = 1;
        public string audioFile = "";
        public List<LyriRiverDto> lines = new List<LyriRiverDto>();

        public SongDefinition ToDefinition()
        {
            var lines = new List<LyriRiver>(lines?.Count ?? 0);
            if (this.lines != null)
            {
                foreach (var l in this.lines)
                {
                    var cues = new List<SyllableCue>();
                    if (l.syllables != null)
                        foreach (var s in l.syllables)
                            cues.Add(new SyllableCue(s.start, s.end, s.text));
                    lines.Add(new LyriRiver(l.start, l.end, l.text, cues));
                }
            }
            return new SongDefinition(id, title, artist, rightsNote,
                durationSeconds, difficulty, audioFile, lines);
        }
    }

    /// <summary>Loads song definitions from JSON TextAssets (Resources/Songs).</summary>
    public static class SongLibraryLoader
    {
        public static List<SongDefinition> LoadFromResources(string folder = "Songs")
        {
            var result = new List<SongDefinition>();
            foreach (var asset in Resources.LoadAll<TextAsset>(folder))
            {
                try
                {
                    var dto = JsonUtility.FromJson<SongDto>(asset.text);
                    var song = dto.ToDefinition();
                    if (song.TryValidate(out string error))
                        result.Add(song);
                    else
                        Debug.LogWarning($"[SyReality] Skipping invalid song '{asset.name}': {error}");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SyReality] Failed to parse song '{asset.name}': {ex.Message}");
                }
            }
            result.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));
            return result;
        }
    }
}

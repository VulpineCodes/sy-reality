using System.Collections.Generic;
using SyReality.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SyReality.Unity
{
    /// <summary>
    /// Song-select panel: builds one button per song in the library (loaded from
    /// Resources/Songs JSON) and forwards the choice to PerformanceManager.
    /// World-space canvas in VR; the same panel works on a desktop preview.
    /// </summary>
    public sealed class SongLibraryUI : MonoBehaviour
    {
        [SerializeField] private PerformanceManager performanceManager;
        [SerializeField] private Transform listRoot;
        [SerializeField] private Button songButtonPrefab;
        [SerializeField] private GameObject panelRoot;

        private readonly List<SongDefinition> _songs = new List<SongDefinition>();

        private void Start()
        {
            _songs.AddRange(SongLibraryLoader.LoadFromResources());
            BuildList();
        }

        private void BuildList()
        {
            if (listRoot == null || songButtonPrefab == null) return;
            foreach (var song in _songs)
            {
                var captured = song;
                var button = Instantiate(songButtonPrefab, listRoot);
                var label = button.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{song.Title} - {song.Artist}  ·  ★{song.Difficulty}";
                button.onClick.AddListener(() => performanceManager?.SelectSong(captured));
            }
        }

        public IReadOnlyList<SongDefinition> Songs => _songs;
        public void Show() { if (panelRoot != null) panelRoot.SetActive(true); }
        public void Hide() { if (panelRoot != null) panelRoot.SetActive(false); }
    }
}

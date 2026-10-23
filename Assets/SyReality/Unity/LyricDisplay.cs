using SyReality.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SyReality.Unity
{
    /// <summary>
    /// Renders the current lyric frame: previous line (dimmed), current line with a
    /// color highlight that advances with the sync engine's character count, next
    /// line (preview), and a progress bar. TextMeshPro rich text does the highlight;
    /// all timing decisions come from the core engine — this component only draws.
    /// </summary>
    public sealed class LyricDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI previousLineText;
        [SerializeField] private TextMeshProUGUI currentLineText;
        [SerializeField] private TextMeshProUGUI nextLineText;
        [SerializeField] private Image progressBar;
        [SerializeField] private Color sungColor = new Color(1f, 0.85f, 0.35f);
        [SerializeField] private Color upcomingColor = Color.white;

        private int _lastLineIndex = int.MinValue;
        private string _previousLine = string.Empty;

        public void Show(LyricFrame frame, string previousLine)
        {
            if (frame.LineIndex != _lastLineIndex)
            {
                _previousLine = frame.LineIndex > 0 || _lastLineIndex >= 0 ? previousLine : string.Empty;
                _lastLineIndex = frame.LineIndex;
                if (previousLineText != null) previousLineText.text = _previousLine;
                if (nextLineText != null)
                    nextLineText.text = frame.NextLine != null ? frame.NextLine.Text : string.Empty;
            }

            if (currentLineText != null)
            {
                if (!frame.IsLineActive)
                {
                    currentLineText.text = frame.NextLine != null ? "♪ … ♪" : string.Empty;
                }
                else
                {
                    string text = frame.CurrentLine.Text;
                    int n = Mathf.Clamp(frame.HighlightCharCount, 0, text.Length);
                    string sung = ColorUtility.ToHtmlStringRGB(sungColor);
                    string rest = ColorUtility.ToHtmlStringRGB(upcomingColor);
                    currentLineText.text =
                        $"<color=#{sung}>{Escape(text.Substring(0, n))}</color>" +
                        $"<color=#{rest}>{Escape(text.Substring(n))}</color>";
                }
            }

            if (progressBar != null)
                progressBar.fillAmount = (float)frame.LineProgress;
        }

        public void Clear()
        {
            _lastLineIndex = int.MinValue;
            if (previousLineText != null) previousLineText.text = string.Empty;
            if (currentLineText != null) currentLineText.text = string.Empty;
            if (nextLineText != null) nextLineText.text = string.Empty;
            if (progressBar != null) progressBar.fillAmount = 0;
        }

        private static string Escape(string s) =>
            s.Replace("<", "‹").Replace(">", "›"); // keep TMP rich-text tags ours only
    }
}

using System.Text;
using Game.Data;
using Game.Localization;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Narrative console: alien glyph noise, Star Cycle header, event text with a
    /// typewriter reveal (maxVisibleCharacters — no per-frame string work).
    /// </summary>
    public class ConsoleView : MonoBehaviour
    {
        [SerializeField] private NarrativeConfig _narrative;
        [SerializeField] private TextMeshProUGUI _cycleLabel;
        [SerializeField] private TextMeshProUGUI _eventText;
        [SerializeField] private TextMeshProUGUI[] _glyphLines;
        [SerializeField] private float _charsPerSecond = 70f;

        private readonly StringBuilder _sb = new StringBuilder(48);
        private float _revealed;
        private int _lastVisible = -1;
        private int _lastCycle = -1;
        private string _cycleFormat = "{0}";

        /// <summary>Re-pulls the translated header format and drops the change guard.</summary>
        public void RefreshStaticText()
        {
            _cycleFormat = Loc.Get(LocKeys.ConsoleCycle);
            _lastCycle = -1;
        }

        public void SetCycle(int cycle)
        {
            if (cycle == _lastCycle) return;
            _lastCycle = cycle;
            _cycleLabel.SetText(_cycleFormat, Mathf.Max(1, cycle));
        }

        public void SetEventText(string tmpRichText)
        {
            _eventText.text = tmpRichText;
            _revealed = 0f;
            _lastVisible = -1;
            _eventText.maxVisibleCharacters = 0;
        }

        public void Tick(float dt)
        {
            // raw length is always ≥ parsed character count — safe reveal cap
            if (_lastVisible > _eventText.text.Length) return;
            _revealed += dt * _charsPerSecond;
            int visible = (int)_revealed;
            if (visible == _lastVisible) return;
            _lastVisible = visible;
            _eventText.maxVisibleCharacters = visible;
        }

        public void ScrambleGlyphs()
        {
            string chars = _narrative.GlyphChars;
            if (string.IsNullOrEmpty(chars)) return;
            for (int l = 0; l < _glyphLines.Length; l++)
            {
                _sb.Clear();
                int n = 24 + Random.Range(0, 8) - l * 2;
                for (int i = 0; i < n; i++)
                {
                    _sb.Append(Random.value < 0.16f ? ' ' : chars[Random.Range(0, chars.Length)]);
                }
                _glyphLines[l].text = _sb.ToString();
            }
        }
    }
}

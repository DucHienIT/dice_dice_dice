using TMPro;
using UnityEngine;

namespace DiceDiceDice
{
    public enum FloatingTextMode
    {
        FlyToTarget = 0,
        Rise = 1
    }

    /// <summary>Pooled popup text (gold gains, CRIT), animated by FloatingTextManager.</summary>
    public class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private RectTransform _rect;

        private FloatingTextMode _mode;
        private Vector2 _start;
        private Vector2 _end;
        private float _time;
        private float _duration;
        private Color _color;

        public void Setup(FloatingTextMode mode, string text, Color color, Vector2 startAnchored, Vector2 endAnchored, float duration)
        {
            _mode = mode;
            _start = startAnchored;
            _end = endAnchored;
            _time = 0f;
            _duration = duration;
            _color = color;
            _label.text = text;
            _label.color = color;
            _rect.anchoredPosition = startAnchored;
            _rect.localScale = Vector3.one;
        }

        public bool Tick(float deltaTime)
        {
            _time += deltaTime;
            float progress = Mathf.Clamp01(_time / _duration);
            float eased = 1f - (1f - progress) * (1f - progress);

            if (_mode == FloatingTextMode.FlyToTarget)
            {
                _rect.anchoredPosition = Vector2.LerpUnclamped(_start, _end, eased);
                _rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.6f, progress);
            }
            else
            {
                _rect.anchoredPosition = _start + new Vector2(0f, eased * 46f);
            }

            Color color = _color;
            color.a = progress > 0.6f ? Mathf.InverseLerp(1f, 0.6f, progress) : 1f;
            _label.color = color;
            return _time >= _duration;
        }
    }
}

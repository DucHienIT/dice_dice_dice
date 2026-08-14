using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>Slide-in reward banner (fortune / upgrade / sidekick). Self-timed via Tick.</summary>
    public class FortuneBanner : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _root;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private TextMeshProUGUI _tag;
        [SerializeField] private float _showSeconds = 3.2f;

        private float _timer = -1f;

        public void Show(Sprite icon, string title, string tag)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _title.text = title;
            _tag.text = tag;
            _timer = _showSeconds;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            _group.alpha = 0f;
            _root.localScale = new Vector3(0.85f, 0.85f, 1f);
        }

        public void Hide()
        {
            _timer = -1f;
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        public void Tick(float dt)
        {
            if (_timer < 0f) return;
            _timer -= dt;
            if (_timer <= 0f)
            {
                Hide();
                return;
            }
            float shown = _showSeconds - _timer;
            // pop in over 0.18s, fade out over the last 0.3s
            float inT = Mathf.Clamp01(shown / 0.18f);
            float s = 0.85f + 0.15f * EaseOutBack(inT);
            _root.localScale = new Vector3(s, s, 1f);
            float alpha = inT;
            if (_timer < 0.3f) alpha = _timer / 0.3f;
            _group.alpha = alpha;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}

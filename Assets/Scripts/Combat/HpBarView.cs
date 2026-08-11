using TMPro;
using UnityEngine;

namespace CCQ.Combat
{
    /// <summary>
    /// World-space HP bar + number. Frame/fill sprites are baked assets assigned on the prefab
    /// (fill pivots at its left edge so localScale.x = ratio). Updates only when values change.
    /// </summary>
    public class HpBarView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _fill;
        [SerializeField] private TextMeshPro _value;
        [SerializeField] private Color _fillTint = Color.white;

        private int _lastCur = int.MinValue;
        private int _lastMax = int.MinValue;

        private void Awake()
        {
            _fill.color = _fillTint;
        }

        public void SetValues(int cur, int max)
        {
            if (cur == _lastCur && max == _lastMax) return;
            _lastCur = cur;
            _lastMax = max;
            float ratio = max <= 0 ? 0f : Mathf.Clamp01(cur / (float)max);
            Vector3 s = _fill.transform.localScale;
            s.x = ratio;
            _fill.transform.localScale = s;
            _value.SetText("{0}", cur);
        }

        public void SetVisible(bool on)
        {
            if (gameObject.activeSelf != on) gameObject.SetActive(on);
            if (!on)
            {
                _lastCur = int.MinValue; // force refresh next show
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace Game.Combat
{
    /// <summary>
    /// Heaven-tribulation lightning: jagged bolts strike down over a world position while
    /// the whole screen flashes white for an instant. Scene-authored and pooled — the
    /// GameManager triggers strikes (boss arrivals, breakthroughs) and ticks the animation;
    /// at rest every renderer is disabled and Tick returns immediately.
    /// </summary>
    public class TribulationFxView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _bolts;
        [SerializeField] private Image _flash;

        [Header("Feel")]
        [SerializeField] private float _duration = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _flashPeak = 0.5f;
        [SerializeField] private float _flickerHz = 24f;
        [SerializeField] private float _boltSpread = 0.6f;

        private float _t;
        private float _intensity;
        private bool _playing;

        /// <summary>Strike above a world position. Intensity scales bolt alpha and flash.</summary>
        public void PlayStrike(Vector3 worldPos, float intensity)
        {
            _t = 0f;
            _intensity = intensity;
            _playing = true;
            for (int i = 0; i < _bolts.Length; i++)
            {
                SpriteRenderer bolt = _bolts[i];
                float dx = (i - (_bolts.Length - 1) * 0.5f) * _boltSpread;
                bolt.transform.position = worldPos + new Vector3(dx, 2.4f + 0.5f * i, 0f);
                bolt.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 8f);
                bolt.flipX = (i & 1) == 1;
                bolt.enabled = true;
            }
            if (_flash != null) _flash.enabled = true;
            Tick(0f);
        }

        public void Tick(float dt)
        {
            if (!_playing) return;
            _t += dt;
            float life = _duration <= 0f ? 1f : Mathf.Clamp01(_t / _duration);
            if (life >= 1f)
            {
                _playing = false;
                for (int i = 0; i < _bolts.Length; i++) _bolts[i].enabled = false;
                if (_flash != null) _flash.enabled = false;
                return;
            }

            float fade = 1f - life;
            float flicker = 0.55f + 0.45f * Mathf.Sin(_t * _flickerHz * Mathf.PI * 2f);
            for (int i = 0; i < _bolts.Length; i++)
            {
                Color c = _bolts[i].color;
                c.a = _intensity * fade * flicker;
                _bolts[i].color = c;
            }
            if (_flash != null)
            {
                Color f = _flash.color;
                f.a = _flashPeak * _intensity * fade * fade;
                _flash.color = f;
            }
        }
    }
}

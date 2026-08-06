using UnityEngine;

namespace DiceDiceDice
{
    public enum EffectKind
    {
        ExplosionRing = 0,
        DeathPop = 1,
        FrostRing = 2,
        Slash = 3,
        HealRise = 4
    }

    /// <summary>Pooled sprite effect, animated by EffectManager. Returns true from Tick when finished.</summary>
    public class VisualEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private Transform _cachedTransform;
        private EffectKind _kind;
        private float _duration;
        private float _time;
        private float _targetScale;
        private Color _color;
        private Vector2 _origin;

        private void Awake()
        {
            _cachedTransform = transform;
        }

        public void Setup(EffectKind kind, Vector2 position, float scale, Color color)
        {
            _kind = kind;
            _time = 0f;
            _targetScale = scale;
            _color = color;
            _origin = position;
            _cachedTransform.position = position;
            _cachedTransform.rotation = Quaternion.identity;

            switch (kind)
            {
                case EffectKind.ExplosionRing:
                    _duration = 0.35f;
                    _renderer.sprite = SpriteFactory.Ring;
                    break;
                case EffectKind.DeathPop:
                    _duration = 0.35f;
                    _renderer.sprite = SpriteFactory.SoftCircle;
                    break;
                case EffectKind.FrostRing:
                    _duration = 0.3f;
                    _renderer.sprite = SpriteFactory.Ring;
                    break;
                case EffectKind.Slash:
                    _duration = 0.25f;
                    _renderer.sprite = SpriteFactory.Slash;
                    _cachedTransform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));
                    break;
                case EffectKind.HealRise:
                    _duration = 0.4f;
                    _renderer.sprite = SpriteFactory.HealCross;
                    break;
            }
            _renderer.color = color;
            _cachedTransform.localScale = Vector3.one * (kind == EffectKind.Slash || kind == EffectKind.HealRise ? scale : 0.05f);
        }

        public bool Tick(float deltaTime)
        {
            _time += deltaTime;
            float progress = Mathf.Clamp01(_time / _duration);
            float alpha = 1f - progress;

            switch (_kind)
            {
                case EffectKind.ExplosionRing:
                case EffectKind.FrostRing:
                case EffectKind.DeathPop:
                    _cachedTransform.localScale = Vector3.one * Mathf.Lerp(0.1f, _targetScale, progress);
                    break;
                case EffectKind.HealRise:
                    _cachedTransform.position = _origin + new Vector2(0f, progress * 0.4f);
                    break;
            }

            Color color = _color;
            color.a = _color.a * alpha;
            _renderer.color = color;
            return _time >= _duration;
        }
    }
}

using UnityEngine;

namespace CCQ.Enemies
{
    /// <summary>
    /// One pooled critter renderer. The look is assembled from pre-baked layers wired on the
    /// prefab (glow / horns / spikes / body / spots / eyes / mouth) — Init only swaps the body
    /// sprite, picks the eye count and toggles the optional layers. No runtime painting.
    /// </summary>
    public class CritterView : MonoBehaviour
    {
        [Header("Layers (draw order: glow, horns, spikes, body, spots, eyes, mouth)")]
        [SerializeField] private SpriteRenderer _glow;
        [SerializeField] private SpriteRenderer _horns;
        [SerializeField] private SpriteRenderer _spikes;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private SpriteRenderer _spots;
        [SerializeField] private SpriteRenderer _eyes;
        [SerializeField] private SpriteRenderer _mouth;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private SpriteRenderer _flash;

        [Header("Baked variants")]
        [Tooltip("One body per GameConfig.CritterColors entry, same order.")]
        [SerializeField] private Sprite[] _bodySprites;
        [Tooltip("Index 0..2 = 1..3 eyes.")]
        [SerializeField] private Sprite[] _eyeSprites;

        [Tooltip("Every layer that tints/fades with the body (all of the above except shadow/flash).")]
        [SerializeField] private SpriteRenderer[] _tintLayers;

        private float _size = 1f;

        public float Size => _size;

        private void Awake()
        {
            _flash.enabled = false;
        }

        public void Init(EnemyState enemy)
        {
            CritterLook look = enemy.Look;
            _size = look.Size;

            _body.sprite = _bodySprites[look.ColorIndex % _bodySprites.Length];
            _eyes.sprite = _eyeSprites[Mathf.Clamp(look.Eyes, 1, _eyeSprites.Length) - 1];

            _glow.enabled = enemy.Kind == EnemyKind.Elite;
            _spikes.enabled = enemy.Kind == EnemyKind.Boss;
            _horns.enabled = look.Horns;
            _spots.enabled = look.Spots;

            SetBodyTint(Color.white);
            _shadow.color = Color.white;
            _flash.enabled = false;
            transform.localScale = new Vector3(_size, _size, 1f);
        }

        public void SetFlash(bool on)
        {
            if (_flash.enabled != on) _flash.enabled = on;
        }

        public void SetBodyTint(Color tint)
        {
            for (int i = 0; i < _tintLayers.Length; i++)
            {
                _tintLayers[i].color = tint;
            }
        }

        /// <summary>Idle squish/spawn-pop: scale multipliers around the base look size.</summary>
        public void SetSquish(float mulX, float mulY)
        {
            transform.localScale = new Vector3(_size * mulX, _size * mulY, 1f);
        }

        /// <summary>t 0→1: dissolve to stardust (scale down + fade).</summary>
        public void SetDissolve(float t)
        {
            float s = _size * (1f - t * 0.6f);
            transform.localScale = new Vector3(s, s, 1f);
            float a = 1f - t;
            for (int i = 0; i < _tintLayers.Length; i++)
            {
                Color c = _tintLayers[i].color;
                c.a = a;
                _tintLayers[i].color = c;
            }
            Color sc = _shadow.color;
            sc.a = a;
            _shadow.color = sc;
        }
    }
}

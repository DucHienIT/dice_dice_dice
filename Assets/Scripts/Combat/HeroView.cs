using UnityEngine;

namespace CCQ.Combat
{
    /// <summary>
    /// Astro-alien hero. Every sprite is baked to an asset at build time and assigned on the
    /// prefab's SpriteRenderers; this view only drives the walk hop, sword swing and hit flash.
    /// BattleStageView positions the root.
    /// </summary>
    public class HeroView : MonoBehaviour
    {
        [Tooltip("Holds body/sword/flash — hops and leans while walking, so the shadow stays put.")]
        [SerializeField] private Transform _rig;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private SpriteRenderer _sword;
        [SerializeField] private SpriteRenderer _flash;
        [SerializeField] private float _swordRestAngle = 28f;
        [SerializeField] private float _swordSwingAngle = -70f;
        [Header("Walk")]
        [SerializeField] private float _hopHeight = 0.16f;
        [SerializeField] private float _leanAngle = 5f;
        [Tooltip("How much the shadow shrinks at the top of a hop.")]
        [SerializeField] private float _shadowShrink = 1.3f;

        private void Awake()
        {
            _flash.enabled = false;
        }

        public void SetSwing(float swing01)
        {
            float z = _swordRestAngle + _swordSwingAngle * Mathf.Sin(swing01 * Mathf.PI);
            _sword.transform.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        public void SetFlash(bool on)
        {
            if (_flash.enabled != on) _flash.enabled = on;
        }

        /// <summary>
        /// Walk cycle. <paramref name="phase"/> counts hops (one hop per whole number),
        /// <paramref name="amount"/> 0→1 blends the whole thing in and out so stopping is smooth.
        /// </summary>
        public void SetWalk(float phase, float amount)
        {
            float hop = Mathf.Abs(Mathf.Sin(phase * Mathf.PI)) * _hopHeight * amount;
            _rig.localPosition = new Vector3(0f, hop, 0f);
            _rig.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin(phase * Mathf.PI * 2f) * _leanAngle * amount);
            float s = Mathf.Max(0.2f, 1f - hop * _shadowShrink);
            _shadow.transform.localScale = new Vector3(s, s, 1f);
        }
    }
}

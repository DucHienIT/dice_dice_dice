using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Astro-alien hero. Every sprite is baked to an asset at build time and assigned on the
    /// prefab's SpriteRenderers; this view only drives the walk hop, sword swing and hit flash.
    /// BattleStageView positions the root.
    /// </summary>
    public class HeroView : MonoBehaviour
    {
        [Tooltip("Holds body/sword/flash — hops and leans while walking, so the shadow stays put.")]
        
        [SerializeField] private SpriteRenderer _body;
        [SerializeField] private Sprite _idleSprite;
        [SerializeField] private Sprite[] _runFrames;
[SerializeField] private Transform _rig;
        [SerializeField] private SpriteRenderer _shadow;
        [SerializeField] private SpriteRenderer _sword;
        [SerializeField] private SpriteRenderer _flash;
        [SerializeField] private float _swordRestAngle = 28f;
        [SerializeField] private float _swordSwingAngle = -70f;
        [Header("Walk")]
        [SerializeField] private float _hopHeight = 0.09f;
        [SerializeField] private float _leanAngle = 3f;
        [Tooltip("How much the shadow shrinks at the top of a hop.")]
        [SerializeField] private float _shadowShrink = 1.1f;

        private float _runSwordOffset;
        private int _shownFrame = -2;

private void Awake()
        {
            _flash.enabled = false;
            ShowFrame(-1);
        }

public void SetSwing(float swing01)
        {
            float z = _swordRestAngle + _runSwordOffset +
                _swordSwingAngle * Mathf.Sin(swing01 * Mathf.PI);
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
            float cycle = phase * Mathf.PI * 2f;
            float stride = Mathf.Sin(cycle);
            float hop = Mathf.Abs(stride) * _hopHeight * amount;

            _rig.localPosition = new Vector3(stride * 0.025f * amount, hop, 0f);
            _rig.localRotation = Quaternion.Euler(0f, 0f,
                -stride * _leanAngle * amount);
            _runSwordOffset = stride * 9f * amount;

            float s = Mathf.Max(0.72f, 1f - hop * _shadowShrink);
            _shadow.transform.localScale = new Vector3(s, s, 1f);

            if (amount > 0.22f && _runFrames != null && _runFrames.Length > 0)
            {
                int frame = Mathf.FloorToInt(Mathf.Repeat(phase, 1f) * _runFrames.Length);
                ShowFrame(frame);
            }
            else
            {
                ShowFrame(-1);
            }
        }
    

private void ShowFrame(int frame)
        {
            if (_shownFrame == frame) return;
            _shownFrame = frame;
            _body.sprite = frame >= 0 && frame < _runFrames.Length
                ? _runFrames[frame]
                : _idleSprite;
        }
}
}

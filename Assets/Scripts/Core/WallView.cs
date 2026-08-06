using DG.Tweening;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>World visuals: wall, ground, board backdrop, shield glow, cracks and hit shake. Pure presentation.</summary>
    public class WallView : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private PaletteConfig _palette;
        [SerializeField] private BaseWall _wall;
        [SerializeField] private SpriteRenderer _wallBody;
        [SerializeField] private SpriteRenderer _wallHitFlash;
        [SerializeField] private SpriteRenderer _ground;
        [SerializeField] private SpriteRenderer _boardBackdrop;
        [SerializeField] private SpriteRenderer _shieldGlow;
        [SerializeField] private SpriteRenderer _crackLow;
        [SerializeField] private SpriteRenderer _crackHigh;

        private const float FieldHalfHeight = 5.4f;
        private const float FieldHalfWidth = 9.6f;

        private Transform _wallTransform;
        private Vector3 _wallHome;

        public void Init()
        {
            _wallTransform = _wallBody.transform.parent;
            _wallHome = _wallTransform.localPosition;

            float wallLeft = _config.WallCenterX - _config.WallWidth * 0.5f;

            _wallBody.sprite = SpriteFactory.White;
            _wallBody.color = _palette.Wall;
            _wallBody.transform.localScale = new Vector3(_config.WallWidth, FieldHalfHeight * 2f, 1f);

            _wallHitFlash.sprite = SpriteFactory.White;
            _wallHitFlash.color = new Color(1f, 0.35f, 0.35f, 0f);
            _wallHitFlash.transform.localScale = new Vector3(_config.WallWidth + 0.08f, FieldHalfHeight * 2f, 1f);

            _ground.sprite = SpriteFactory.White;
            _ground.color = _palette.Ground;
            _ground.transform.position = new Vector3(0f, -FieldHalfHeight + 0.25f, 0f);
            _ground.transform.localScale = new Vector3(FieldHalfWidth * 2f + 0.4f, 0.5f, 1f);

            _boardBackdrop.sprite = SpriteFactory.White;
            Color backdrop = _palette.BackgroundTop;
            backdrop.a = 0.55f;
            _boardBackdrop.color = backdrop;
            float backdropWidth = wallLeft + FieldHalfWidth;
            _boardBackdrop.transform.position = new Vector3((-FieldHalfWidth + wallLeft) * 0.5f, 0f, 0f);
            _boardBackdrop.transform.localScale = new Vector3(backdropWidth, FieldHalfHeight * 2f, 1f);

            _shieldGlow.sprite = SpriteFactory.White;
            _shieldGlow.color = new Color(_palette.Shield.r, _palette.Shield.g, _palette.Shield.b, 0.45f);
            _shieldGlow.transform.position = new Vector3(_config.WallStopX + 0.12f, 0f, 0f);
            _shieldGlow.transform.localScale = new Vector3(0.12f, FieldHalfHeight * 2f, 1f);

            _crackLow.sprite = SpriteFactory.Slash;
            _crackHigh.sprite = SpriteFactory.Slash;
            Color crackColor = _palette.WallDark;
            _crackLow.color = crackColor;
            _crackHigh.color = crackColor;
            _crackLow.transform.position = new Vector3(_config.WallCenterX, -1.6f, 0f);
            _crackHigh.transform.position = new Vector3(_config.WallCenterX, 1.4f, 0f);
            _crackLow.transform.localScale = Vector3.one * 0.45f;
            _crackHigh.transform.localScale = Vector3.one * 0.45f;

            _wall.HpChanged += RefreshState;
            RefreshState();
        }

        public void PlayHit()
        {
            _wallTransform.DOKill();
            _wallTransform.localPosition = _wallHome;
            _wallTransform.DOShakePosition(0.25f, new Vector3(0.07f, 0f, 0f), 20).SetLink(gameObject);
            _wallHitFlash.DOKill();
            Color flash = _wallHitFlash.color;
            flash.a = 0.5f;
            _wallHitFlash.color = flash;
            _wallHitFlash.DOFade(0f, 0.3f).SetLink(gameObject);
        }

        private void RefreshState()
        {
            bool lowHp = _wall.MaxHp > 0 && (float)_wall.Hp / _wall.MaxHp < 0.4f;
            if (_crackLow.enabled != lowHp)
            {
                _crackLow.enabled = lowHp;
                _crackHigh.enabled = lowHp;
            }
            bool shielded = _wall.Shield > 0;
            if (_shieldGlow.enabled != shielded)
            {
                _shieldGlow.enabled = shielded;
            }
        }
    }
}

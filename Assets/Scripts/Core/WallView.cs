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
        [SerializeField] private Sprite _arenaSprite;
        [SerializeField] private Sprite _wallSprite;
        [SerializeField] private SpriteRenderer _wallBody;
        [SerializeField] private SpriteRenderer _wallHitFlash;
        [SerializeField] private SpriteRenderer _ground;
        [SerializeField] private SpriteRenderer _boardBackdrop;
        [SerializeField] private SpriteRenderer _shieldGlow;
        [SerializeField] private SpriteRenderer _crackLow;
        [SerializeField] private SpriteRenderer _crackHigh;

        private const float FieldHalfHeight = 5.4f;
        private const float FieldHalfWidth = 9.6f;
        private const float WallVisualWidth = 0.9f;
        /// <summary>Backdrop/ground overscan: phones are wider or taller than the 1920x1080 design box
        /// (see ScreenFitter), so the flat fills must run past its edges.</summary>
        private const float Overscan = 5f;

        private Transform _wallTransform;
        private Vector3 _wallHome;

        public void Init()
        {
            _wallTransform = _wallBody.transform.parent;
            _wallHome = _wallTransform.localPosition;

            float wallLeft = _config.WallCenterX - _config.WallWidth * 0.5f;

            _wallBody.sprite = _wallSprite != null ? _wallSprite : SpriteFactory.White;
            _wallBody.color = _wallSprite != null ? Color.white : _palette.Wall;
            SetSpriteSize(_wallBody, WallVisualWidth, FieldHalfHeight * 2f);
            _wallBody.transform.localPosition = new Vector3((_config.WallWidth - WallVisualWidth) * 0.5f, 0f, 0f);

            _wallHitFlash.sprite = _wallBody.sprite;
            _wallHitFlash.color = new Color(1f, 0.35f, 0.35f, 0f);
            _wallHitFlash.transform.localScale = _wallBody.transform.localScale;
            _wallHitFlash.transform.localPosition = _wallBody.transform.localPosition;

            _ground.enabled = _arenaSprite == null;
            if (_ground.enabled)
            {
                _ground.sprite = SpriteFactory.White;
                _ground.color = _palette.Ground;
                _ground.transform.position = new Vector3(0f, -FieldHalfHeight + 0.5f - Overscan * 0.5f, 0f);
                _ground.transform.localScale = new Vector3((FieldHalfWidth + Overscan) * 2f, Overscan, 1f);
            }

            _boardBackdrop.sprite = _arenaSprite != null ? _arenaSprite : SpriteFactory.White;
            _boardBackdrop.color = _arenaSprite != null
                ? Color.white
                : new Color(_palette.BackgroundTop.r, _palette.BackgroundTop.g, _palette.BackgroundTop.b, 0.55f);
            if (_arenaSprite != null)
            {
                _boardBackdrop.transform.position = Vector3.zero;
                SetSpriteSize(_boardBackdrop, FieldHalfWidth * 2f, FieldHalfHeight * 2f);
            }
            else
            {
                float backdropWidth = wallLeft + FieldHalfWidth + Overscan;
                _boardBackdrop.transform.position = new Vector3((-FieldHalfWidth - Overscan + wallLeft) * 0.5f, 0f, 0f);
                _boardBackdrop.transform.localScale = new Vector3(backdropWidth, (FieldHalfHeight + Overscan) * 2f, 1f);
            }

            _shieldGlow.sprite = SpriteFactory.White;
            _shieldGlow.color = new Color(_palette.Shield.r, _palette.Shield.g, _palette.Shield.b, 0.45f);
            _shieldGlow.transform.position = new Vector3(_config.WallStopX + 0.12f, 0f, 0f);
            _shieldGlow.transform.localScale = new Vector3(0.12f, (FieldHalfHeight + Overscan) * 2f, 1f);

            _crackLow.sprite = SpriteFactory.Slash;
            _crackHigh.sprite = SpriteFactory.Slash;
            Color crackColor = _palette.WallDark;
            _crackLow.color = crackColor;
            _crackHigh.color = crackColor;
            float wallVisualCenterX = _config.WallCenterX + (_config.WallWidth - WallVisualWidth) * 0.5f;
            _crackLow.transform.position = new Vector3(wallVisualCenterX, -1.6f, 0f);
            _crackHigh.transform.position = new Vector3(wallVisualCenterX, 1.4f, 0f);
            _crackLow.transform.localScale = Vector3.one * 0.45f;
            _crackHigh.transform.localScale = Vector3.one * 0.45f;

            _wall.HpChanged += RefreshState;
            RefreshState();
        }

        private static void SetSpriteSize(SpriteRenderer renderer, float width, float height)
        {
            Vector2 spriteSize = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
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

using Game.Data;
using UnityEngine;

namespace Game.Worlds
{
    /// <summary>
    /// Two-layer side-scrolling backdrop. The distant sky is repeated as a wide, mirrored
    /// three-tile belt; the ground strip (ground, rocks, lake, flora) is baked to tile seamlessly
    /// and is drawn twice side by side, scrolling left while the hero walks forward. Nothing is
    /// painted at runtime — both sprites live on the World asset.
    /// </summary>
    public class WorldBackgroundRenderer : MonoBehaviour
    {
        private const int SkyCopyCount = 3;
        private const float AuthoredBackdropOffsetY = 1.6f;
        [SerializeField] private SpriteRenderer _sky;
        [Tooltip("Two copies of the same strip, leap-frogging each other as it scrolls.")]
        [SerializeField] private SpriteRenderer[] _groundCopies;
        [Tooltip("Animated lake glow, one per ground copy — rides along with the lake.")]
        [SerializeField] private SpriteRenderer[] _lakeGlows;
        [SerializeField] private SpriteRenderer[] _twinkles;
        [SerializeField] private float _worldWidth = 14.6f;
        [SerializeField] private float _worldHeight = 12f;
        [SerializeField] private float _bottomWorldY = -2.4f;
        [SerializeField] private float _horizonWorldY = 3f;
        [SerializeField] private float _authoredSkyParallax = 0.22f;
        [SerializeField] private float _proceduralSkyParallax = 0.12f;

        private float[] _twinklePhases;
        private SpriteRenderer[] _skyCopies;
        private float _scrollX;
        private float _skyScrollX;
        private float _skyTileWidth;
        private bool _authoredBackdrop;
        private Vector3 _skyBasePosition;

private void Awake()
        {
            _twinklePhases = new float[_twinkles.Length];
            EnsureSkyCopies();
        }

public void Build(int worldIndex, World def)
        {
            EnsureSkyCopies();
            _authoredBackdrop = def.AuthoredBackdrop;
            float skyY = _authoredBackdrop
                ? _bottomWorldY + AuthoredBackdropOffsetY
                : _bottomWorldY;
            _skyBasePosition = new Vector3(0f, skyY, 0f);
            Vector3 skyScale = _authoredBackdrop
                ? new Vector3(1.16f, 1f, 1f)
                : Vector3.one;
            for (int i = 0; i < _skyCopies.Length; i++)
            {
                _skyCopies[i].sprite = def.SkyLayer;
                _skyCopies[i].transform.localScale = skyScale;
                _skyCopies[i].gameObject.SetActive(true);
            }
            _skyTileWidth = def.SkyLayer != null
                ? Mathf.Max(0.01f, def.SkyLayer.bounds.size.x * skyScale.x)
                : _worldWidth;

            bool showAtmosphericFx = !_authoredBackdrop;
            var glow = new Color(def.Lake.r, def.Lake.g, def.Lake.b, 0.18f);
            for (int i = 0; i < _groundCopies.Length; i++)
            {
                _groundCopies[i].sprite = def.GroundLayer;
                _groundCopies[i].gameObject.SetActive(true);
                _lakeGlows[i].color = glow;
                _lakeGlows[i].gameObject.SetActive(showAtmosphericFx);
            }
            _scrollX = 0f;
            _skyScrollX = 0f;
            PlaceSky();
            PlaceGround();

            var rng = new System.Random(worldIndex * 1337 + 7);
            for (int i = 0; i < _twinkles.Length; i++)
            {
                _twinkles[i].gameObject.SetActive(showAtmosphericFx);
                float tx = ((float)rng.NextDouble() - 0.5f) * _worldWidth * 0.92f;
                float ty = Mathf.Lerp(_horizonWorldY + 1.6f,
                    _bottomWorldY + _worldHeight - 0.5f, (float)rng.NextDouble());
                _twinkles[i].transform.position = new Vector3(tx, ty, 0f);
                _twinklePhases[i] = (float)rng.NextDouble() * Mathf.PI * 2f;
            }
        }

        /// <summary>Advances the ground by <paramref name="worldDelta"/> units to the left.</summary>
public void Scroll(float worldDelta)
        {
            // Nearby landmarks move at full speed; the wide mountain belt drifts left and
            // only wraps after one whole tile has left the camera.
            _scrollX += worldDelta;
            _skyScrollX += worldDelta * (_authoredBackdrop
                ? _authoredSkyParallax
                : _proceduralSkyParallax);
            PlaceSky();
            PlaceGround();
        }

        public void Tick(float time)
        {
            for (int i = 0; i < _twinkles.Length; i++)
            {
                Color c = _twinkles[i].color;
                c.a = 0.25f + 0.55f * Mathf.Abs(Mathf.Sin(time * 1.4f + _twinklePhases[i]));
                _twinkles[i].color = c;
            }
            float a = 0.14f + 0.06f * Mathf.Sin(time * 0.9f);
            for (int i = 0; i < _lakeGlows.Length; i++)
            {
                Color lc = _lakeGlows[i].color;
                lc.a = a;
                _lakeGlows[i].color = lc;
            }
        }

        private void PlaceGround()
        {
            // one copy trails off to the left while the next slides in from the right
            float off = -Mathf.Repeat(_scrollX, _worldWidth);
            for (int i = 0; i < _groundCopies.Length; i++)
            {
                _groundCopies[i].transform.position =
                    new Vector3(off + i * _worldWidth, _bottomWorldY, 0f);
            }
        }
    

private void EnsureSkyCopies()
        {
            if (_skyCopies != null && _skyCopies.Length == SkyCopyCount) return;

            _skyCopies = new SpriteRenderer[SkyCopyCount];
            _skyCopies[0] = _sky;
            for (int i = 1; i < _skyCopies.Length; i++)
            {
                SpriteRenderer copy = Instantiate(_sky, _sky.transform.parent, true);
                copy.name = "Sky Loop " + i;
                _skyCopies[i] = copy;
            }
        }

        private void PlaceSky()
        {
            if (_skyCopies == null || _skyTileWidth <= 0f) return;

            float wrapped = Mathf.Repeat(_skyScrollX, _skyTileWidth);
            int firstTile = Mathf.FloorToInt(_skyScrollX / _skyTileWidth);
            for (int i = 0; i < _skyCopies.Length; i++)
            {
                int tileIndex = firstTile + i;
                _skyCopies[i].flipX = _authoredBackdrop && (tileIndex & 1) != 0;
                _skyCopies[i].transform.position = _skyBasePosition +
                    Vector3.right * (-wrapped + i * _skyTileWidth);
            }
        }
}
}

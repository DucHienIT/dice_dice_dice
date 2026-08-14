using Game.Data;
using UnityEngine;

namespace Game.Worlds
{
    /// <summary>
    /// Two-layer side-scrolling backdrop. The distant sky is baked per world
    /// and drifts with subtle parallax; the ground strip (ground, rocks, lake, flora) is baked to tile seamlessly
    /// and is drawn twice side by side, scrolling left while the hero walks forward. Nothing is
    /// painted at runtime — both sprites live on the World asset.
    /// </summary>
    public class WorldBackgroundRenderer : MonoBehaviour
    {
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

        private float[] _twinklePhases;
        private float _scrollX;
        private float _skyScrollX;
        private bool _authoredBackdrop;
        private Vector3 _skyBasePosition;

        private void Awake()
        {
            _twinklePhases = new float[_twinkles.Length];
        }

        public void Build(int worldIndex, World def)
        {
            _authoredBackdrop = def.AuthoredBackdrop;
            _sky.sprite = def.SkyLayer;
            float skyY = _authoredBackdrop
                ? _bottomWorldY + AuthoredBackdropOffsetY
                : _bottomWorldY;
            _skyBasePosition = new Vector3(0f, skyY, 0f);
            _sky.transform.position = _skyBasePosition;
            _sky.transform.localScale = _authoredBackdrop
                ? new Vector3(1.08f, 1f, 1f)
                : Vector3.one;

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
            // Nearby road landmarks move at full speed; distant mountains drift slowly.
            _scrollX += worldDelta;
            _skyScrollX += worldDelta;
            float parallaxRate = _authoredBackdrop ? 0.07f : 0.035f;
            float parallaxRange = _authoredBackdrop ? 0.32f : 0.18f;
            float skyOffset = Mathf.PingPong(_skyScrollX * parallaxRate, parallaxRange);
            _sky.transform.position = _skyBasePosition + Vector3.left * skyOffset;
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
    }
}

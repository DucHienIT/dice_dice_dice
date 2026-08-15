using Game.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Worlds
{
    /// <summary>
    /// Side-scrolling backdrop: the sky is repeated as a wide, mirrored three-tile belt
    /// that drifts left while the hero walks forward. Nothing is painted at runtime —
    /// the sky sprite is addressable (one bundle per world) and streamed in by Enter;
    /// the previous world's sprite stays on screen until the new one has loaded, and its
    /// handle is only released after the swap so the texture is never yanked mid-frame.
    /// </summary>
    public class WorldBackgroundRenderer : MonoBehaviour
    {
        private const int SkyCopyCount = 3;
        private const float AuthoredBackdropOffsetY = 1.6f;
        [SerializeField] private SpriteRenderer _sky;
        [SerializeField] private SpriteRenderer[] _twinkles;
        [SerializeField] private float _worldWidth = 14.6f;
        [SerializeField] private float _worldHeight = 12f;
        [SerializeField] private float _bottomWorldY = -2.4f;
        [SerializeField] private float _horizonWorldY = 3f;
        [SerializeField] private float _authoredSkyParallax = 0.5f;
        [SerializeField] private float _proceduralSkyParallax = 0.3f;

        private float[] _twinklePhases;
        private SpriteRenderer[] _skyCopies;
        private float _skyScrollX;
        private float _skyTileWidth;
        private bool _authoredBackdrop;
        private Vector3 _skyBasePosition;
        // handle backing the sprite currently on screen — released only after a newer one lands
        private AsyncOperationHandle<Sprite> _skyHandle;
        private AsyncOperationHandle<Sprite> _pendingHandle;
        private int _pendingWorldIndex;
        private World _pendingWorld;
        private System.Action<AsyncOperationHandle<Sprite>> _onSkyLoaded;

        private void Awake()
        {
            _twinklePhases = new float[_twinkles.Length];
            _onSkyLoaded = OnSkyLoaded;
            EnsureSkyCopies();
        }

        private void OnDestroy()
        {
            if (_pendingHandle.IsValid()) Addressables.Release(_pendingHandle);
            if (_skyHandle.IsValid()) Addressables.Release(_skyHandle);
        }

        /// <summary>
        /// Streams in the world's sky bundle and swaps the belt over on arrival. Safe to
        /// call again before the previous load finished — the superseded load is dropped.
        /// </summary>
        public void Enter(int worldIndex, World def)
        {
            if (_pendingHandle.IsValid())
            {
                Addressables.Release(_pendingHandle);
                _pendingHandle = default;
            }
            _pendingWorldIndex = worldIndex;
            _pendingWorld = def;
            _pendingHandle = Addressables.LoadAssetAsync<Sprite>(def.SkyLayerRef.RuntimeKey);
            _pendingHandle.Completed += _onSkyLoaded;
        }

        private void OnSkyLoaded(AsyncOperationHandle<Sprite> handle)
        {
            if (!handle.Equals(_pendingHandle)) return; // superseded by a newer Enter

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[World] Sky backdrop failed to load for " + _pendingWorld.name);
                return;
            }
            Apply(_pendingWorldIndex, _pendingWorld, handle.Result);
            if (_skyHandle.IsValid()) Addressables.Release(_skyHandle);
            _skyHandle = handle;
            _pendingHandle = default;
        }

        private void Apply(int worldIndex, World def, Sprite sky)
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
                _skyCopies[i].sprite = sky;
                _skyCopies[i].transform.localScale = skyScale;
                _skyCopies[i].gameObject.SetActive(true);
            }
            _skyTileWidth = sky != null
                ? Mathf.Max(0.01f, sky.bounds.size.x * skyScale.x)
                : _worldWidth;

            bool showAtmosphericFx = !_authoredBackdrop;
            _skyScrollX = 0f;
            PlaceSky();

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

        /// <summary>Advances the backdrop by <paramref name="worldDelta"/> units to the left.</summary>
        public void Scroll(float worldDelta)
        {
            // the wide mountain belt drifts left and only wraps after one whole tile has left the camera
            _skyScrollX += worldDelta * (_authoredBackdrop
                ? _authoredSkyParallax
                : _proceduralSkyParallax);
            PlaceSky();
        }

        public void Tick(float time)
        {
            for (int i = 0; i < _twinkles.Length; i++)
            {
                Color c = _twinkles[i].color;
                c.a = 0.25f + 0.55f * Mathf.Abs(Mathf.Sin(time * 1.4f + _twinklePhases[i]));
                _twinkles[i].color = c;
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

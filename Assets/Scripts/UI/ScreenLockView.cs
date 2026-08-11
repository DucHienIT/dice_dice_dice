using CCQ.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace CCQ.UI
{
    /// <summary>
    /// Pillarboxes the game to its authored portrait aspect on any window shape.
    /// Camera side is one line (<see cref="Camera.rect"/> makes camera.aspect the locked
    /// value, so framing and ScreenPointToRay follow for free); the Canvas side needs
    /// real work because a Screen Space - Overlay canvas always covers the whole window
    /// and CanvasScaler derives its factor from Screen.width/height, which knows nothing
    /// about the black bars.
    /// </summary>
    public class ScreenLockView : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [Tooltip("One Frame per scaler, same index. Every screen lives under its Frame.")]
        [SerializeField] private CanvasScaler[] _scalers;
        [SerializeField] private RectTransform[] _frames;
        [Tooltip("Off falls back to the pre-lock layout: full-window camera and untouched scalers.")]
        [SerializeField] private bool _portraitLock = true;
        [SerializeField] private Vector2 _designResolution = new Vector2(1080f, 1920f);

        private CanvasScaler.ScreenMatchMode[] _authoredMatchMode;
        private Vector2[] _authoredReference;
        private float[] _authoredMatch;
        private int _lastWidth = -1;
        private int _lastHeight = -1;

        /// <summary>True when the layout changed and dependents must refit.</summary>
        public bool Tick()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return false;
            Apply();
            return true;
        }

        public void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            CacheAuthoredValues();

            ScreenLayout.Lock(_portraitLock && _designResolution.y > 0f
                ? _designResolution.x / _designResolution.y
                : 0f);

            if (_camera != null) _camera.rect = ScreenLayout.Viewport;

            Vector2 viewport = ScreenLayout.ViewportPixels;
            for (int i = 0; i < _scalers.Length; i++)
            {
                CanvasScaler scaler = _scalers[i];
                RectTransform frame = _frames[i];
                if (scaler == null || frame == null) continue;

                if (!ScreenLayout.IsLocked)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.screenMatchMode = _authoredMatchMode[i];
                    scaler.referenceResolution = _authoredReference[i];
                    scaler.matchWidthOrHeight = _authoredMatch[i];
                    Stretch(frame);
                    continue;
                }

                float scale = ScaleFactorFor(i, viewport);
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = scale;

                frame.anchorMin = new Vector2(0.5f, 0.5f);
                frame.anchorMax = new Vector2(0.5f, 0.5f);
                frame.pivot = new Vector2(0.5f, 0.5f);
                frame.anchoredPosition = Vector2.zero;
                frame.sizeDelta = viewport / scale;
            }
        }

        /// <summary>
        /// Reproduces the scaler's own ScaleWithScreenSize math against the viewport
        /// instead of the window. Using a simpler rule here would silently re-tune the
        /// UI on shapes it was already correct for.
        /// </summary>
        private float ScaleFactorFor(int i, Vector2 viewport)
        {
            Vector2 reference = _authoredReference[i];
            float wRatio = viewport.x / Mathf.Max(1f, reference.x);
            float hRatio = viewport.y / Mathf.Max(1f, reference.y);
            switch (_authoredMatchMode[i])
            {
                case CanvasScaler.ScreenMatchMode.Expand:
                    return Mathf.Min(wRatio, hRatio);
                case CanvasScaler.ScreenMatchMode.Shrink:
                    return Mathf.Max(wRatio, hRatio);
                default:
                    float logW = Mathf.Log(wRatio, 2f);
                    float logH = Mathf.Log(hRatio, 2f);
                    return Mathf.Pow(2f, Mathf.Lerp(logW, logH, _authoredMatch[i]));
            }
        }

        private void CacheAuthoredValues()
        {
            // Apply() overwrites uiScaleMode, so the authored numbers are read once,
            // before the first overwrite, and reused from then on.
            if (_authoredReference != null) return;
            int n = _scalers.Length;
            _authoredReference = new Vector2[n];
            _authoredMatch = new float[n];
            _authoredMatchMode = new CanvasScaler.ScreenMatchMode[n];
            for (int i = 0; i < n; i++)
            {
                if (_scalers[i] == null) continue;
                _authoredReference[i] = _scalers[i].referenceResolution;
                _authoredMatch[i] = _scalers[i].matchWidthOrHeight;
                _authoredMatchMode[i] = _scalers[i].screenMatchMode;
            }
        }

        private static void Stretch(RectTransform frame)
        {
            frame.anchorMin = Vector2.zero;
            frame.anchorMax = Vector2.one;
            frame.pivot = new Vector2(0.5f, 0.5f);
            frame.offsetMin = Vector2.zero;
            frame.offsetMax = Vector2.zero;
        }
    }
}

using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>
    /// Keeps the 1920x1080 design box fully visible on any phone aspect and pushes edge-anchored UI
    /// out of notches / rounded corners. The camera grows (never shrinks) so the design box always fits;
    /// paired with a CanvasScaler in Expand mode this keeps 1 world unit = exactly 100 canvas px,
    /// which the board slots rely on. Ticked by <see cref="GameManager"/> - no own Update (CODE_RULES 5.2).
    /// </summary>
    public class ScreenFitter : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform[] _safeAreaRects;

        /// <summary>Half of the 1920x1080 design box in world units (1u = 100px).</summary>
        public const float DesignHalfWidth = 9.6f;
        public const float DesignHalfHeight = 5.4f;

        private int _width = -1;
        private int _height = -1;
        private Rect _safeArea;

        public void Init()
        {
            Apply();
        }

        public void Tick()
        {
            if (Screen.width == _width && Screen.height == _height && Screen.safeArea == _safeArea)
            {
                return;
            }
            Apply();
        }

        private void Apply()
        {
            _width = Screen.width;
            _height = Screen.height;
            _safeArea = Screen.safeArea;

            float aspect = _height > 0 ? (float)_width / _height : DesignHalfWidth / DesignHalfHeight;
            _camera.orthographicSize = Mathf.Max(DesignHalfHeight, DesignHalfWidth / Mathf.Max(0.1f, aspect));

            if (_width <= 0 || _height <= 0)
            {
                return;
            }
            var min = new Vector2(_safeArea.xMin / _width, _safeArea.yMin / _height);
            var max = new Vector2(_safeArea.xMax / _width, _safeArea.yMax / _height);
            for (int i = 0; i < _safeAreaRects.Length; i++)
            {
                RectTransform rect = _safeAreaRects[i];
                rect.anchorMin = min;
                rect.anchorMax = max;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
        }
    }
}

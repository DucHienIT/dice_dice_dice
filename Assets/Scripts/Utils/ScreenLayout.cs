using UnityEngine;

namespace Game.Utils
{
    /// <summary>
    /// Aspect lock shared by the camera and every Canvas. Pure math over
    /// <see cref="Screen"/>; <see cref="ScreenLockView"/> owns the value and the
    /// application of it, so nothing here reads config or touches the scene.
    /// </summary>
    public static class ScreenLayout
    {
        private static readonly Rect Full = new Rect(0f, 0f, 1f, 1f);

        /// <summary>Width / height the game is authored for; 0 means "no lock".</summary>
        public static float LockedAspect { get; private set; }

        public static bool IsLocked => LockedAspect > 0f;

        public static float ScreenAspect => (float)Screen.width / Mathf.Max(1, Screen.height);

        public static void Lock(float aspect) => LockedAspect = aspect > 0f ? aspect : 0f;

        public static void Unlock() => LockedAspect = 0f;

        /// <summary>
        /// Normalized viewport the game is allowed to draw into. A window TALLER than
        /// the locked aspect is left alone — a vertical layout absorbs spare height for
        /// free, and letterboxing it would waste screen on 9:20 phones. Only the wide
        /// side gets pillarboxed.
        /// </summary>
        public static Rect Viewport
        {
            get
            {
                if (!IsLocked) return Full;
                float aspect = ScreenAspect;
                if (aspect <= LockedAspect) return Full;
                float width = LockedAspect / aspect;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }
        }

        public static Vector2 ViewportPixels
        {
            get
            {
                Rect v = Viewport;
                return new Vector2(v.width * Screen.width, v.height * Screen.height);
            }
        }
    }
}

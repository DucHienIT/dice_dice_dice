using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>
    /// Pools popup texts on the popup canvas. World positions map to anchored positions 1u = 100px
    /// because the canvas reference resolution (1920x1080) matches the camera view (ortho 5.4).
    /// </summary>
    public class FloatingTextManager : MonoBehaviour
    {
        private const float WorldToUi = 100f;

        [SerializeField] private GameConfig _config;
        [SerializeField] private PaletteConfig _palette;
        [SerializeField] private FloatingText _prefab;
        [SerializeField] private RectTransform _poolParent;
        [SerializeField] private RectTransform _goldTarget;

        private ObjectPool<FloatingText> _pool;
        private readonly List<FloatingText> _active = new List<FloatingText>(32);

        public void Init()
        {
            _pool = new ObjectPool<FloatingText>(_prefab, _poolParent, _config.FloatingTextPoolSize);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Tick(deltaTime))
                {
                    _pool.Release(_active[i]);
                    _active[i] = _active[_active.Count - 1];
                    _active.RemoveAt(_active.Count - 1);
                }
            }
        }

        public void ShowGoldFly(Vector2 worldPosition, int amount)
        {
            Vector2 start = worldPosition * WorldToUi;
            Vector2 end = GoldTargetAnchored();
            Spawn(FloatingTextMode.FlyToTarget, "+" + amount, _palette.Gold, start, end, 0.7f);
        }

        public void ShowCrit(Vector2 worldPosition)
        {
            Spawn(FloatingTextMode.Rise, "CRIT!", _palette.Crit, worldPosition * WorldToUi + new Vector2(0f, 18f), Vector2.zero, 0.5f);
        }

        private Vector2 GoldTargetAnchored()
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, _goldTarget.position);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_poolParent, screen, null, out local);
            return local;
        }

        private void Spawn(FloatingTextMode mode, string text, Color color, Vector2 start, Vector2 end, float duration)
        {
            FloatingText instance = _pool.Get();
            instance.Setup(mode, text, color, start, end, duration);
            _active.Add(instance);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Pools and ticks all world-space visual effects in one loop.</summary>
    public class EffectManager : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private PaletteConfig _palette;
        [SerializeField] private VisualEffect _effectPrefab;
        [SerializeField] private LightningBolt _lightningPrefab;
        [SerializeField] private Transform _effectParent;
        [SerializeField] private FloatingTextManager _floatingText;
        [SerializeField] private WallView _wallView;

        private ObjectPool<VisualEffect> _effectPool;
        private ObjectPool<LightningBolt> _lightningPool;
        private readonly List<VisualEffect> _activeEffects = new List<VisualEffect>(48);
        private readonly List<LightningBolt> _activeLightning = new List<LightningBolt>(8);
        private Material _lineMaterial;

        public void Init()
        {
            _effectPool = new ObjectPool<VisualEffect>(_effectPrefab, _effectParent, _config.EffectPoolSize);
            _lightningPool = new ObjectPool<LightningBolt>(_lightningPrefab, _effectParent, _config.LightningPoolSize);
        }

        public void Tick(float deltaTime)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                if (_activeEffects[i].Tick(deltaTime))
                {
                    _effectPool.Release(_activeEffects[i]);
                    _activeEffects[i] = _activeEffects[_activeEffects.Count - 1];
                    _activeEffects.RemoveAt(_activeEffects.Count - 1);
                }
            }
            for (int i = _activeLightning.Count - 1; i >= 0; i--)
            {
                if (_activeLightning[i].Tick(deltaTime))
                {
                    _lightningPool.Release(_activeLightning[i]);
                    _activeLightning[i] = _activeLightning[_activeLightning.Count - 1];
                    _activeLightning.RemoveAt(_activeLightning.Count - 1);
                }
            }
        }

        public void DespawnAll()
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                _effectPool.Release(_activeEffects[i]);
            }
            _activeEffects.Clear();
            for (int i = _activeLightning.Count - 1; i >= 0; i--)
            {
                _lightningPool.Release(_activeLightning[i]);
            }
            _activeLightning.Clear();
        }

        public void SpawnExplosion(Vector2 position, float radius)
        {
            Spawn(EffectKind.ExplosionRing, position, radius * 2f, new Color(1f, 0.61f, 0.29f, 0.95f));
        }

        public void SpawnDeathPop(Vector2 position, float radius, Color color)
        {
            Spawn(EffectKind.DeathPop, position, radius * 4f, color);
        }

        public void SpawnFrostRing(Vector2 position)
        {
            Spawn(EffectKind.FrostRing, position, 0.7f, new Color(0.66f, 0.91f, 1f, 0.9f));
        }

        public void SpawnSlash(Vector2 position)
        {
            Spawn(EffectKind.Slash, position, 0.6f, Color.white);
        }

        public void SpawnHeal(Vector2 position)
        {
            Spawn(EffectKind.HealRise, position, 0.3f, new Color(1f, 0.54f, 0.78f));
        }

        public void SpawnCritLabel(Vector2 worldPosition)
        {
            _floatingText.ShowCrit(worldPosition);
        }

        public void SpawnLightning(Vector2 from, Vector2 to)
        {
            LightningBolt bolt = _lightningPool.Get();
            if (bolt.Line.sharedMaterial == null)
            {
                if (_lineMaterial == null)
                {
                    _lineMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                bolt.Line.sharedMaterial = _lineMaterial;
            }
            bolt.Setup(from, to, new Color(1f, 0.91f, 0.42f));
            _activeLightning.Add(bolt);
        }

        public void SpawnWallHit(float y)
        {
            _wallView.PlayHit();
        }

        private void Spawn(EffectKind kind, Vector2 position, float scale, Color color)
        {
            VisualEffect effect = _effectPool.Get();
            effect.Setup(kind, position, scale, color);
            _activeEffects.Add(effect);
        }
    }
}

using Game.Data;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Pooled damage/heal floaters, prewarmed in the scene, ticked centrally.
    /// Zero allocation on the number paths (cached strings / SetText).
    /// </summary>
    public class FloaterManager : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private Floater[] _pool;
        [SerializeField] private float _jitterX = 0.14f;
        [SerializeField] private float _bigSize = 5.2f;
        [SerializeField] private float _normalSize = 3.6f;

        public static readonly Color DamageColor = Color.white;
        public static readonly Color CritColor = new Color(1f, 0.67f, 0.24f);
        public static readonly Color HeroHurtColor = new Color(1f, 0.56f, 0.65f);
        public static readonly Color HeroCritColor = new Color(1f, 0.24f, 0.36f);
        public static readonly Color HealColor = new Color(0.55f, 0.94f, 0.48f);
        public static readonly Color ThornsColor = new Color(0.78f, 0.61f, 1f);
        public static readonly Color NoticeColor = new Color(1f, 0.83f, 0.36f);

        public void SpawnAmount(Vector3 pos, int amount, bool crit, Color color, bool positive)
        {
            Floater f = Next();
            if (f == null) return;
            Prime(f, pos, crit ? _bigSize : _normalSize, color);
            if (positive) f.Text.SetText("+{0}", amount);
            else if (crit) f.Text.SetText("{0}!", amount);
            else f.Text.SetText("{0}", amount);
        }

        /// <summary>Rare notices only (LEVEL UP!, ENRAGED!) — string set is allowed here.</summary>
        public void SpawnNotice(Vector3 pos, string text, Color color)
        {
            Floater f = Next();
            if (f == null) return;
            Prime(f, pos, _bigSize, color);
            f.Text.text = text;
        }

        public void Clear()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (_pool[i].Active) _pool[i].OnDespawn();
            }
        }

        public void Tick(float dt)
        {
            float life = _config.FloaterLife;
            float rise = _config.FloaterRise;
            float fadeStart = life * 0.72f;
            for (int i = 0; i < _pool.Length; i++)
            {
                Floater f = _pool[i];
                if (!f.Active) continue;
                f.Age += dt;
                if (f.Age >= life)
                {
                    f.OnDespawn();
                    continue;
                }
                Transform t = f.transform;
                Vector3 pos = t.position;
                pos.y += rise * dt;
                t.position = pos;
                if (f.Age > fadeStart)
                {
                    float a = 1f - (f.Age - fadeStart) / (life - fadeStart);
                    Color c = f.Text.color;
                    c.a = a;
                    f.Text.color = c;
                }
            }
        }

        private Floater Next()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].Active) return _pool[i];
            }
            // pool exhausted: recycle the oldest instead of allocating
            Floater oldest = _pool[0];
            for (int i = 1; i < _pool.Length; i++)
            {
                if (_pool[i].Age > oldest.Age) oldest = _pool[i];
            }
            oldest.OnDespawn();
            return oldest;
        }

        private void Prime(Floater f, Vector3 pos, float size, Color color)
        {
            pos.x += Random.Range(-_jitterX, _jitterX);
            f.transform.position = pos;
            f.Age = 0f;
            f.Active = true;
            f.Text.fontSize = size;
            color.a = 1f;
            f.Text.color = color;
            f.gameObject.SetActive(true);
        }
    }
}

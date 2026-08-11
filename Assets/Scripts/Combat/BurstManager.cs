using UnityEngine;

namespace CCQ.Combat
{
    /// <summary>
    /// Pooled stardust burst particles (hit impacts, kills, level-ups). The star sprite is a
    /// baked asset on the BurstStar prefab.
    /// Prewarmed sprite pool ticked centrally — no ParticleSystem, no allocation.
    /// </summary>
    public class BurstManager : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _pool;
        [SerializeField] private float _gravity = 3.2f;
        [SerializeField] private float _life = 0.55f;

        private Vector2[] _velocity;
        private float[] _age;
        private bool[] _active;

        private void Awake()
        {
            _velocity = new Vector2[_pool.Length];
            _age = new float[_pool.Length];
            _active = new bool[_pool.Length];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i].gameObject.SetActive(false);
            }
        }

        public void Burst(Vector3 pos, Color color, int count, float speed)
        {
            int spawned = 0;
            for (int i = 0; i < _pool.Length && spawned < count; i++)
            {
                if (_active[i]) continue;
                _active[i] = true;
                _age[i] = 0f;
                float ang = Random.value * Mathf.PI * 2f;
                float spd = speed * Random.Range(0.5f, 1.2f);
                _velocity[i] = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + speed * 0.35f);
                SpriteRenderer sr = _pool[i];
                sr.transform.position = pos;
                sr.transform.localScale = Vector3.one * Random.Range(0.7f, 1.25f);
                Color c = color;
                c.a = 1f;
                sr.color = c;
                sr.gameObject.SetActive(true);
                spawned++;
            }
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_active[i]) continue;
                _age[i] += dt;
                if (_age[i] >= _life)
                {
                    _active[i] = false;
                    _pool[i].gameObject.SetActive(false);
                    continue;
                }
                _velocity[i].y -= _gravity * dt;
                Transform t = _pool[i].transform;
                Vector3 pos = t.position;
                pos.x += _velocity[i].x * dt;
                pos.y += _velocity[i].y * dt;
                t.position = pos;
                float k = 1f - _age[i] / _life;
                t.localScale = Vector3.one * (0.4f + 0.85f * k);
                Color c = _pool[i].color;
                c.a = k;
                _pool[i].color = c;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Expands a WaveDefinition into a time-sorted spawn queue and feeds EnemyManager.</summary>
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private WaveSet _waveSet;
        [SerializeField] private EnemyManager _enemies;

        private struct QueuedSpawn
        {
            public float Time;
            public EnemyDefinition Enemy;
        }

        private readonly List<QueuedSpawn> _queue = new List<QueuedSpawn>(64);
        private int _cursor;
        private float _time;
        private int _wave;

        public bool IsFinished => _cursor >= _queue.Count;

        public WaveDefinition CurrentWave => _waveSet.GetWave(_wave - 1);

        public int TotalWaves => _waveSet.WaveCount;

        public void Begin(int wave)
        {
            _wave = wave;
            _time = 0f;
            _cursor = 0;
            _queue.Clear();
            WaveDefinition definition = _waveSet.GetWave(wave - 1);
            List<SpawnEntry> spawns = definition.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                SpawnEntry entry = spawns[i];
                for (int n = 0; n < entry.Count; n++)
                {
                    QueuedSpawn item;
                    item.Time = entry.StartTime + n * entry.Interval;
                    item.Enemy = entry.Enemy;
                    _queue.Add(item);
                }
            }
            _queue.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        public void Tick(float deltaTime)
        {
            if (IsFinished)
            {
                return;
            }
            _time += deltaTime;
            while (_cursor < _queue.Count && _queue[_cursor].Time <= _time)
            {
                _enemies.Spawn(_queue[_cursor].Enemy, _wave);
                _cursor++;
            }
        }
    }
}

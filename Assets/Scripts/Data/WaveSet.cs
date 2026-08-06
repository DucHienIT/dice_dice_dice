using System;
using System.Collections.Generic;
using UnityEngine;

namespace DiceDiceDice
{
    [Serializable]
    public class SpawnEntry
    {
        [SerializeField] private EnemyDefinition _enemy;
        [SerializeField] private int _count = 1;
        [SerializeField] private float _interval = 1f;
        [SerializeField] private float _startTime;

        public EnemyDefinition Enemy => _enemy;
        public int Count => _count;
        public float Interval => _interval;
        public float StartTime => _startTime;

#if UNITY_EDITOR
        public SpawnEntry() { }

        public SpawnEntry(EnemyDefinition enemy, int count, float interval, float startTime)
        {
            _enemy = enemy;
            _count = count;
            _interval = interval;
            _startTime = startTime;
        }
#endif
    }

    [Serializable]
    public class WaveDefinition
    {
        [SerializeField] private string _label;
        [SerializeField] private bool _bossAlarm;
        [SerializeField] private List<SpawnEntry> _spawns = new List<SpawnEntry>();

        public string Label => _label;
        public bool BossAlarm => _bossAlarm;
        public List<SpawnEntry> Spawns => _spawns;

#if UNITY_EDITOR
        public WaveDefinition() { }

        public WaveDefinition(string label, bool bossAlarm, List<SpawnEntry> spawns)
        {
            _label = label;
            _bossAlarm = bossAlarm;
            _spawns = spawns;
        }
#endif
    }

    /// <summary>The full 10-wave run structure (spec section 15).</summary>
    [CreateAssetMenu(menuName = "DiceDiceDice/Wave Set", fileName = "WaveSet")]
    public class WaveSet : ScriptableObject
    {
        [SerializeField] private List<WaveDefinition> _waves = new List<WaveDefinition>();

        public int WaveCount => _waves.Count;

        public WaveDefinition GetWave(int index)
        {
            return _waves[index];
        }

#if UNITY_EDITOR
        public void EditorSetWaves(List<WaveDefinition> waves)
        {
            _waves = waves;
        }
#endif
    }
}

using System;

namespace CCQ.Save
{
    /// <summary>
    /// JSON payload for ccq_meta — the only progress that outlives a run. Ranks are stored
    /// next to their upgrade id so reordering or inserting a track keeps them.
    /// </summary>
    [Serializable]
    public class MetaSaveData
    {
        public int shards;
        public int lifetime;
        public string[] upgradeIds;
        public int[] ranks;
    }
}

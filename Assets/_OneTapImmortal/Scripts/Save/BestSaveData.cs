using System;

namespace Game.Save
{
    /// <summary>best — overwritten only when the new score is higher.</summary>
    [Serializable]
    public class BestSaveData
    {
        public int score;   // total global rounds
        public int world;  // 1-based for display
        public int round;
        public int lv;
    }
}

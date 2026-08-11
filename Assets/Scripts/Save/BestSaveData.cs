using System;

namespace CCQ.Save
{
    /// <summary>ccq_best — overwritten only when the new score is higher.</summary>
    [Serializable]
    public class BestSaveData
    {
        public int score;   // total global rounds
        public int planet;  // 1-based for display
        public int round;
        public int lv;
    }
}

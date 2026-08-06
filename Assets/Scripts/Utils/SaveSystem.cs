using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>PlayerPrefs persistence with a game-specific key prefix.</summary>
    public static class SaveSystem
    {
        private const string MutedKey = "DDD_Muted";
        private const string BestWaveKey = "DDD_BestWave";

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(MutedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(MutedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static int BestWave
        {
            get => PlayerPrefs.GetInt(BestWaveKey, 0);
            set
            {
                if (value <= BestWave)
                {
                    return;
                }
                PlayerPrefs.SetInt(BestWaveKey, value);
                PlayerPrefs.Save();
            }
        }
    }
}

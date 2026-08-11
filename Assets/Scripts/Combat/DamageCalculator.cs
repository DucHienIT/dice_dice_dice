using UnityEngine;

namespace CCQ.Combat
{
    /// <summary>Spec damage formula: atk × rand(1±variance) [× critMult] − def, minimum 1.</summary>
    public static class DamageCalculator
    {
        public static int Roll(float atk, int def, float critChance, float critMult,
            float variance, out bool crit)
        {
            float raw = atk * Random.Range(1f - variance, 1f + variance);
            crit = Random.value < critChance;
            if (crit) raw *= critMult;
            return Mathf.Max(1, Mathf.RoundToInt(raw - def));
        }
    }
}

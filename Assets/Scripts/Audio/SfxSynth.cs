using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Procedural audio: every clip is synthesized once at startup — no audio files.
    /// </summary>
    public static class SfxSynth
    {
        private const int Rate = 44100;

        public static AudioClip Engage()
        {
            float[] d = Buffer(0.09f);
            AddSine(d, 660f, 990f, 0f, 0.09f, 0.5f, 24f);
            return Make("sfx_engage", d);
        }

        public static AudioClip Click()
        {
            float[] d = Buffer(0.05f);
            AddSine(d, 880f, 660f, 0f, 0.05f, 0.35f, 40f);
            return Make("sfx_click", d);
        }

        public static AudioClip Hit()
        {
            float[] d = Buffer(0.16f);
            AddSine(d, 150f, 55f, 0f, 0.14f, 0.85f, 18f);
            AddNoise(d, 0f, 0.06f, 0.35f, 55f);
            return Make("sfx_hit", d);
        }

        public static AudioClip CritHit()
        {
            float[] d = Buffer(0.22f);
            AddSine(d, 220f, 60f, 0f, 0.2f, 0.9f, 14f);
            AddNoise(d, 0f, 0.1f, 0.5f, 40f);
            AddSine(d, 1320f, 880f, 0f, 0.08f, 0.25f, 30f);
            return Make("sfx_crit", d);
        }

        public static AudioClip HeroHurt()
        {
            float[] d = Buffer(0.18f);
            AddSine(d, 330f, 160f, 0f, 0.16f, 0.6f, 16f);
            AddNoise(d, 0f, 0.05f, 0.25f, 60f);
            return Make("sfx_hurt", d);
        }

        public static AudioClip Heal()
        {
            float[] d = Buffer(0.3f);
            AddSine(d, 523f, 523f, 0f, 0.14f, 0.3f, 12f);
            AddSine(d, 784f, 784f, 0.08f, 0.2f, 0.3f, 10f);
            return Make("sfx_heal", d);
        }

        public static AudioClip Chime()
        {
            float[] d = Buffer(0.45f);
            AddSine(d, 659f, 659f, 0f, 0.18f, 0.28f, 8f);
            AddSine(d, 831f, 831f, 0.1f, 0.2f, 0.28f, 8f);
            AddSine(d, 988f, 988f, 0.2f, 0.24f, 0.28f, 7f);
            return Make("sfx_chime", d);
        }

        public static AudioClip WinArp()
        {
            float[] d = Buffer(0.6f);
            AddSine(d, 523f, 523f, 0f, 0.16f, 0.3f, 10f);
            AddSine(d, 659f, 659f, 0.1f, 0.16f, 0.3f, 10f);
            AddSine(d, 784f, 784f, 0.2f, 0.16f, 0.3f, 10f);
            AddSine(d, 1047f, 1047f, 0.3f, 0.28f, 0.32f, 7f);
            return Make("sfx_win", d);
        }

        public static AudioClip LevelUp()
        {
            float[] d = Buffer(0.5f);
            AddSine(d, 440f, 1760f, 0f, 0.35f, 0.35f, 6f);
            AddSine(d, 2093f, 2093f, 0.3f, 0.2f, 0.25f, 9f);
            return Make("sfx_levelup", d);
        }

        public static AudioClip Trap()
        {
            float[] d = Buffer(0.28f);
            AddSine(d, 190f, 90f, 0f, 0.24f, 0.55f, 10f);
            AddNoise(d, 0f, 0.2f, 0.3f, 14f);
            return Make("sfx_trap", d);
        }

        public static AudioClip BossSting()
        {
            float[] d = Buffer(1.0f);
            AddSine(d, 98f, 98f, 0f, 0.9f, 0.45f, 3f);
            AddSine(d, 147f, 147f, 0.12f, 0.8f, 0.35f, 3f);
            AddSine(d, 196f, 185f, 0.3f, 0.6f, 0.25f, 4f);
            AddNoise(d, 0f, 0.12f, 0.2f, 25f);
            return Make("sfx_boss", d);
        }

        public static AudioClip Death()
        {
            float[] d = Buffer(1.2f);
            AddSine(d, 440f, 110f, 0f, 1.1f, 0.4f, 3f);
            AddSine(d, 220f, 55f, 0.2f, 0.9f, 0.35f, 3f);
            return Make("sfx_death", d);
        }

        public static AudioClip Warp()
        {
            float[] d = Buffer(1.0f);
            AddSine(d, 220f, 1320f, 0f, 0.85f, 0.3f, 3.2f);
            AddNoise(d, 0f, 0.9f, 0.18f, 3.5f);
            AddSine(d, 1567f, 2093f, 0.7f, 0.28f, 0.22f, 8f);
            return Make("sfx_warp", d);
        }

        /// <summary>Seamless ambient pad; frequencies are quantized to the loop length.</summary>
        public static AudioClip PadLoop(float rootHz, bool minor)
        {
            const float len = 6f;
            float[] d = Buffer(len);
            float third = rootHz * (minor ? 1.1892f : 1.2599f); // minor/major third
            AddLoopSine(d, len, rootHz, 0.16f, 0.11f);
            AddLoopSine(d, len, rootHz * 1.5f, 0.11f, 0.23f);
            AddLoopSine(d, len, rootHz * 2f, 0.08f, 0.17f);
            AddLoopSine(d, len, third * 2f, 0.06f, 0.29f);
            AddLoopSine(d, len, rootHz * 4.007f, 0.025f, 0.41f); // slight shimmer detune
            return Make("music_pad", d);
        }

        private static float[] Buffer(float seconds) => new float[(int)(Rate * seconds)];

        private static void AddSine(float[] d, float f0, float f1, float start, float dur,
            float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            int n = Mathf.Min(d.Length - s0, (int)(dur * Rate));
            float phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float k = i / (float)n;
                float f = Mathf.Lerp(f0, f1, k);
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Exp(-decay * t) * Mathf.Min(1f, i / (Rate * 0.004f));
                d[s0 + i] += Mathf.Sin(phase) * amp * env;
            }
        }

        private static void AddNoise(float[] d, float start, float dur, float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            int n = Mathf.Min(d.Length - s0, (int)(dur * Rate));
            var rng = new System.Random(1234);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-decay * t);
                d[s0 + i] += ((float)rng.NextDouble() * 2f - 1f) * amp * env;
            }
        }

        private static void AddLoopSine(float[] d, float loopLen, float freq, float amp, float lfoHz)
        {
            // quantize so the phase closes exactly at the loop point — no click
            float f = Mathf.Round(freq * loopLen) / loopLen;
            float lfo = Mathf.Round(lfoHz * loopLen) / loopLen;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float env = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * lfo * t);
                d[i] += Mathf.Sin(2f * Mathf.PI * f * t) * amp * env;
            }
        }

        private static AudioClip Make(string name, float[] data)
        {
            // soft clip
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

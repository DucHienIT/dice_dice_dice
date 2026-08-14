using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Procedural audio: every clip is synthesized once at startup — no audio files.
    /// The tuning is the Chinese pentatonic (cung–thương–giốc–chủy–vũ); melodic SFX are
    /// degree-indexed so everything shares one modal color, and the plucked envelope
    /// gives the guzheng-like character the theme calls for.
    /// </summary>
    public static class SfxSynth
    {
        private const int Rate = 44100;

        // gong, shang, jue, zhi, yu — 1, 9/8, 5/4, 3/2, 5/3
        private static readonly float[] Pentatonic = { 1f, 1.125f, 1.25f, 1.5f, 1.6667f };

        /// <summary>Frequency of a pentatonic degree; degrees past 4 fold up an octave.</summary>
        private static float Note(float rootHz, int degree)
        {
            int octave = degree / 5;
            int step = degree % 5;
            return rootHz * Pentatonic[step] * (1 << octave);
        }

        public static AudioClip Engage()
        {
            float[] d = Buffer(0.14f);
            AddPluck(d, 660f, 0f, 0.4f, 9f);
            AddPluck(d, 990f, 0.04f, 0.22f, 11f);
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
            float[] d = Buffer(0.26f);
            AddSine(d, 220f, 60f, 0f, 0.2f, 0.9f, 14f);
            AddNoise(d, 0f, 0.1f, 0.5f, 40f);
            // sword-qi ring
            AddPluck(d, 1760f, 0.02f, 0.2f, 7f);
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
            // spirit-spring: root and fifth plucked gently (D5 base)
            float[] d = Buffer(0.5f);
            AddPluck(d, Note(587.33f, 0), 0f, 0.26f, 5f);
            AddPluck(d, Note(587.33f, 3), 0.1f, 0.26f, 4.5f);
            return Make("sfx_heal", d);
        }

        public static AudioClip Chime()
        {
            // fortuitous encounter: three rising pentatonic plucks
            float[] d = Buffer(0.7f);
            AddPluck(d, Note(587.33f, 0), 0f, 0.26f, 4.5f);
            AddPluck(d, Note(587.33f, 2), 0.11f, 0.26f, 4.5f);
            AddPluck(d, Note(587.33f, 4), 0.22f, 0.28f, 4f);
            return Make("sfx_chime", d);
        }

        public static AudioClip WinArp()
        {
            // victory run up the scale to the octave
            float[] d = Buffer(0.85f);
            AddPluck(d, Note(523.25f, 0), 0f, 0.28f, 5f);
            AddPluck(d, Note(523.25f, 2), 0.1f, 0.28f, 5f);
            AddPluck(d, Note(523.25f, 3), 0.2f, 0.28f, 5f);
            AddPluck(d, Note(523.25f, 5), 0.3f, 0.32f, 4f);
            return Make("sfx_win", d);
        }

        public static AudioClip LevelUp()
        {
            // breakthrough: rising sweep capped with an artifact ring
            float[] d = Buffer(0.8f);
            AddSine(d, 440f, 1760f, 0f, 0.35f, 0.35f, 6f);
            AddPluck(d, 2093f, 0.3f, 0.28f, 3.5f);
            AddPluck(d, 1568f, 0.38f, 0.2f, 3f);
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
            // temple bell (two detuned partials ~1:2.4) over thunder
            float[] d = Buffer(1.4f);
            AddSine(d, 98f, 98f, 0f, 1.3f, 0.42f, 2.2f);
            AddSine(d, 235f, 233f, 0f, 1.1f, 0.22f, 2.8f);
            AddSine(d, 49f, 49f, 0.02f, 0.9f, 0.3f, 3f);
            AddRumble(d, 0f, 0.7f, 0.4f, 6f);
            return Make("sfx_boss", d);
        }

        public static AudioClip Death()
        {
            float[] d = Buffer(1.5f);
            AddSine(d, 440f, 110f, 0f, 1.1f, 0.4f, 3f);
            AddSine(d, 220f, 55f, 0.2f, 0.9f, 0.35f, 3f);
            // the last toll
            AddSine(d, 98f, 98f, 0.5f, 0.9f, 0.3f, 2.5f);
            return Make("sfx_death", d);
        }

        public static AudioClip Warp()
        {
            // ascension: whoosh plus a quick pentatonic run skyward
            float[] d = Buffer(1.1f);
            AddSine(d, 220f, 1320f, 0f, 0.85f, 0.26f, 3.2f);
            AddNoise(d, 0f, 0.9f, 0.15f, 3.5f);
            AddPluck(d, Note(587.33f, 3), 0.55f, 0.2f, 5f);
            AddPluck(d, Note(587.33f, 5), 0.7f, 0.22f, 4.5f);
            AddPluck(d, Note(587.33f, 7), 0.85f, 0.24f, 4f);
            return Make("sfx_warp", d);
        }

        /// <summary>
        /// Seamless ambient loop: an open root/fifth/octave drone (no third — the modal,
        /// unresolved color) with a slow plucked melody on top. Melody degrees index the
        /// pentatonic an octave above the root; negative entries are rests. All frequencies
        /// are quantized to the loop length and melody tails wrap around the seam.
        /// </summary>
        public static AudioClip PadLoop(float rootHz, bool minor, int[] melodyDegrees)
        {
            const float len = 6f;
            float[] d = Buffer(len);
            AddLoopSine(d, len, rootHz, 0.15f, 0.11f);
            AddLoopSine(d, len, rootHz * 1.5f, 0.10f, 0.23f);
            AddLoopSine(d, len, rootHz * 2f, 0.07f, 0.17f);
            // shimmer sits on the 12th for the darker realms, the 13th for the bright ones
            AddLoopSine(d, len, rootHz * (minor ? 3f : 3.3333f), 0.03f, 0.41f);

            if (melodyDegrees != null && melodyDegrees.Length > 0)
            {
                float slot = len / melodyDegrees.Length;
                for (int i = 0; i < melodyDegrees.Length; i++)
                {
                    int degree = melodyDegrees[i];
                    if (degree < 0) continue;
                    float f = Note(rootHz * 2f, degree);
                    f = Mathf.Round(f * len) / len; // loop-phase safe
                    AddPluckWrapped(d, f, i * slot, 0.085f, 1.6f);
                }
            }
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

        /// <summary>
        /// Plucked-string tone: instant attack, long ring, harmonics dying faster than the
        /// fundamental, with a touch of inharmonicity so it reads as a real string.
        /// </summary>
        private static void AddPluck(float[] d, float freq, float start, float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            int n = d.Length - s0;
            if (n <= 0) return;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float w = 2f * Mathf.PI * freq * t;
                float env = Mathf.Exp(-decay * t);
                float sample =
                    Mathf.Sin(w) +
                    0.42f * Mathf.Exp(-decay * 1.8f * t) * Mathf.Sin(w * 2.003f) +
                    0.22f * Mathf.Exp(-decay * 2.6f * t) * Mathf.Sin(w * 2.997f) +
                    0.10f * Mathf.Exp(-decay * 3.4f * t) * Mathf.Sin(w * 4.01f);
                d[s0 + i] += sample * amp * env;
            }
        }

        /// <summary>Pluck whose tail wraps past the end of the buffer — for seamless loops.</summary>
        private static void AddPluckWrapped(float[] d, float freq, float start, float amp,
            float decay)
        {
            int s0 = (int)(start * Rate);
            int n = (int)(Rate * 3f); // let the tail ring for up to three seconds
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float w = 2f * Mathf.PI * freq * t;
                float env = Mathf.Exp(-decay * t);
                float sample =
                    Mathf.Sin(w) +
                    0.42f * Mathf.Exp(-decay * 1.8f * t) * Mathf.Sin(w * 2.003f) +
                    0.22f * Mathf.Exp(-decay * 2.6f * t) * Mathf.Sin(w * 2.997f);
                d[(s0 + i) % d.Length] += sample * amp * env;
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

        /// <summary>One-pole lowpassed noise — thunder instead of hiss.</summary>
        private static void AddRumble(float[] d, float start, float dur, float amp, float decay)
        {
            int s0 = (int)(start * Rate);
            int n = Mathf.Min(d.Length - s0, (int)(dur * Rate));
            var rng = new System.Random(4321);
            float lp = 0f;
            const float k = 0.035f; // cutoff ~250 Hz at 44.1k
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float white = (float)rng.NextDouble() * 2f - 1f;
                lp += k * (white - lp);
                float env = Mathf.Exp(-decay * t);
                d[s0 + i] += lp * amp * env * 6f; // lowpass eats energy; make it back
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

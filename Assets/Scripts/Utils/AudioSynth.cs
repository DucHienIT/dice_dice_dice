using UnityEngine;

namespace DiceDiceDice
{
    /// <summary>Synthesizes every AudioClip in memory (no audio files), mirroring the design's SFX feel (spec section 25).</summary>
    public static class AudioSynth
    {
        private const int SampleRate = 44100;

        private enum Shape { Sine, Square, Triangle, Saw }

        private struct Note
        {
            public float Freq;
            public float Duration;
            public Shape Shape;
            public float Volume;
            public float Slide;
            public float Offset;

            public Note(float freq, float duration, Shape shape, float volume, float slide, float offset)
            {
                Freq = freq;
                Duration = duration;
                Shape = shape;
                Volume = volume;
                Slide = slide;
                Offset = offset;
            }
        }

        public static AudioClip CreateClip(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Coin:
                    return Render("sfx_coin",
                        new Note(880f, 0.09f, Shape.Square, 0.35f, 0f, 0f),
                        new Note(1320f, 0.12f, Shape.Square, 0.35f, 0f, 0.06f));
                case Sfx.Roll:
                    return Render("sfx_roll", new Note(160f, 0.12f, Shape.Triangle, 0.6f, -40f, 0f));
                case Sfx.Thud:
                    return Render("sfx_thud", new Note(90f, 0.15f, Shape.Sine, 0.8f, 0f, 0f));
                case Sfx.Buy:
                    return Render("sfx_buy",
                        new Note(520f, 0.08f, Shape.Triangle, 0.55f, 0f, 0f),
                        new Note(780f, 0.1f, Shape.Triangle, 0.55f, 0f, 0.07f));
                case Sfx.Error:
                    return Render("sfx_error", new Note(140f, 0.2f, Shape.Saw, 0.45f, 0f, 0f));
                case Sfx.Merge:
                    return Render("sfx_merge",
                        new Note(440f, 0.09f, Shape.Sine, 0.55f, 0f, 0f),
                        new Note(660f, 0.09f, Shape.Sine, 0.55f, 0f, 0.08f),
                        new Note(880f, 0.16f, Shape.Sine, 0.6f, 0f, 0.16f));
                case Sfx.Shoot:
                    return Render("sfx_shoot", new Note(600f, 0.04f, Shape.Square, 0.15f, 0f, 0f));
                case Sfx.Hit:
                    return Render("sfx_hit", new Note(220f, 0.05f, Shape.Square, 0.2f, 0f, 0f));
                case Sfx.Die:
                    return Render("sfx_die", new Note(300f, 0.12f, Shape.Saw, 0.28f, -160f, 0f));
                case Sfx.Boom:
                    return Render("sfx_boom", new Note(70f, 0.3f, Shape.Saw, 0.8f, -30f, 0f));
                case Sfx.LevelUp:
                    return Render("sfx_levelup",
                        new Note(523f, 0.14f, Shape.Triangle, 0.6f, 0f, 0f),
                        new Note(659f, 0.14f, Shape.Triangle, 0.6f, 0f, 0.09f),
                        new Note(784f, 0.14f, Shape.Triangle, 0.6f, 0f, 0.18f),
                        new Note(1046f, 0.14f, Shape.Triangle, 0.6f, 0f, 0.27f));
                case Sfx.Boss:
                    return Render("sfx_boss",
                        new Note(110f, 0.5f, Shape.Saw, 0.8f, -20f, 0f),
                        new Note(82f, 0.6f, Shape.Saw, 0.8f, -10f, 0.3f));
                case Sfx.Lose:
                    return Render("sfx_lose",
                        new Note(392f, 0.25f, Shape.Triangle, 0.65f, 0f, 0f),
                        new Note(330f, 0.25f, Shape.Triangle, 0.65f, 0f, 0.18f),
                        new Note(262f, 0.25f, Shape.Triangle, 0.65f, 0f, 0.36f),
                        new Note(196f, 0.35f, Shape.Triangle, 0.65f, 0f, 0.54f));
                case Sfx.Win:
                    return Render("sfx_win",
                        new Note(523f, 0.2f, Shape.Triangle, 0.65f, 0f, 0f),
                        new Note(659f, 0.2f, Shape.Triangle, 0.65f, 0f, 0.12f),
                        new Note(784f, 0.2f, Shape.Triangle, 0.65f, 0f, 0.24f),
                        new Note(1046f, 0.2f, Shape.Triangle, 0.65f, 0f, 0.36f),
                        new Note(1318f, 0.3f, Shape.Triangle, 0.65f, 0f, 0.48f));
                case Sfx.Zap:
                    return Render("sfx_zap", new Note(1200f, 0.08f, Shape.Saw, 0.28f, -400f, 0f));
                default:
                    return Render("sfx_default", new Note(440f, 0.1f, Shape.Sine, 0.4f, 0f, 0f));
            }
        }

        /// <summary>Ambient two-chord pad, sine-windowed so it loops without clicks.</summary>
        public static AudioClip CreateAmbientLoop()
        {
            const float duration = 8f;
            int samples = (int)(SampleRate * duration);
            var data = new float[samples];
            float[] chord = { 110f, 164.8f, 220f, 277.2f };
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float window = Mathf.Sin(Mathf.PI * i / samples);
                float value = 0f;
                for (int n = 0; n < chord.Length; n++)
                {
                    float wobble = 1f + 0.002f * Mathf.Sin(2f * Mathf.PI * 0.13f * t + n);
                    value += Mathf.Sin(2f * Mathf.PI * chord[n] * wobble * t) * 0.05f;
                }
                data[i] = value * window;
            }
            var clip = AudioClip.Create("ambient_pad", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Render(string clipName, params Note[] notes)
        {
            float total = 0f;
            for (int i = 0; i < notes.Length; i++)
            {
                total = Mathf.Max(total, notes[i].Offset + notes[i].Duration);
            }
            int samples = Mathf.CeilToInt(SampleRate * (total + 0.02f));
            var data = new float[samples];
            for (int n = 0; n < notes.Length; n++)
            {
                Note note = notes[n];
                int start = (int)(note.Offset * SampleRate);
                int count = (int)(note.Duration * SampleRate);
                float phase = 0f;
                for (int i = 0; i < count && start + i < samples; i++)
                {
                    float t = (float)i / count;
                    float freq = note.Freq + note.Slide * t;
                    phase += freq / SampleRate;
                    float envelope = Mathf.Exp(-5f * t);
                    data[start + i] += Oscillate(note.Shape, phase) * note.Volume * envelope * 0.25f;
                }
            }
            for (int i = 0; i < samples; i++)
            {
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }
            var clip = AudioClip.Create(clipName, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Oscillate(Shape shape, float phase)
        {
            float p = phase - Mathf.Floor(phase);
            switch (shape)
            {
                case Shape.Square: return p < 0.5f ? 1f : -1f;
                case Shape.Triangle: return 4f * Mathf.Abs(p - 0.5f) - 1f;
                case Shape.Saw: return 2f * p - 1f;
                default: return Mathf.Sin(2f * Mathf.PI * p);
            }
        }
    }
}

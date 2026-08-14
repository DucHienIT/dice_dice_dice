using System;
using System.Collections.Generic;
using I2.Loc;
using UnityEngine;

namespace Game.Localization
{
    /// <summary>
    /// Facade over I2 Localization — the only place the game touches I2. Every player-facing
    /// string is a term key resolved through here; the translations live in
    /// Assets/Localization/Localization.csv and are imported into
    /// Assets/Resources/I2Languages.asset by Tools &gt; Game &gt; Localization &gt; Import CSV.
    /// </summary>
    public static class Loc
    {
        public const string EnglishCode = "en";
        public const string VietnameseCode = "vi";

        /// <summary>
        /// Fires after the active language changed (one frame later — I2 batches the
        /// notification). Views re-pull every string they cached.
        /// </summary>
        public static event Action Changed;

        public static string CurrentCode => LocalizationManager.CurrentLanguageCode;

        /// <summary>
        /// Hooks I2's localize callback before the first scene loads. Re-subscribing is
        /// idempotent, which also covers entering play mode with domain reload disabled —
        /// I2 clears its own callback list when play mode exits.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            LocalizationManager.OnLocalizeEvent -= RaiseChanged;
            LocalizationManager.OnLocalizeEvent += RaiseChanged;
        }

        /// <summary>Translation for a term key. Missing keys return the key itself and warn.</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string text = LocalizationManager.GetTranslation(key);
            if (!string.IsNullOrEmpty(text)) return text;
            Debug.LogWarning("[Loc] Missing term: " + key);
            return key;
        }

        /// <summary>Translation of one random key out of a pool (narrative flavor lines).</summary>
        public static string Pick(string[] keys)
        {
            if (keys == null || keys.Length == 0) return string.Empty;
            return Get(keys[UnityEngine.Random.Range(0, keys.Length)]);
        }

        public static string Format(string key, object arg0) =>
            string.Format(Get(key), arg0);

        public static string Format(string key, object arg0, object arg1) =>
            string.Format(Get(key), arg0, arg1);

        public static string Format(string key, object arg0, object arg1, object arg2) =>
            string.Format(Get(key), arg0, arg1, arg2);

        public static string Format(string key, object arg0, object arg1, object arg2,
            object arg3) => string.Format(Get(key), arg0, arg1, arg2, arg3);

        /// <summary>Display name of a language in its own tongue, e.g. "Tiếng Việt".</summary>
        public static string LanguageLabel(string code) => Get(LocKeys.LanguagePrefix + code);

        /// <summary>Switches to the next language declared in the source, wrapping around.</summary>
        public static void CycleLanguage()
        {
            List<string> languages = LocalizationManager.GetAllLanguages();
            if (languages == null || languages.Count < 2) return;
            int index = languages.IndexOf(LocalizationManager.CurrentLanguage);
            LocalizationManager.CurrentLanguage = languages[(index + 1) % languages.Count];
        }

        private static void RaiseChanged() => Changed?.Invoke();
    }
}

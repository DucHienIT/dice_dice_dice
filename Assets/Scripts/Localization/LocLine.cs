namespace Game.Localization
{
    /// <summary>
    /// A narrative line kept as (term key + token substitutions) rather than a finished
    /// string, so the console can re-render the very same sentence in another language
    /// when the player switches. Struct: no allocation while it is passed around;
    /// only <see cref="Resolve"/> builds a string, and only at event time.
    /// </summary>
    public struct LocLine
    {
        public string Key;

        /// <summary>Render this line on its own row when appended after another one.</summary>
        public bool OnNewLine;

        private string _t0, _v0, _t1, _v1, _t2, _v2;

        public bool IsEmpty => string.IsNullOrEmpty(Key);

        public static LocLine Of(string key) => new LocLine { Key = key };

        /// <summary>Binds a token such as "{e}" to its runtime value. Up to three per line.</summary>
        public LocLine With(string token, string value)
        {
            if (_t0 == null) { _t0 = token; _v0 = value; }
            else if (_t1 == null) { _t1 = token; _v1 = value; }
            else { _t2 = token; _v2 = value; }
            return this;
        }

        public LocLine NextLine()
        {
            OnNewLine = true;
            return this;
        }

        public string Resolve()
        {
            if (IsEmpty) return string.Empty;
            string text = Loc.Get(Key);
            if (_t0 != null) text = text.Replace(_t0, _v0);
            if (_t1 != null) text = text.Replace(_t1, _v1);
            if (_t2 != null) text = text.Replace(_t2, _v2);
            return text;
        }
    }
}

using System.Text;

namespace CCQ.Utils
{
    /// <summary>
    /// Converts spec/demo narrative markup (*emphasis*, &lt;b&gt;, {token}) into TMP rich text.
    /// Event-time only, never per frame.
    /// </summary>
    public static class RichText
    {
        private const string EmOpen = "<color=#FFB454>";
        private const string EmClose = "</color>";
        private const string BoldOpen = "<color=#C9A6FF><b>";
        private const string BoldClose = "</b></color>";

        private static readonly StringBuilder Sb = new StringBuilder(256);

        public static string Format(string template)
        {
            Sb.Clear();
            bool emOpen = false;
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
                if (c == '*')
                {
                    Sb.Append(emOpen ? EmClose : EmOpen);
                    emOpen = !emOpen;
                }
                else if (c == '<' && Match(template, i, "<b>"))
                {
                    Sb.Append(BoldOpen);
                    i += 2;
                }
                else if (c == '<' && Match(template, i, "</b>"))
                {
                    Sb.Append(BoldClose);
                    i += 3;
                }
                else if (c == '<' && Match(template, i, "<br>"))
                {
                    Sb.Append('\n');
                    i += 3;
                }
                else
                {
                    Sb.Append(c);
                }
            }
            if (emOpen) Sb.Append(EmClose);
            return Sb.ToString();
        }

        public static string Replace(string template, string token, string value)
        {
            return template.Replace(token, value);
        }

        private static bool Match(string s, int at, string tag)
        {
            if (at + tag.Length > s.Length) return false;
            for (int i = 0; i < tag.Length; i++)
            {
                if (s[at + i] != tag[i]) return false;
            }
            return true;
        }
    }
}

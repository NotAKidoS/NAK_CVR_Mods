// ReSharper disable RedundantUsingDirective
// ReSharper disable RedundantNameQualifier
// ReSharper disable ReplaceWithFieldKeyword

using System.Text;

namespace NAK.CleanPlates.UI
{
    // TMP stops laying out at a C0 control character and everything after it is
    // silently invisible.
    public static class SafeText
    {
        public static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            int first = -1;
            for (int i = 0; i < value.Length; i++)
            {
                if (!IsStripped(value[i])) continue;
                first = i;
                break;
            }
            if (first < 0) return value;

            var builder = new System.Text.StringBuilder(value.Length);
            builder.Append(value, 0, first);
            for (int i = first; i < value.Length; i++)
                if (!IsStripped(value[i])) builder.Append(value[i]);

            return builder.ToString();
        }

        public static bool IsBlank(string value) => string.IsNullOrWhiteSpace(Clean(value));

        private static bool IsStripped(char c)
            => (c < ' ' && c != '\n' && c != '\t' && c != '\r')
               || c == '\u007F'
               || (c >= '\u0080' && c <= '\u009F');
    }
}
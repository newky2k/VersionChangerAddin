using System;

namespace DSoft.VersionChanger.Extensions
{
    public static class StringExtensions
    {
        public static bool CaseContains(this string originalString, string value, StringComparison comparisonMode)
        {
            return (originalString.IndexOf(value, comparisonMode) != -1);
        }

        public static string ValueForNode(this string source, string nodeName)
        {
            var openerText = $"<{nodeName}>";
            var closerText = $"</{nodeName}>";

            var pos = source.IndexOf(openerText);
            var closerPos = source.IndexOf(closerText);

            if (pos != -1 && closerPos != -1)
            {
                var sPos = pos + openerText.Length;
                var leng = closerPos - sPos;

                //find the value between the opne and close nodes
                var value = source.Substring(pos + openerText.Length, leng);

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value ;
                }
            }

            return "";
        }
    }
}

using System;

namespace DSoft.VersionChanger.Extensions
{
    public static class StringExtensions
    {
        public static bool CaseContains(this string originalString, string value, StringComparison comparisonMode)
        {
            return (originalString.IndexOf(value, comparisonMode) != -1);
        }
    }
}

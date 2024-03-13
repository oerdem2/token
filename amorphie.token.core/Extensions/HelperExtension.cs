using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace amorphie.token.core.Extensions
{
    public static class HelperExtensions
    {
        public static string ToTitleCase(this string input, string culture = "tr-TR")
        {
            var returnString = string.Empty;

            returnString += input.First().ToString().ToUpper(new CultureInfo(culture));
            foreach (char c in input.Substring(1, input.Length - 1))
            {
                returnString += c.ToString().ToLower(new CultureInfo(culture));
            }
            return returnString;
        }

        public static string ConvertTitleCase(this string input, string culture = "tr-TR")
        {
            var splittedInput = input.Split(" ");
            var returnString = string.Empty;
            foreach (var part in splittedInput)
            {
                returnString += part.ToTitleCase() + " ";
            }

            return returnString.Trim();
        }

    }
}
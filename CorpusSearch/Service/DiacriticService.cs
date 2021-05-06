﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Codex_API.Service
{
    public class DiacriticService
    {
        private static Dictionary<char, string> diacriticMap = new Dictionary<char, string>
        {
            ['á'] = "a",
            ['é'] = "e",
            ['í'] = "i",
            ['ó'] = "o",
            ['ú'] = "u",
            ['ẃ'] = "w",
            ['ï'] = "i",
            ['â'] = "a",
            ['ý'] = "y",
            ['ø'] = "o",
            ['ö'] = "o",
            ['ẁ'] = "w",
            ['ç'] = "c", // check this one: c or ch?
            ['ỳ'] = "y",
            ['ê'] = "e",
            ['ô'] = "o",
            ['ŷ'] = "y",
            ['ǎ'] = "a",
            ['ì'] = "i",
            ['ě'] = "e",
            ['ë'] = "e",
            ['ŵ'] = "w",
            ['û'] = "u",
            ['ò'] = "o",
            ['æ'] = "ae",
            ['ǔ'] = "u",
            ['œ'] = "oe",
            ['ù'] = "u",
            ['è'] = "e",
            ['ǒ'] = "o",
            ['ŕ'] = "r",
            ['ǐ'] = "i",
            ['à'] = "a",
            ['î'] = "i",
            ['ĵ'] = "j",
            ['ﬆ'] = "st",
        };

        private static readonly ILookup<string, char> reverseMap;

        static DiacriticService() 
        {
            reverseMap = diacriticMap.ToLookup(x => x.Value, x => x.Key);
        }

        /// <summary>
        /// TODO: This doesn't do st or ae
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static IList<string> Replace(char input)
        {
            var ret = diacriticMap.GetValueOrDefault(input, null);
            if (ret != null)
            {
                return new[] { ret };
            }

            return reverseMap[input.ToString()].Select(x => x.ToString()).ToList();

        }

        public static string Replace(string input)
        {
            StringBuilder output = new StringBuilder();

            foreach (var c in input)
            {
                output.Append(diacriticMap.GetValueOrDefault(c, c.ToString()));
            }

            return output.ToString();
        }
    }
}
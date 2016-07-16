namespace WikimediaProcessing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class WikimediaMarkup
    {
        /// <summary>
        /// Specifies whether parenthetical text should be removed when calling <see cref="RemoveMarkup"/>. As this text often contains IPA pronunciation, see-also text, or tangential information,
        /// it may be helpful to remove this text in some analyses.
        /// </summary>
        public static bool RemoveParentheticals = false;

        /// <summary>
        /// Approximately identifies articles that are Wikipedia "special" pages (eg, "Category:foo", "File:foo.jpg", "Wikipedia:something")
        /// </summary>
        public static readonly Regex SpecialPageRegex;
        private static readonly Regex RemoveParensRegex;
        private static readonly Regex LinkTemplateRegex;
        private static readonly IList<Tuple<Regex, string>> OrderedRegexes;
        static WikimediaMarkup()
        {
            RemoveParensRegex = PatternAndReplacement(@"\([^()]*\)", string.Empty).Item1;
            SpecialPageRegex = PatternAndReplacement(@"^[a-z]+:", string.Empty, RegexOptions.IgnoreCase).Item1;

            OrderedRegexes = new List<Tuple<Regex, string>>
            {
                // <ref>this is a reference</ref>
                PatternAndReplacement(@"(<ref[^>/]*/>)|(<ref[^>/]*>.*?</ref>)", string.Empty),
                            
                // link template
                PatternAndReplacement(@"{{l\|[^\|}]+\|([^}\|]+)(\|[^\|]*)*}}", "$1"),
                
                // Curlies, such as {{cite: ...}
                //PatternAndReplacement(@"{{.*?}}", string.Empty),
                PatternAndReplacement(@"{{[^{}]*?}}", string.Empty),

                // tables
                PatternAndReplacement(@"{\|.*?\|}", string.Empty),

                // internal links: [[Awakenings (book)|Awakenings]], [[Awakenings]]
                // specifically exclude from this pattern tags that can contain others; eg, [[File:foo.jpg|a photograph of [[Abraham Lincoln]].]]
                PatternAndReplacement(@"\[\[(?!(?:Category|File|Image):)(?:[^|\[\]]+?\|)?([^\[\]]+?)\]\]", "$1"),

                // internal links that we don't want to keep.
                PatternAndReplacement(@"\[\[(Category|File|Image):[^\[\]]+?\]\]", string.Empty),

                // external links with text: [http://foo "click here for foo"]
                PatternAndReplacement(@"\[[^\[\]]+(?: ([""']*)([^""\[\]]+)\1)\]", "$2"),

                // external links w/o text: [http://foo]
                PatternAndReplacement(@"\[[^ \[\]]+\]", string.Empty),

                // <!-- html comments -->
                PatternAndReplacement(@"<!--[^\>]*-->", string.Empty),

                // <html tags>
                PatternAndReplacement(@"<[^\>]*>", string.Empty),

                // ===Headings===
                PatternAndReplacement(@"(=+)([^'=]+)\1", "$2"),

                // ''Text formatting''
                PatternAndReplacement(@"(''+)(.*?)\1", "$2"),

                // * lists; :quotes
                PatternAndReplacement(@"^\s*[*:] (.+)$", "$1", RegexOptions.Compiled | RegexOptions.Multiline)
            };
        }

        /// <summary>
        /// Best-effort removal of wiki-markdown, HTML, and (possibly) parentheticals.
        /// </summary>
        /// <param name="rawWikipediaText">Raw wikipedia article text</param>
        /// <returns>Plaintext(ish) of the raw article</returns>
        public static string RemoveMarkup(string rawWikipediaText)
        {
            if (string.IsNullOrWhiteSpace(rawWikipediaText))
                return string.Empty;

            string trimmed = rawWikipediaText;

            if (RemoveParentheticals)
            {
                do
                {
                    trimmed = RemoveParensRegex.Replace(trimmed, string.Empty);
                } while (RemoveParensRegex.IsMatch(trimmed));
            }

            // remove everything after "See Also", "References", etc
            var sectionsToIgnore = new[] { "See also", "References", "Further reading", "External links" };
            foreach (var sectionToIgnore in sectionsToIgnore)
            {
                int index;
                if (
                    (index = trimmed.IndexOf("==" + sectionToIgnore + "==", StringComparison.InvariantCultureIgnoreCase)) !=
                    -1)
                {
                    trimmed = trimmed.Substring(0, index);
                }
            }

            // various fixes
            trimmed = Regex.Replace(trimmed, @"{{as of\|(\d+)}}", "as of $1");

            var agg = trimmed;

            foreach (var tup in OrderedRegexes)
            {
                do
                {
                    agg = tup.Item1.Replace(agg, tup.Item2).Trim();
                } while (tup.Item1.IsMatch(agg));
            }

            var paragraphs = agg
                .Trim()
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                ;

            return string.Join("\n\n", paragraphs);
        }

        private static Tuple<Regex, string> PatternAndReplacement(string pattern, string replacement, RegexOptions? options = null)
        {
            var optionsSet = options ?? RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.Singleline;

            return Tuple.Create(new Regex(pattern, optionsSet), replacement);
        }
    }
}

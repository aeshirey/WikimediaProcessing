namespace PlaintextWikipedia
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class WordFrequency
    {
        // TODO: this is subject to system RAM. for very large datasets (eg, the entire wikipedia dump),
        // this will crash. storage should be offloaded to something that can spill to disk gracefully.
        /// <summary>
        /// Calculate n-gram frequencies from a set of Wikipedia articles
        /// </summary>
        /// <param name="articles">A <see cref="WikipediaArticle"/>. Only the Plaintext property is relevant.</param>
        /// <param name="nGramSize">The size of the n-gram in words. Default is 1.</param>
        /// <param name="cutoff">The minimum frequency of an n-gram to be returned. Defaults to 10.</param>
        /// <returns>The set of all n-grams from <see cref="articles"/> and their raw frequencies.</returns>
        public static IList<KeyValuePair<string, long>> GetNGramFrequencies(IEnumerable<WikipediaArticle> articles, ushort nGramSize = 1, long cutoff = 10L)
        {
            var freqs = new Dictionary<string, long>(100000);

            foreach (var article in articles)
            {
                foreach (var section in GetSections(article.Plaintext))
                {
                    var words = section.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

                    for (var i = 0; i < words.Length - nGramSize + 1; i++)
                    {
                        var word = string.Join("\t", words, i, nGramSize);
                        
                        long n;
                        freqs.TryGetValue(word, out n);
                        freqs[word] = n + 1;
                    }
                }
            }

            var freqsCutoff = freqs
                .Where(kvp => kvp.Value >= cutoff)
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            return freqsCutoff;
        }

        /// <summary>
        /// Given some text, split it on various punctuation that will delineate related text.
        /// </summary>
        /// <param name="markupRemoved">Plaintext output from <see cref="MarkupRemoval.RemoveMarkup"/></param>
        /// <returns>An IEnumerable of semi-independent strings of words</returns>
        private static IEnumerable<string> GetSections(string markupRemoved)
        {
            var pattern = @"[,""\(\).?–]";

            return Regex.Split(markupRemoved, pattern)
                .Select(s => s.Trim())
                .Where(s => !String.IsNullOrEmpty(s))
                ;
        }
    }
}

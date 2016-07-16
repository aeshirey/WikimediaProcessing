using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SQLite;
using WikimediaProcessing;

namespace NGrams
{
    public static class WordFrequency
    {
        private const int ArticleBatchSize = 10000;

        private class TermFrequency
        {
            [PrimaryKey]
            public string Term { get; set; }
            public uint Frequency { get; set; }
        }

        private class Progress
        {
            [PrimaryKey]
            public int ArticleId { get; set; }
            public string Title { get; set; }
        }

        /// <summary>
        /// Calculate n-gram frequencies from a set of Wikipedia articles
        /// </summary>
        /// <param name="articles">A <see cref="WikipediaArticle"/>. Only the Plaintext property is relevant.</param>
        /// <param name="nGramSize">The size of the n-gram in words. Default is 1.</param>
        /// <param name="dbFilename">An optional location for the database file, if you want it preserved</param>
        /// <returns>The set of all n-grams from <see cref="articles"/> and their raw frequencies.</returns>
        public static void CalculateNGramFrequencies(IEnumerable<WikimediaPage> articles, string dbFilename, ushort nGramSize = 1)
        {
            IList<KeyValuePair<string, uint>> ret;
            using (var db = CreateDb(dbFilename))
            {
                var freqs = new Dictionary<string, uint>(6 * ArticleBatchSize);

                try
                {
                    // had we previously processed something?
                    var articleCount = db.Table<Progress>().Any()
                        ? db.Table<Progress>().Max(rec => rec.ArticleId)
                        : 0;

                    foreach (var article in articles.Skip(articleCount))
                    {
                        foreach (var section in GetSections(article.Plaintext))
                        {
                            var words =
                                section.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

                            for (var i = 0; i < words.Length - nGramSize + 1; i++)
                            {
                                var word = string.Join("\t", words, i, nGramSize);

                                uint n;
                                freqs.TryGetValue(word, out n);
                                freqs[word] = n + 1;
                            }
                        }

                        if (++articleCount % ArticleBatchSize == 0)
                        {
                            // having collected a sufficient number of articles in-memory, spill them to our SQLite database
                            UpdateDb(db, freqs, new Progress { ArticleId = articleCount, Title = article.Title });
                            freqs = new Dictionary<string, uint>(6 * ArticleBatchSize);
                        }
                    }

                    // one last flush to the db
                    if (freqs.Any())
                    {
                        UpdateDb(db, freqs, new Progress { ArticleId = articleCount, Title = "[COMPLETED]" });
                    }
                }
                catch (IOException e)
                {
                    // handle when we've gone to sleep
                }

                db.Close();
            }
        }

        /// <summary>
        /// Return the n-gram frequencies previously calculated
        /// </summary>
        /// <param name="dbFilename">The location of the database file</param>
        /// <param name="cutoff">The minimum frequency of an n-gram to be returned. Defaults to 10.</param>
        public static IEnumerable<KeyValuePair<string, uint>> GetNGramFrequencies(string dbFilename, uint cutoff = 10)
        {
            using (var db = CreateDb(dbFilename))
            {
                var rows = db.Table<TermFrequency>()
                    .Where(row => row.Frequency >= cutoff)
                    .OrderByDescending(row => row.Frequency);

                foreach (var row in rows)
                {
                    yield return new KeyValuePair<string, uint>(row.Term, row.Frequency);
                }
            }
        }

        private static SQLiteConnection CreateDb(string dbFile)
        {
            var conn = new SQLiteConnection(dbFile);
            conn.CreateTable<TermFrequency>();
            conn.CreateIndex<TermFrequency>(t => t.Term, true);

            conn.CreateTable<Progress>();
            return conn;
        }

        private static void UpdateDb(SQLiteConnection conn, Dictionary<string, uint> counts, Progress progress)
        {
            conn.BeginTransaction();
            foreach (var kvp in counts)
            {
                var term = conn.Table<TermFrequency>().Where(row => row.Term == kvp.Key).FirstOrDefault();

                if (term == null)
                {
                    conn.Insert(new TermFrequency { Term = kvp.Key, Frequency = kvp.Value });
                }
                else
                {
                    conn.Update(new TermFrequency { Term = kvp.Key, Frequency = kvp.Value + term.Frequency });
                }
            }

            conn.Insert(progress);
            conn.Commit();

            Console.WriteLine("Committed {0} ({1}) @ {2}", progress.ArticleId, progress.Title, DateTime.Now);
        }

        /// <summary>
        /// Given some text, split it on various punctuation that will delineate related text.
        /// </summary>
        /// <param name="markupRemoved">Plaintext output from <see cref="WikimediaMarkup.RemoveMarkup"/></param>
        /// <returns>An IEnumerable of semi-independent strings of words</returns>
        private static IEnumerable<string> GetSections(string markupRemoved)
        {
            const string pattern = @"[,""\(\).?–;\n\r\t]";

            return Regex.Split(markupRemoved, pattern, RegexOptions.Multiline | RegexOptions.Singleline)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                ;
        }
    }
}

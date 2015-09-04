namespace NGrams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using PlaintextWikipedia;

    class Program
    {
        private static string inputFile;
        private static string outputFile;
        private static int articleLimit;
        private static ushort nGramSize = 1;
        private static long cutoff = 10L;

        private static bool ParseArgs(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-in" && i + 1 <= args.Length)
                {
                    inputFile = args[++i];

                    if (!File.Exists(inputFile))
                    {
                        Console.WriteLine("Could not find input file '{0}'", inputFile);
                        return false;
                    }

                    if (!inputFile.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase)
                        && !inputFile.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Console.WriteLine("ProcessWP currently only handles extracted XML files.");
                        Console.WriteLine("Download and extract the .xml.bz2 file from https://dumps.wikimedia.org/enwiki/");
                        return false;
                    }
                }
                else if (args[i].ToLower() == "-out" && i + 1 <= args.Length)
                {
                    outputFile = args[++i];
                }
                else if (args[i].ToLower() == "-articles" && i + 1 <= args.Length)
                {
                    if (!int.TryParse(args[++i], out articleLimit))
                    {
                        Console.WriteLine("Invalid article limit ({0}) specified. Defaulting to all.");
                        articleLimit = -1;
                    }
                }
                else if (args[i].ToLower() == "-n" && i + 1 <= args.Length)
                {
                    if (!ushort.TryParse(args[++i], out nGramSize))
                    {
                        Console.WriteLine("Invalid NGram size ({0}) specified. Defaulting to 1.");
                    }
                }
                else if (args[i].ToLower() == "-cutoff" && i + 1 <= args.Length)
                {
                    if (!long.TryParse(args[++i], out cutoff))
                    {
                        Console.WriteLine("Invalid cutoff threshold ({0}) specified. Defaulting to 10.");
                    }
                }
                else if (args[i].ToLower() == "-removeparens")
                {
                    WikipediaMarkup.RemoveParentheticals = true;
                }
            }

            return !string.IsNullOrEmpty(inputFile) && !string.IsNullOrEmpty(outputFile);
        }

        static void Main(string[] args)
        {
            if (!ParseArgs(args))
            {
                return;
            }

            var startTime = DateTime.Now;

            IEnumerable<WikipediaArticle> articles;

            if (inputFile.EndsWith(".xml", StringComparison.CurrentCultureIgnoreCase))
            {
                articles = WikipediaArticle.ReadArticlesFromXmlDump(inputFile)
                    .Where(article => !article.IsDisambiguation && !article.IsRedirect && !article.IsSpecialPage);
            }
            else
            {
                articles = WikipediaArticle.ReadFromDisk(inputFile);
            }

            if (articleLimit > 0)
            {
                articles = articles.Take(articleLimit);
            }

            using (var fh = new StreamWriter(outputFile))
            {
                foreach (var kvp in WordFrequency.GetNGramFrequencies(articles, nGramSize, cutoff))
                {
                    fh.WriteLine("{0}\t{1}", kvp.Key, kvp.Value);
                }
            }

            var endTime = DateTime.Now;

            TimeSpan processTime = endTime - startTime;
            Console.WriteLine("Process took " + processTime);
        }
    }
}

namespace ProcessWP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using PlaintextWikipedia;

    class Program
    {
        private static string inputFile;
        private static string outputFile;
        private static string title;
        private static int articleLimit = -1;
        private static bool rawText = false;

        static void Usage()
        {
            Console.WriteLine();
            Console.WriteLine("Batch usage:");
            Console.WriteLine("  ProcessWP.exe -in dump.xml -out output.dat [-articles ###] [-removeparens]");
            Console.WriteLine("    -articles ### specifies that only ### articles should be kept. Default is every article. Must be integer.");
            Console.WriteLine();

            Console.WriteLine("Single article usage:");
            Console.WriteLine("  ProcessWP.exe -in dump.xml -out output.txt -title 'article title' [-raw] [-removeparens]");
            Console.WriteLine("    -title specifies which article title to find");
            Console.WriteLine("    -raw retrieves the raw (Wikimedia markup + HTML) instead of the plaintext version");
            Console.WriteLine();

            Console.WriteLine("In either case, -removeparens removes parenthetical text Omitting this retains parens.");
        }

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
                else if (args[i].ToLower() == "-removeparens")
                {
                    WikipediaMarkup.RemoveParentheticals = true;
                }
                else if (args[i].ToLower() == "-title" && i + 1 <= args.Length)
                {
                    title = args[++i];
                }
                else if (args[i].ToLower() == "-raw")
                {
                    rawText = true;
                }
            }

            return !string.IsNullOrEmpty(inputFile) && !string.IsNullOrEmpty(outputFile);
        }

        static void Main(string[] args)
        {
            if (!ParseArgs(args))
            {
                Usage();
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

            if (!string.IsNullOrEmpty(title))
            {
                var targetArticle =
                    articles.FirstOrDefault(
                        article => article.Title.Equals(title, StringComparison.CurrentCultureIgnoreCase));

                if (targetArticle == null)
                {
                    Console.WriteLine("Could not find article '{0}'", title);
                    return;
                }

                File.WriteAllText(outputFile, rawText ? targetArticle.Text : targetArticle.Plaintext);
            }
            else
            {
                if (articleLimit > 0)
                {
                    articles = articles.Take(articleLimit);
                }

                var numberOfArticles = WikipediaArticle.WriteToDisk(articles, outputFile);
                Console.WriteLine("Wrote {0} articles to disk.", numberOfArticles);
            }

            var endTime = DateTime.Now;

            TimeSpan processTime = endTime - startTime;
            Console.WriteLine("Process took " + processTime);
        }
    }
}

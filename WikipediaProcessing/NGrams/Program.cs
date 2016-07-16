namespace NGrams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using WikimediaProcessing;

    internal class Program
    {
        private static string inputFile;
        private static string dbFile;
        private static string outputFile;
        private static int articleLimit;
        private static ushort nGramSize = 1;
        private static uint cutoff = 10;

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
                        Console.WriteLine(
                            "Download and extract the .xml.bz2 file from https://dumps.wikimedia.org/enwiki/");
                        return false;
                    }
                }
                else if (args[i].ToLower() == "-out" && i + 1 <= args.Length)
                {
                    outputFile = args[++i];
                }
                else if (args[i].ToLower() == "-db" && i + 1 <= args.Length)
                {
                    dbFile = args[++i];
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
                    if (!uint.TryParse(args[++i], out cutoff))
                    {
                        Console.WriteLine("Invalid cutoff threshold ({0}) specified. Defaulting to 10.");
                    }
                }
                else if (args[i].ToLower() == "-removeparens")
                {
                    WikimediaMarkup.RemoveParentheticals = true;
                }
            }

            return !(string.IsNullOrEmpty(inputFile) && (string.IsNullOrEmpty(dbFile) || string.IsNullOrEmpty(outputFile)));
        }

        private static void Main(string[] args)
        {
            if (!ParseArgs(args))
            {
                Console.WriteLine("NGrams.exe [-in input.(xml|dat)] [-db frequencies.db] [-out frequencies.txt]");
                Console.WriteLine("At least two of the input files must be present:");
                Console.WriteLine("   -in specifies the Wikipedia plaintext dump location");
                Console.WriteLine("   -db specifies the location of processed n-gram frequencies.");
                Console.WriteLine("        When used with -in, the input file will be processed into -db");
                Console.WriteLine("        When used without -in, assume frequencies exist and read from this db");
                Console.WriteLine("   -out specifies the plaintext TSV that should contain the database dump");
                return;
            }

            if (string.IsNullOrEmpty(inputFile) && !string.IsNullOrEmpty(dbFile))
            {
                // process existing DB into frequency counts
                DumpFrequenciesToDisk(WordFrequency.GetNGramFrequencies(dbFile, cutoff), outputFile);
            }
            else
            {
                var wm = new Wikimedia(inputFile);
                var articles = wm.Articles()
                        .Where(article => !article.IsDisambiguation && !article.IsRedirect && !article.IsSpecialPage);

                if (articleLimit > 0)
                {
                    articles = articles.Take(articleLimit);
                }

                if (string.IsNullOrEmpty(dbFile))
                {
                    // don't care about the db; just use a temp file
                    dbFile = Path.GetTempFileName();
                }

                Console.WriteLine("Beginning n-gram calculation");
                var startTime = DateTime.Now;
                WordFrequency.CalculateNGramFrequencies(articles, dbFile, nGramSize);
                var endTime = DateTime.Now;

                Console.WriteLine("Calculation took " + (endTime - startTime));

                if (!string.IsNullOrEmpty(outputFile))
                {
                    Console.WriteLine("Writing frequencies to disk");

                    startTime = DateTime.Now;
                    DumpFrequenciesToDisk(WordFrequency.GetNGramFrequencies(dbFile, cutoff), outputFile);
                    endTime = DateTime.Now;

                    Console.WriteLine("Dump to disk took " + (endTime - startTime));
                }
            }
        }

        private static void DumpFrequenciesToDisk(IEnumerable<KeyValuePair<string, uint>> frequencies, string filename)
        {

            using (var fh = new StreamWriter(filename))
            {
                foreach (var kvp in frequencies)
                {
                    fh.WriteLine("{0}\t{1}", kvp.Key, kvp.Value);
                }
            }
        }
    }
}

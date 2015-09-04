namespace PlaintextWikipedia
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Loosely describes a Wikipedia article
    /// </summary>
    public class WikipediaArticle
    {
        /// <summary>
        /// The Wikipedia article title
        /// </summary>
        public string Title;

        /// <summary>
        /// The raw wiki-markdown of the article (if available)
        /// </summary>
        public string Text;
        private string _Plaintext;

        /// <summary>
        /// Indicates whether the article is a redirect to another article.
        /// </summary>
        public bool IsRedirect
        {
            get { return Text.StartsWith("#REDIRECT", true, CultureInfo.CurrentCulture); }
        }

        /// <summary>
        /// Indicates whether the article is a special page, loosely defined as one starting with alphas followed by a colon (eg, "Category:foo", "File:foo.jpg", "Wikipedia:something").
        /// </summary>
        public bool IsSpecialPage
        {
            get { return WikipediaMarkup.SpecialPageRegex.IsMatch(Title); }
        }

        /// <summary>
        /// Indicates whether the page is a disambiguation page.
        /// </summary>
        public bool IsDisambiguation
        {
            get { return Title.Contains("(disambiguation)"); }
        }

        /// <summary>
        /// The (approximately) plaintext version of the raw article.
        /// </summary>
        public string Plaintext
        {
            get { return _Plaintext ?? (_Plaintext = WikipediaMarkup.RemoveMarkup(Text)); }
        }

        /// <summary>
        /// Writes a set of <see cref="WikipediaArticle"/>s to disk in a simple binary format consisting of the article title and the plaintext contents.
        /// </summary>
        /// <param name="articles">A set of articles, probably from <see cref="ReadArticlesFromXmlDump"/></param>
        /// <param name="outputFilename">The filename into which articles should be saved</param>
        /// <returns>The number of articles written</returns>
        public static int WriteToDisk(IEnumerable<WikipediaArticle> articles, string outputFilename)
        {
            var numberOfArticles = 0;
            using (var fh = File.Create(outputFilename))
            using (var bh = new BinaryWriter(fh))
            {
                int i = 0;
                foreach (var article in articles)
                {
                    bh.Write(article.Title);
                    bh.Write(article.Plaintext);
                    ++numberOfArticles;
                }
            }

            return numberOfArticles;
        }

        /// <summary>
        /// Reads plaintext articles (ie, from <see cref="WriteToDisk"/>) from disk, keeping only the title and plaintext.
        /// </summary>
        /// <param name="inputFilename">The file from which to read.</param>
        /// <returns>An IEnumerable of articles.</returns>
        public static IEnumerable<WikipediaArticle> ReadFromDisk(string inputFilename)
        {
            using (var fh = File.OpenRead(inputFilename))
            using (var bh = new BinaryReader(fh, Encoding.UTF8))
            {
                while (fh.Position != fh.Length)
                {
                    var title = bh.ReadString();
                    var plaintext = bh.ReadString();

                    yield return new WikipediaArticle() { Title = title, _Plaintext = plaintext };
                }
            }
        }

        /// <summary>
        /// Reads articles from a Wikipedia dump. The file currently must be BUnzipped. XML is assumed to be valid. <seealso cref="https://dumps.wikimedia.org/enwiki/"/>
        /// </summary>
        /// <param name="filename">An unzipped Wikipedia dump</param>
        /// <returns>An IEnumerable of articles from the XML file</returns>
        public static IEnumerable<WikipediaArticle> ReadArticlesFromXmlDump(string filename)
        {
            var settings = new XmlReaderSettings()
            {
                ValidationType = ValidationType.None,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            XmlReader x = XmlTextReader.Create(new StreamReader(filename), settings);

            while (x.ReadToFollowing("page"))
            {
                if (x.NodeType == XmlNodeType.Element)
                {
                    var article = ReadArticle(x);
                    yield return article;
                }
            }
        }

        /// <summary>
        /// Read a single article from the XML dump
        /// </summary>
        /// <param name="x">Previously opened Xmlreader</param>
        /// <returns>One article containing the title and wiki-markdown + HTML content.</returns>
        private static WikipediaArticle ReadArticle(XmlReader x)
        {
            var ret = new WikipediaArticle();

            x.ReadToFollowing("title");
            ret.Title = x.ReadString();

            x.ReadToFollowing("text");
            ret.Text = x.ReadElementContentAsString();
            
            return ret;
        }
    }
}

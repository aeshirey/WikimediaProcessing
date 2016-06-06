using PlaintextWikipedia;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;

namespace Plaintext
{
    public class WikiSection
    {
        public string SectionName { get; set; }

        public string Content { get; set; }

        public ICollection<WikiSection> SubSections { get; set; }
    }

    public class WiktionaryArticle
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

        public IList<WikiSection> Languages;

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

        public bool HasLanguage(string language)
        {
            if (Languages == null)
            {
                Languages = ParseSection(string.Empty, Text)
                    .SubSections
                    .ToList();
            }

            return Languages.Any(l => l.SectionName == language);
        }

        private WikiSection ParseSection(string title, string markup, int level = 2 /* language starts at two dashes (eg, '==English==') */ )
        {
            const string RawPattern = "^={{{0}}}([^=]+?)={{{0}}}[\r\n]+";
            var pattern = string.Format(RawPattern, level);

            var matches = Regex.Matches(markup, pattern, RegexOptions.Multiline);

            if (matches.Count == 0)
            {
                return new WikiSection
                {
                    SectionName = title,
                    Content = markup,
                    SubSections = new List<WikiSection>()
                };
            }

            var section = new WikiSection
            {
                SectionName = title,
                SubSections = new List<WikiSection>()
            };

            if (matches[0].Index > 0)
            {
                section.Content = markup.Substring(0, matches[0].Index);
            }

            for (var i = 0; i < matches.Count; i++)
            {
                var subsectionName = matches[i].Groups[1].Value;

                var contentStartIndex = matches[i].Length + matches[i].Index;

                var subsectionContent = i == matches.Count - 1
                    ? markup.Substring(contentStartIndex) // last match, just take everything else
                    : markup.Substring(contentStartIndex, matches[i + 1].Index - contentStartIndex); // take everything from this match (after the heading) to the beginning of the next match

                section.SubSections.Add(ParseSection(subsectionName, subsectionContent, level + 1));
            }

            return section;
        }


        /// <summary>
        /// Writes a set of <see cref="WiktionaryArticle"/>s to disk in a simple binary format consisting of the article title and the plaintext contents.
        /// </summary>
        /// <param name="articles">A set of articles, probably from <see cref="ReadArticlesFromXmlDump"/></param>
        /// <param name="outputFilename">The filename into which articles should be saved</param>
        /// <returns>The number of articles written</returns>
        public static int WriteToDisk(IEnumerable<WiktionaryArticle> articles, string outputFilename)
        {
            var numberOfArticles = 0;
            using (var fh = File.Create(outputFilename))
            using (var bh = new BinaryWriter(fh))
            {
                var jss = new JavaScriptSerializer();
                foreach (var article in articles)
                {
                    var json = jss.Serialize(article);

                    bh.Write(json);
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
        public static IEnumerable<WiktionaryArticle> ReadFromDisk(string inputFilename)
        {
            using (var fh = File.OpenRead(inputFilename))
            using (var bh = new BinaryReader(fh, Encoding.UTF8))
            {
                var jss = new JavaScriptSerializer();
                while (fh.Position != fh.Length)
                {
                    var json = bh.ReadString();

                    var article = jss.Deserialize<WiktionaryArticle>(json);

                    yield return article;
                }
            }
        }

        /// <summary>
        /// Reads articles from a Wikipedia dump. The file currently must be BUnzipped. XML is assumed to be valid. <seealso cref="https://dumps.wikimedia.org/enwiki/"/>
        /// </summary>
        /// <param name="filename">An unzipped Wikipedia dump</param>
        /// <returns>An IEnumerable of articles from the XML file</returns>
        public static IEnumerable<WiktionaryArticle> ReadArticlesFromXmlDump(string filename)
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
                    yield return ReadArticle(x);
                }
            }
        }

        /// <summary>
        /// Read a single article from the XML dump
        /// </summary>
        /// <param name="x">Previously opened Xmlreader</param>
        /// <returns>One article containing the title and wiki-markdown + HTML content.</returns>
        private static WiktionaryArticle ReadArticle(XmlReader x)
        {
            x.ReadToFollowing("title");
            var title = x.ReadString();

            x.ReadToFollowing("text");
            var text = x.ReadElementContentAsString();

            return new WiktionaryArticle
            {
                Title = title,
                Text = text
            };
        }
    }
}

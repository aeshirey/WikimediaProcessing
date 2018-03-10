using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace WikimediaProcessing
{
    public class Wikimedia
    {
        private readonly Stream input;
        private readonly XmlReader xmlReader;
        private readonly XmlReaderSettings settings = new XmlReaderSettings()
        {
            ValidationType = ValidationType.None,
            ConformanceLevel = ConformanceLevel.Fragment
        };

        public Wikimedia(string filename)
        {
            var fileStream = File.OpenRead(filename);

            if (filename.EndsWith(".xml.bz2"))
            {
                input = new BZip2InputStream(fileStream);
                xmlReader = XmlTextReader.Create(input, settings);

                // TODO: We can create the XmlTextReader, but upon calling xmlReader.ReadToFollowing, a bz2 file fails to read.
                throw new NotImplementedException("BZip2 reading currently does not work.");
            }
            else if (filename.EndsWith(".xml"))
            {
                input = fileStream;
                xmlReader = XmlTextReader.Create(input, settings);
            }
            else
            {
                input = fileStream;
            }
        }

        /// <summary>
        /// Reads articles one by one from an opened stream. The XML must be directly accessible from this stream
        /// </summary>
        public IEnumerable<WikimediaPage> Articles
        {
            get
            {
                if (xmlReader != null)
                {
                    while (xmlReader.ReadToFollowing("page"))
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element)
                        {
                            yield return ReadArticle(xmlReader);
                        }
                    }
                }
                else
                {
                    using (var bh = new BinaryReader(input, Encoding.UTF8))
                    {
                        while (input.Position != input.Length)
                        {
                            var json = bh.ReadString();

                            var article = JsonConvert.DeserializeObject<WikimediaPage>(json);

                            yield return article;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read a single article from the XML dump
        /// </summary>
        /// <param name="x">Previously opened Xmlreader</param>
        /// <returns>One article containing the title and wiki-markdown + HTML content.</returns>
        private static WikimediaPage ReadArticle(XmlReader x)
        {
            x.ReadToFollowing("title");
            var title = x.ReadString();

            x.ReadToFollowing("text");
            var text = x.ReadElementContentAsString();

            return new WikimediaPage
            {
                Title = title,
                Text = text
            };
        }


        /// <summary>
        /// Writes a set of <see cref="WikimediaPage"/>s to disk in a simple binary format consisting of the article title and the plaintext contents.
        /// </summary>
        /// <param name="articles">A set of articles, probably from <see cref="ReadArticlesFromXmlDump"/></param>
        /// <param name="outputFilename">The filename into which articles should be saved</param>
        /// <returns>The number of articles written</returns>
        public static int WriteToDisk(IEnumerable<WikimediaPage> articles, string outputFilename)
        {
            var numberOfArticles = 0;
            using (var fh = File.Create(outputFilename))
            using (var bh = new BinaryWriter(fh))
            {
                foreach (var article in articles)
                {
                    var json = JsonConvert.SerializeObject(article);

                    bh.Write(json);
                    ++numberOfArticles;
                }
            }

            return numberOfArticles;
        }
    }
}

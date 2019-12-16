using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WikimediaProcessing
{
    public class WikimediaPage
    {
        /// <summary>
        /// The Wikipedia article id
        /// </summary>
        public int Id { get; set; }

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
            get { return WikimediaMarkup.SpecialPageRegex.IsMatch(Title); }
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
            get { return _Plaintext ?? (_Plaintext = WikimediaMarkup.RemoveMarkup(Text)); }
        }


        private ICollection<WikiSection> _Sections;
        public ICollection<WikiSection> Sections
        {
            get
            {
                return _Sections ?? (_Sections = ParseSection(string.Empty, Text, 2).SubSections);
            }
        }


        /// <summary>
        /// Derive the structure of a Wikimedia page by its sections, subsections, and down the hierarchy.
        /// </summary>
        /// <param name="title">The title of the section being parsed</param>
        /// <param name="markup">The Wikimedia markup to be parsed</param>
        /// <param name="level">The numeric level of the hierarchy (eg, "==History==" is at the second level)</param>
        /// <returns>A tree of WikiSections</returns>
        internal WikiSection ParseSection(string title, string markup, int level = 2 /* language starts at two dashes (eg, '==English==') */ )
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikimediaProcessing
{
    public class WikiSection
    {
        private string _Plaintext;

        public string SectionName { get; set; }

        public string Content { get; set; }

        public ICollection<WikiSection> SubSections { get; set; }

        public string Plaintext
        {
            get { return _Plaintext ?? (_Plaintext = WikimediaMarkup.RemoveMarkup(Content)); }
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Content))
                return SectionName;

            if (Content.Length < 20)
                return SectionName + ": " + Content;

            return SectionName + ": " + Content.Substring(0, 20) + "...";
        }
    }
}

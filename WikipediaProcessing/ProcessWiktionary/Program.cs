using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Plaintext;

namespace ProcessWiktionary
{
    class Program
    {
        static void Main(string[] args)
        {
            var dump = @"C:\Users\Adam\Downloads\enwiktionary-20160601-pages-articles-multistream.xml";

            //var x = WiktionaryArticle.ReadArticlesFromXmlDump(dump)
            //    .Where(article => !article.IsSpecialPage && !article.IsRedirect && !article.IsDisambiguation)
            //    .Where(article => article.HasLanguage("French"))
            //    .Select(article =>
            //        new WiktionaryArticle
            //        {
            //            Text = article.Text,
            //            Title = article.Title,
            //            Languages = article.Languages.Where(a => a.SectionName.Contains("French")).ToList()
            //        });

            //WiktionaryArticle.WriteToDisk(x, @"C:\users\Adam\Desktop\FrenchWiktionaryArticles.dat");

            var x = WiktionaryArticle.ReadFromDisk(@"C:\users\Adam\Desktop\FrenchWiktionaryArticles.dat")
                .Take(100)
                .ToArray();
        }


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WikimediaProcessing;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            GetMotorcycleArticles();
        }


        private static void GetMotorcycleArticles()
        {
            var wm = new Wikimedia("enwiki-20160701-pages-articles-multistream.xml");

            var articles = wm.Articles()
                .Where(article => article.Text.Contains("{{Infobox Motorcycle"));

            Wikimedia.WriteToDisk(articles, "motorcycles.dat");
        }
    }
}

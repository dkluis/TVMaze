using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common_Lib;
using HtmlAgilityPack;

namespace Web_Lib
{
    public class WebScrape : IDisposable
    {
        Logger log;
        private List<string> magnets = new();
        private Common common = new();

        public WebScrape(Logger logger)
        {
            log = logger;
        }

        public void Dispose()
        {
            // throw new NotImplementedException();
            this.Dispose();
        }

        #region Getters

        public List<string> GetMagnetTVShowEpisode(string showname, string episode)
        {
            magnets = new();
            // Figure out if the episode is the first one of a season, then process TVShowSeason -> if nothing found process the episode
            if (common.ReturnEpisode(episode) == 1)
            {
                string season = "S" + common.ReturnEpisode(episode).ToString().PadLeft(2, '0');
                Console.WriteLine($"Season is {season}");
                magnets = GetMagnetTVShowSeason(showname, season);
            }
            
            return magnets;
        }

        public List<string> GetMagnetTVShowSeason(string showname, string season)
        {

            return magnets;
        }

        #endregion

        #region Web Scrapping Test

        public List<string> TestWebScrap()
        {
            string html = "https://eztv.re/search/dcs-legends-of-tomorrow-s06e01";
            HtmlWeb web = new HtmlWeb();
            List<string> magnets = new();

            HtmlDocument htmlDoc = web.Load(html);
            //log.Write($"HTML Doc: {htmlDoc.ParsedText}", "WebScrape", 0, false);

            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            foreach (HtmlNode node in table)
            {
                if (node.Attributes["href"].Value.Contains("magnet:"))
                {
                    magnets.Add(node.Attributes["href"].Value);
                    log.Write($"Attribute HREF " + node.Attributes["href"].Value, "Test Web Scrap", 0);
                }
            }
            return magnets;
        }

        #endregion

    }
}

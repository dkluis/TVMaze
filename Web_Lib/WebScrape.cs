using Common_Lib;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;

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
        }

        #region Getters

        public List<string> GetMagnetTVShowEpisode(string showname, int seas_num, int epi_num)
        {
            magnets = new();
            //EZTV 
            if (epi_num == 1)
            {
                // Process the whole season
                GetEZTVMagnets(showname, seas_num, epi_num, true);
                // Get all other website magnets for whole season
                if (magnets.Count > 0)
                {
                    return magnets;
                }
            }
            // Process the season/episode
            GetEZTVMagnets(showname, seas_num, epi_num, false);
            // Get all other website magnets by episode
            return magnets;
        }

        private void GetEZTVMagnets(string showname, int seas_num, int epi_num, bool season_only)
        {
            string html_base = "https://eztv.re/search/";
            string html = html_base + BuildEztvURL(showname, seas_num, epi_num, season_only);

            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(html);
            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            foreach (HtmlNode node in table)
            {
                if (node.Attributes["href"].Value.Contains("magnet:"))
                {
                    magnets.Add(node.Attributes["href"].Value);
                    log.Write($"Attribute HREF " + node.Attributes["href"].Value, "Test Web Scrap", 0);
                }
            }
        }

        private string BuildEztvURL(string showname, int seas_num, int epi_num, bool season_only = false)
        {
            string eztv_url = "https://eztv.re/search/";
            showname = common.RemoveSpecialCharsInShowname(showname);
            showname = showname.Replace(" ", "-");  //eztv seach char.
            showname = showname + "S" + seas_num.ToString().PadLeft(2, '0');
            if (!season_only)
            {
                showname = showname + "E" + epi_num.ToString().PadLeft(2, '0');
            }
            return eztv_url;
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

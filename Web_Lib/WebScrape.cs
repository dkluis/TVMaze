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
        public bool WholeSeasonFound;

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
                GetEZTVMagnets(showname, seas_num, epi_num);
                // Get all other website magnets for whole season
                if (magnets.Count > 0)
                {
                    return magnets;
                }
            }
            // Process the season/episode
            GetEZTVMagnets(showname, seas_num, epi_num);
            // Get all other website magnets by episode
            return magnets;
        }

        private void GetEZTVMagnets(string showname, int seas_num, int epi_num)
        {
            string html = BuildEztvURL(showname);
            string compareshowname = common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".");
            string compareseason;
            if (epi_num == 1)
            {
                compareseason = common.BuildSeasonOnly(seas_num);
            }
            else
            {
                compareseason = common.BuildSeasonEpisodeString(seas_num, epi_num);
            }
            string comparewithmagnet = compareshowname + "." + compareseason;
            log.Write($"Compare string = {comparewithmagnet}", "Eztv", 3);

            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(html);
            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");

            foreach (HtmlNode node in table)
            {
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:"))
                {
                    log.Write($"Unmatched: " + node.Attributes["href"].Value, "Eztv", 3);
                }
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains(comparewithmagnet))
                {
                    magnets.Add(node.Attributes["href"].Value);
                }
            }

            if (magnets.Count == 0)
            {
                WholeSeasonFound = false;
                compareseason = common.BuildSeasonEpisodeString(seas_num, epi_num);
                comparewithmagnet = compareshowname + "." + compareseason;
                log.Write($"Did not find a whole season now running: Compare string = {comparewithmagnet}", "Eztv", 3);
                foreach (HtmlNode node in table)
                {
                    if (node.Attributes["href"].Value.ToLower().Contains("magnet:"))
                    {
                        log.Write($"Unmatched: " + node.Attributes["href"].Value, "Eztv", 3);
                    }
                    if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                        node.Attributes["href"].Value.ToLower().Contains(compareshowname) &&
                        node.Attributes["href"].Value.ToLower().Contains(compareseason))
                    {
                        magnets.Add(node.Attributes["href"].Value);
                    }
                }
            }
            else
            {
                WholeSeasonFound = true;
            }
        }

        private string BuildEztvURL(string showname)
        {
            string eztv_url = "https://eztv.re/search/";
            showname = common.RemoveSpecialCharsInShowname(showname);
            showname = showname.Replace(" ", "-");  //eztv seach char.
            eztv_url = eztv_url + showname;
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
                }
            }
            return magnets;
        }

        #endregion

    }
}

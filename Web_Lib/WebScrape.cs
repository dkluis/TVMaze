using Common_Lib;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

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

        public string GetMagnetTVShowEpisode(string showname, int seas_num, int epi_num)
        {
            magnets = new();
            GetEZTVMagnets(showname, seas_num, epi_num);
            GetMagnetDLMagnets(showname, seas_num, epi_num);
            // GetRarbgMagments(showname, seas_num, epi_num);
            // Get MagnetDL
            // Eztv API
            // 
            if (magnets.Count > 0)
            {
                log.Write($"Total Magnets found {magnets.Count}", "Getters", 1);
                return magnets[0];
            }
            else
            {
                return "";
            }
        }

        #endregion

        #region EZTV

        private void GetEZTVMagnets(string showname, int seas_num, int epi_num)
        {
            string html = BuildEztvURL(showname);
            string compareshowname = common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".");
            string compareseason;

            int priority;
            string prioritizedmagnet;

            if (epi_num == 1)
            {
                compareseason = common.BuildSeasonOnly(seas_num);
            }
            else
            {
                compareseason = common.BuildSeasonEpisodeString(seas_num, epi_num);
            }
            string comparewithmagnet = compareshowname + "." + compareseason;
            log.Write($"Compare string = {comparewithmagnet}", "Eztv", 2);

            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(html);
            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");

            foreach (HtmlNode node in table)
            {
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains(comparewithmagnet))
                {
                    priority = PrioritizeMagnet(node.Attributes["href"].Value, "Eztv");
                    if (priority > 130)
                    {
                        prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                        log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "Eztv", 3);
                        magnets.Add(prioritizedmagnet);
                    }
                }
            }

            if (magnets.Count == 0)
            {
                WholeSeasonFound = false;
                compareseason = common.BuildSeasonEpisodeString(seas_num, epi_num);
                comparewithmagnet = compareshowname + "." + compareseason;
                log.Write($"Did not find a whole season now running with ----> Compare string = {comparewithmagnet}", "Eztv", 2);
                foreach (HtmlNode node in table)
                {
                    if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                        node.Attributes["href"].Value.ToLower().Contains(compareshowname) &&
                        node.Attributes["href"].Value.ToLower().Contains(compareseason))
                    {
                        priority = PrioritizeMagnet(node.Attributes["href"].Value, "Eztv");
                        if (priority > 130)
                        {
                            prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                            log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "Eztv", 3);
                            magnets.Add(prioritizedmagnet);
                        }
                    }
                }
            }
            else
            {
                WholeSeasonFound = true;
            }
            magnets.Sort();
            magnets.Reverse();
        }

        private string BuildEztvURL(string showname)
        {
            string eztv_url = "https://eztv.re/search/";
            showname = common.RemoveSpecialCharsInShowname(showname);
            showname = showname.Replace(" ", "-");  //eztv seach char.
            eztv_url = eztv_url + showname;
            log.Write($"URL MagnetDL is {eztv_url}", "Eztv", 3);
            return eztv_url;
        }

        #endregion

        #region MagnetDL

        private void GetMagnetDLMagnets(string showname, int seas_num, int epi_num)
        {
            string html = BuildMagnetDLURL(showname);

            string compareshowname = common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".");
            string compareseason;
            int magnetcount = magnets.Count;

            int priority;
            string prioritizedmagnet;

            if (epi_num == 1)
            {
                compareseason = common.BuildSeasonOnly(seas_num);
            }
            else
            {
                compareseason = common.BuildSeasonEpisodeString(seas_num, epi_num);
            }
            string comparewithmagnet = compareshowname + "." + compareseason;
            log.Write($"Compare string = {comparewithmagnet}", "MagnetDL", 2);

            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(html);

            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            if (table is null)
            {
                Environment.Exit(99);
            }
            foreach (HtmlNode node in table)
            {
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains(comparewithmagnet))
                {
                    priority = PrioritizeMagnet(node.Attributes["href"].Value, "MagnetDL");
                    if (priority > 130)
                    {
                        prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                        log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "MagnetDL", 3);
                        magnets.Add(prioritizedmagnet);
                    }
                }
            }

            if (magnets.Count == magnetcount)
            {
                if (table is null)
                {
                    Environment.Exit(99);
                }
                WholeSeasonFound = false;
                compareseason = common.BuildSeasonEpisodeString(seas_num, epi_num);
                comparewithmagnet = compareshowname + "." + compareseason;
                log.Write($"Did not find a whole season now running with ----> Compare string = {comparewithmagnet}", "MagnetDL", 2);
                foreach (HtmlNode node in table)
                {
                    if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                        node.Attributes["href"].Value.ToLower().Contains(compareshowname) &&
                        node.Attributes["href"].Value.ToLower().Contains(compareseason))
                    {
                        priority = PrioritizeMagnet(node.Attributes["href"].Value, "MagnetDL");
                        if (priority > 130)
                        {
                            prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                            log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "MagnetDL", 3);
                            magnets.Add(prioritizedmagnet);
                        }
                    }
                }
            }
            else
            {
                WholeSeasonFound = true;
            }
            magnets.Sort();
            magnets.Reverse();
        }

        private string BuildMagnetDLURL(string showname)
        {
            string url = "https://www.magnetdl.com/";
            showname = common.RemoveSpecialCharsInShowname(showname);
            showname = showname.Replace(" ", "-");  //MagnetDL seach char.
            url = url + "/" + showname[0].ToString().ToLower() + "/" +showname + "/";
            log.Write($"URL MagnetDL is {url}", "MagnetDL", 3);
            return url;
        }

        #endregion

        #region RARBG
        /*
        private void GetRarbgMagnets(string showname, int seas_num, int epi_num)
        {
            string html = BuildRarbgURL(showname);
            string compareshowname = common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".");
            string validatestring;
            int magnetcount = magnets.Count;

            int priority;
            string prioritizedmagnet;

            if (epi_num == 1)
            {
                validatestring = common.BuildSeasonOnly(seas_num);
            }
            else
            {
                validatestring = common.BuildSeasonEpisodeString(seas_num, epi_num);
            }
            string comparewithmagnet = compareshowname + "." + validatestring;
            log.Write($"Compare string = {comparewithmagnet}", "PirateBay", 2);

            WebAPI tvmapi = new(log);
            HttpResponseMessage result = tvmapi.GetRarbgInfo(html);
            log.Write($"Result back from API call {result.StatusCode}", "Program WebAPI", 3);
            if (!result.IsSuccessStatusCode)
            {
                Environment.Exit(99);
            }
            var content = result.Content.ReadAsStringAsync().Result;
            dynamic jsoncontent = JsonConvert.DeserializeObject(content);

            log.Write($"JSon is {jsoncontent}");

            tvmapi.Dispose();
            /*
            if (magnets.Count == magnetcount)
            {
                WholeSeasonFound = false;
                validatestring = common.BuildSeasonEpisodeString(seas_num, epi_num);
                comparewithmagnet = compareshowname + "." + validatestring;
                log.Write($"Did not find a whole season now running with ----> Compare string = {comparewithmagnet}", "PirateBay", 2);
                foreach (HtmlNode node in table)
                {
                    if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                        node.Attributes["href"].Value.ToLower().Contains(compareshowname) &&
                        node.Attributes["href"].Value.ToLower().Contains(validatestring))
                    {
                        priority = PrioritizeMagnet(node.Attributes["href"].Value, "PirateBay");
                        if (priority > 130)
                        {
                            prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                            log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "PirateBay", 3);
                            magnets.Add(prioritizedmagnet);
                        }
                    }
                }
            }
            else
            {
                WholeSeasonFound = true;
            }
        
            magnets.Sort();
            magnets.Reverse();
        }

        private string BuildRarbgURL(string showname)
        {
            string url = "https://torrentapi.org/pubapi_v2.php?mode=search&search_string=";
            showname = common.RemoveSpecialCharsInShowname(showname);
            showname = showname.Replace(" ", "%20");  //piratebay seach char.
            url = url + showname;
            url = url + "'&token=lnjzy73ucv&format=json_extended&app_id=lol";
            log.Write($"URL Piratebay is {url}", "PirateBay", 3);
            return url;
        }
        */
        #endregion

        #region Priorities

        private int PrioritizeMagnet(string magnet, string provider)
        {
            int prio;
            switch (provider)
            {
                case "Eztv":
                    prio = 100;
                    break;
                case "MagnetDL":
                    prio = 110;    // Does not have container info so +10 by default
                    break;
                default:
                    prio = 100;
                    break;
            }
            // Codex values
            if (magnet.ToLower().Contains("x264") || magnet.ToLower().Contains("h264"))
            {
                prio += 60;
            }
            if (magnet.ToLower().Contains("xvid"))
            {
                prio += 50;
            }
            if (magnet.ToLower().Contains("x265") || magnet.ToLower().Contains("h265"))
            {
                prio += 20;
            }
            // Resolution values
            if (magnet.ToLower().Contains("1080p"))
            {
                prio += 15;
            }
            if (magnet.ToLower().Contains("hdtv"))
            {
                prio += 14;
            }
            if (magnet.ToLower().Contains("720p"))
            {
                prio += 10;
            }
            if (magnet.ToLower().Contains("480p"))
            {
                prio += 3;
            }
            // Container values
            if (magnet.ToLower().Contains(".mkv"))
            {
                prio += 10;
            }
            if (magnet.ToLower().Contains(".mp3"))
            {
                prio += 5;
            }
            if (magnet.ToLower().Contains(".avi"))
            {
                prio += 3;
            }

            return prio;
        }

        #endregion

        #region Web Scrapping Test

        public List<string> TestWebScrap()
        {
            string html = "https://eztv.re/search/the-white-lotus";
            HtmlWeb web = new HtmlWeb();
            List<string> magnets = new();

            HtmlDocument htmlDoc = web.Load(html);
            //log.Write($"HTML Doc: {htmlDoc.ParsedText}", "WebScrape", 0, false);

            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            int priority;
            string prioritizedmagnet;
            foreach (HtmlNode node in table)
            {
                if (node.Attributes["href"].Value.Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains("s01e04"))
                {
                    //ToDo add prio value to magnet info//
                    priority = PrioritizeMagnet(node.Attributes["href"].Value, "Eztv");
                    if (priority > 100)
                    {
                        prioritizedmagnet = priority.ToString() + "#$# " + node.Attributes["href"].Value;
                        if (priority > 130)
                        {
                            magnets.Add(prioritizedmagnet);
                        }
                    }
                }
            }
            magnets.Sort();
            magnets.Reverse();
            return magnets;
        }

        #endregion

    }
}

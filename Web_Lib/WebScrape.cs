using Common_Lib;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Web_Lib
{
    public class WebScrape : IDisposable
    {
        private readonly TextFileHandler log;
        public List<string> magnets = new();
        // private Common common = new();
        public bool WholeSeasonFound;
        public bool rarbgError;

        public WebScrape(TextFileHandler logger)
        {
            log = logger;
        }

        #region EZTV

        public void GetEZTVMagnets(string showname, string seasepi)
        {
            string html = BuildEztvURL(showname);

            string comparewithmagnet = Common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".") + "." + seasepi + ".";
            log.Write($"Compare string = {comparewithmagnet}", "Eztv", 2);

            int priority;
            string prioritizedmagnet;

            HtmlWeb web = new();
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

            magnets.Sort();
            magnets.Reverse();
        }

        private string BuildEztvURL(string showname)
        {
            string eztv_url = "https://eztv.re/search/";
            showname = Common.RemoveSpecialCharsInShowname(showname);
            showname = showname.Replace(" ", "-");  //eztv seach char.
            eztv_url += showname;
            log.Write($"URL MagnetDL is {eztv_url}", "Eztv", 3);
            return eztv_url;
        }

        #endregion

        #region MagnetDL

        public void GetMagnetDLMagnets(string showname, string seasepi)
        {
            string html = BuildMagnetDLURL(showname);

            string comparewithmagnet = Common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".") + "." + seasepi + ".";
            log.Write($"Compare string = {comparewithmagnet}", "MagnetDL", 2);

            int priority;
            string prioritizedmagnet;


            HtmlWeb web = new();
            HtmlDocument htmlDoc = web.Load(html);

            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            if (table is null)
            {
                log.Write($"No result return from the webscape", "MagnetDL", 0);
                return;
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

            magnets.Sort();
            magnets.Reverse();
        }

        private string BuildMagnetDLURL(string showname)
        {
            string url = "https://www.magnetdl.com/";
            showname = Common.RemoveSpecialCharsInShowname(showname);
            showname = showname.Replace(" ", "-");  //MagnetDL seach char.
            url = url + "/" + showname[0].ToString().ToLower() + "/" + showname + "/";
            log.Write($"URL MagnetDL is {url}", "MagnetDL", 3);
            return url;
        }

        #endregion

        #region RarbgAPI

        public void GetRarbgMagnets(string showname, string seasepi)
        {
            int prio;
            WebAPI tvmapi = new(log);
            log.Write("Start to Rarbg API test", "Program", 0);

            string comparewithmagnet = Common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".") + "." + seasepi + ".";

            HttpResponseMessage result = tvmapi.GetRarbgMagnets(showname + " " + seasepi);
            log.Write($"Compare string = {comparewithmagnet}", "RarbgAPI", 3);

            log.Write($"Result back from API call {result.StatusCode}", "RarbgAPI", 3);
            if (!result.IsSuccessStatusCode)
            {
                log.Write($"No Result returned from the API", "RarbgAPI", 0);
                return;
            }

            var content = result.Content.ReadAsStringAsync().Result;
            if (content == "{\"error\":\"No results found\",\"error_code\":20}")
            {
                // Try to get the result again.   Sometimes gives this error eventhough there are magnets to be found
                log.Write("Status OK, Error Occured Not Found", "Rarbg", 1);
                rarbgError = true;
                return;
            }

            dynamic jsoncontent = JsonConvert.DeserializeObject(content);
            // log.Write($"JSon is {jsoncontent}");

            foreach (var show in jsoncontent["torrent_results"])
            {
                string magnet = show["download"];
                prio = PrioritizeMagnet(magnet, "RarbgAPI");
                log.Write($"Magnet found: {magnet}");
                if (prio > 130 && magnet.ToLower().Contains(comparewithmagnet))
                {
                    //To Do still need the compara string check
                    magnets.Add(prio + "#$# " + magnet);
                    log.Write($"Prioritized Magnet Recorded {prio}#$# {magnet}", "RarbgAPI", 3);
                }
            }

            magnets.Sort();
            magnets.Reverse();
        }

        #endregion

        #region EztvAPI IMDB


        #endregion

        #region Priorities

        private static int PrioritizeMagnet(string magnet, string provider)
        {
            var prio = provider switch
            {
                "Eztv" or "EztvAPI" => 100,
                "MagnetDL" => 110,   // Does not have container info so +10 by default
                "RarbgAPI" => 130,   // Typically has the better so +30 by default
                _ => 100,
            };
            // Codex values
            if (magnet.ToLower().Contains("x264") || magnet.ToLower().Contains("h264"))
            {
                prio += 60;
            }
            else if (magnet.ToLower().Contains("xvid"))
            {
                prio += 50;
            }
            else if (magnet.ToLower().Contains("x265") || magnet.ToLower().Contains("h265"))
            {
                prio += 20;
            }
            // Resolution values
            if (magnet.ToLower().Contains("1080p"))
            {
                prio += 15;
            }
            else if (magnet.ToLower().Contains("hdtv"))
            {
                prio += 14;
            }
            else if (magnet.ToLower().Contains("720p"))
            {
                prio += 10;
            }
            else if (magnet.ToLower().Contains("480p"))
            {
                prio += 3;
            }
            // Container values
            if (magnet.ToLower().Contains(".mkv"))
            {
                prio += 10;
            }
            else if (magnet.ToLower().Contains(".mp3"))
            {
                prio += 5;
            }
            else if (magnet.ToLower().Contains(".avi"))
            {
                prio += 3;
            }

            return prio;
        }

        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }

    public class Magnets
    {

        #region Get Prioritized Magnet

        public string PerformShowEpisodeMagnetsSearch(string showname, int seas_num, int epi_num, TextFileHandler logger)
        {
            TextFileHandler log = logger;
            string seasepi;
            if (epi_num == 1)
            {
                //Search for whole season first
                seasepi = Common.BuildSeasonOnly(seas_num);
            }
            else
            {
                seasepi = Common.BuildSeasonEpisodeString(seas_num, epi_num);
            }

            using WebScrape seasonscrape = new(log);
            {
                seasonscrape.magnets = new();
                seasonscrape.GetRarbgMagnets(showname, seasepi);
                seasonscrape.GetEZTVMagnets(showname, seasepi);
                seasonscrape.GetMagnetDLMagnets(showname, seasepi);
                // Eztv API only for imdb known shows
                // GetMagnetsEztvAPI(imdb);

                if (seasonscrape.magnets.Count > 0)
                {
                    return seasonscrape.magnets[0];
                }
            }

            if (epi_num == 1) // Nothing found while search for the Season
            {
                using WebScrape episodescrape = new(log);
                {
                    episodescrape.magnets = new();
                    seasepi = Common.BuildSeasonEpisodeString(seas_num, epi_num);
                    episodescrape.GetRarbgMagnets(showname, seasepi);
                    episodescrape.GetEZTVMagnets(showname, seasepi);
                    episodescrape.GetMagnetDLMagnets(showname, seasepi);

                    if (episodescrape.magnets.Count > 0)
                    {
                        log.Write($"Total Magnets found {episodescrape.magnets.Count}", "Getters", 1);
                        return episodescrape.magnets[0];
                    }
                    else
                    {

                        log.Write("No Magnets found", "Getters", 1);
                        return "";
                    }
                }
            }
            else
            {
                return "";  // Nothing found
            }
        }

        #endregion

    }
}

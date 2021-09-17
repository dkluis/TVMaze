using Common_Lib;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly AppInfo appinfo;

        public WebScrape(AppInfo info)
        {
            appinfo = info;
            log = appinfo.TxtFile;
        }

        #region Finders

        #region EZTV

        public void GetEZTVMagnets(string showname, string seasepi)
        {
            string html = BuildEztvURL($"{showname}-{seasepi}");

            string comparewithmagnet = Common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".") + "." + seasepi + ".";
            log.Write($"Compare string = {comparewithmagnet}", "Eztv", 4);

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
                        log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "Eztv", 4);
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
            log.Write($"URL EZTV is {eztv_url}", "Eztv", 4);
            return eztv_url;
        }

        #endregion

        #region MagnetDL

        public void GetMagnetDLMagnets(string showname, string seasepi)
        {
            string html = BuildMagnetDLURL($"{showname}-{seasepi}");

            string comparewithmagnet = Common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".") + "." + seasepi + ".";
            log.Write($"Compare string = {comparewithmagnet}", "MagnetDL", 4);

            int priority;
            string prioritizedmagnet;


            HtmlWeb web = new();
            HtmlDocument htmlDoc = web.Load(html);

            HtmlNodeCollection table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            if (table is null)
            {
                log.Write($"No result return from the webscape", "MagnetDL", 3);
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
                        log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "MagnetDL", 4);
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
            log.Write($"URL MagnetDL is {url}", "MagnetDL", 4);
            return url;
        }

        #endregion

        #region RarbgAPI

        public void GetRarbgMagnets(string showname, string seasepi)
        {
            int prio;
            WebAPI tvmapi = new(appinfo);

            string comparewithmagnet = Common.RemoveSpecialCharsInShowname(showname).Replace(" ", ".") + "." + seasepi + ".";

            HttpResponseMessage result = tvmapi.GetRarbgMagnets(showname + " " + seasepi);
            log.Write($"Compare string = {comparewithmagnet}", "RarbgAPI", 4);

            log.Write($"Result back from API call {result.StatusCode}", "RarbgAPI", 4);
            if (!result.IsSuccessStatusCode)
            {
                log.Write($"No Result returned from the API", "RarbgAPI", 4);
                return;
            }

            string content = result.Content.ReadAsStringAsync().Result;
            if (content == "{\"error\":\"No results found\",\"error_code\":20}")
            {
                log.Write("Status OK, Error Occured Not Found", "Rarbg", 4);
                rarbgError = true;
                return;
            }

            dynamic jsoncontent = JsonConvert.DeserializeObject(content);
            foreach (var show in jsoncontent["torrent_results"])
            {
                string magnet = show["download"];
                prio = PrioritizeMagnet(magnet, "RarbgAPI");
                //log.Write($"Magnet found: {magnet}");
                if (prio > 130 && magnet.ToLower().Contains(comparewithmagnet))
                {
                    //TODO still need the compare string check
                    magnets.Add(prio + "#$# " + magnet);
                    log.Write($"Prioritized Magnet Recorded {prio}#$# {magnet}", "RarbgAPI", 4);
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
                prio += 65;
            }
            // Resolution values
            if (magnet.ToLower().Contains("1080p."))
            {
                prio += 15;
            }
            else if (magnet.ToLower().Contains("hdtv."))
            {
                prio += 14;
            }
            else if (magnet.ToLower().Contains("720p."))
            {
                prio += 10;
            }
            else if (magnet.ToLower().Contains("480p."))
            {
                prio += 3;
            }
            else if (magnet.ToLower().Contains("2160p."))
            {
                prio -= 75;
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

            // Wrong Languages
            if (magnet.ToLower().Contains(".italian."))
            {
                prio -= 75;
            }

            return prio;
        }

        #endregion

        #endregion

        #region ShowRss

        public List<string> GetShowRssInfo()
        {

            //TODO Figure out how to log into ShowRss via webscrape and replace below string loading

            string showrsspath = Path.Combine(appinfo.ConfigPath, "Inputs", "ShowRss.html");
            HtmlDocument showrsshtml = new();

            string showrssinfo = File.ReadAllText(showrsspath);
            showrsshtml.LoadHtml(showrssinfo);

            HtmlNodeCollection table = showrsshtml.DocumentNode.SelectNodes("//li/a");
            List<string> Titles = new();
            string showname;

            foreach (HtmlNode node in table)
            {
                if (node.Attributes["class"] is null) { continue; }
                if (node.Attributes["class"].Value.ToLower().Contains("sh"))
                {
                    showname = Common.RemoveSpecialCharsInShowname(node.Attributes["title"].Value);
                    showname = Common.RemoveSuffixFromShowname(showname);
                    Titles.Add(showname);
                }
            }

            return Titles;
        }

        public bool ShowRssLogin()
        {
            bool success = false;



            return success;
        }

        #endregion

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }

    public class Magnets
    {
        private readonly AppInfo appinfo;

        #region Get Prioritized Magnet

        public Magnets(AppInfo info)
        {
            appinfo = info;
        }

        public Tuple<bool, string> PerformShowEpisodeMagnetsSearch(string showname, int seas_num, int epi_num, TextFileHandler logger)
        {
            TextFileHandler log = logger;
            string seasepi;
            string magnet = "";
            Tuple<bool, string> result = new(false, "");

            if (epi_num == 1) //Search for whole season first
            {
                seasepi = Common.BuildSeasonOnly(seas_num);
                magnet = PerformFindMagnet(showname, seasepi, log);
                result = new(true, magnet);
            }

            if (magnet == "") //Search if no season found for all other episodes
            {
                seasepi = Common.BuildSeasonEpisodeString(seas_num, epi_num);
                magnet = PerformFindMagnet(showname, seasepi, log);
                result = new(false, magnet);
            }
            return result;
        }

        public string PerformFindMagnet(string showname, string seasepi, TextFileHandler log)
        {
            using WebScrape seasonscrape = new(appinfo);
            {
                seasonscrape.magnets = new();
                seasonscrape.GetRarbgMagnets(showname, seasepi);
                seasonscrape.GetEZTVMagnets(showname, seasepi);
                seasonscrape.GetMagnetDLMagnets(showname, seasepi);
                // Eztv API only for imdb known shows
                // GetMagnetsEztvAPI(imdb);

                if (seasonscrape.magnets.Count > 0)
                {
                    log.Write($"Total Magnets found {seasonscrape.magnets.Count}", "Getters", 4);
                    string[] temp = seasonscrape.magnets[0].Split("#$#");
                    string magnet = temp[1];
                    return magnet;
                }
            }
            return "";
        }

        #endregion

    }
}

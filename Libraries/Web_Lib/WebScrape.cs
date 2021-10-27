using System;
using System.Collections.Generic;
using System.IO;
using Common_Lib;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Web_Lib
{
    public class WebScrape : IDisposable
    {
        private readonly AppInfo _appinfo;
        private readonly TextFileHandler _log;
        public List<string> Magnets = new();
        public bool RarbgError;
        public bool WholeSeasonFound;

        public WebScrape(AppInfo info)
        {
            _appinfo = info;
            _log = _appinfo.TxtFile;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #region Finders

        #region EZTV

        public void GetEztvMagnets(string showname, string seasepi)
        {
            var foundmagnets = 0;
            var html = BuildEztvUrl($"{showname}-{seasepi}");

            var comparewithmagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showname).Replace(" ", ".") + "." +
                                    seasepi + ".";
            _log.Write($"Compare string = {comparewithmagnet}", "Eztv", 4);

            int priority;
            string prioritizedmagnet;
            HtmlWeb web = new();
            HtmlDocument htmlDoc = new();
            try
            {
                htmlDoc = web.Load(html);
            }
            catch (HtmlWebException ex)
            {
                _log.Write($"Error Occurred loading Url {html} --- {ex}", "EZTV", 0);
                return;
            }

            var table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            if (table is not null)
            {
                foreach (var node in table)
                    if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                        node.Attributes["href"].Value.ToLower().Contains(comparewithmagnet))
                    {
                        priority = PrioritizeMagnet(node.Attributes["href"].Value, "Eztv");
                        if (priority > 130)
                        {
                            prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                            _log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "Eztv", 4);
                            Magnets.Add(prioritizedmagnet);
                            foundmagnets++;
                        }
                    }
            }
            else
            {
                _log.Write("No result returned from the webscape Eztv", "Eztv", 4);
                return;
            }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundmagnets} via EZTV");
        }

        private string BuildEztvUrl(string showname)
        {
            var eztvUrl = "https://eztv.re/search/";
            showname = Common.RemoveSpecialCharsInShowName(showname);
            showname = showname.Replace(" ", "-"); //eztv seach char.
            eztvUrl += showname;
            _log.Write($"URL EZTV is {eztvUrl}", "Eztv", 4);
            return eztvUrl;
        }

        #endregion

        #region MagnetDL

        public void GetMagnetDlMagnets(string showname, string seasepi)
        {
            var foundmagnets = 0;
            var html = BuildMagnetDlurl($"{showname}-{seasepi}");

            var comparewithmagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showname).Replace(" ", ".") + "." +
                                    seasepi + ".";
            _log.Write($"Compare string = {comparewithmagnet}", "MagnetDL", 4);

            int priority;
            string prioritizedmagnet;

            HtmlWeb web = new();
            HtmlDocument htmlDoc = new();
            try
            {
                htmlDoc = web.Load(html);
            }
            catch (HtmlWebException ex)
            {
                _log.Write($"Error Occurred loading Url {html} --- {ex}", "MagnetDL", 0);
                return;
            }

            var table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            if (table is null)
            {
                _log.Write("No result returned from the webscape MagnetDL", "MagnetDL", 4);
                return;
            }

            foreach (var node in table)
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains(comparewithmagnet))
                {
                    priority = PrioritizeMagnet(node.Attributes["href"].Value, "MagnetDL");
                    if (priority > 130)
                    {
                        prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                        _log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "MagnetDL", 4);
                        Magnets.Add(prioritizedmagnet);
                        foundmagnets++;
                    }
                }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundmagnets} via MagnetDL");
        }

        private string BuildMagnetDlurl(string showname)
        {
            var url = "https://www.magnetdl.com/";
            showname = Common.RemoveSpecialCharsInShowName(showname);
            showname = showname.Replace(" ", "-"); //MagnetDL seach char.
            url = url + "/" + showname[0].ToString().ToLower() + "/" + showname + "/";
            _log.Write($"URL MagnetDL is {url}", "MagnetDL", 4);
            return url;
        }

        #endregion

        #region RarbgAPI

        public void GetRarbgMagnets(string showname, string seasepi)
        {
            int prio;
            var foundmagnets = 0;
            WebApi tvmapi = new(_appinfo);
            var comparewithmagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showname).Replace(" ", ".") + "." +
                                    seasepi + ".";
            var result = tvmapi.GetRarbgMagnets(showname + " " + seasepi);

            _log.Write($"Compare string = {comparewithmagnet}", "RarbgAPI", 4);
            _log.Write($"Result back from API call {result.StatusCode}", "RarbgAPI", 4);

            if (!result.IsSuccessStatusCode)
            {
                _log.Write("No Result returned from the API RarbgAPI", "RarbgAPI", 4);
                return;
            }

            var content = result.Content.ReadAsStringAsync().Result;
            if (content == "{\"error\":\"No results found\",\"error_code\":20}")
            {
                _log.Write("No Result returned from the API RarbgAPI", "RarbgAPI", 4);
                RarbgError = true;
                return;
            }

            dynamic jsoncontent = JsonConvert.DeserializeObject(content);
            foreach (var show in jsoncontent["torrent_results"])
            {
                string magnet = show["download"];
                prio = PrioritizeMagnet(magnet, "RarbgAPI");
                if (prio > 130 && magnet.ToLower().Contains(comparewithmagnet))
                {
                    Magnets.Add(prio + "#$# " + magnet);
                    foundmagnets++;
                    _log.Write($"Prioritized Magnet Recorded {prio}#$# {magnet}", "RarbgAPI", 4);
                }
            }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundmagnets} via RarbgAPI");
        }

        #endregion

        #region PirateBay

        public void GetPirateBayMagnets(string showname, string seasepi)
        {
            var foundmagnets = 0;
            var html = BuildPirateBayUrl($"{showname}+{seasepi}");

            var comparewithmagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showname).Replace(" ", ".") + "." +
                                    seasepi + ".";
            _log.Write($"Compare string = {comparewithmagnet}", "PirateBay", 4);

            int priority;
            string prioritizedmagnet;

            HtmlWeb web = new();
            HtmlDocument htmlDoc = new();
            try
            {
                htmlDoc = web.Load(html);
            }
            catch (HtmlWebException ex)
            {
                _log.Write($"Error Occurred loading Url {html} --- {ex}", "PirateBay", 0);
                return;
            }

            var table = htmlDoc.DocumentNode.SelectNodes("//td/a");
            if (table is null)
            {
                _log.Write("No result returned from the webscape PirateBay", "PirateBay", 4);
                return;
            }

            foreach (var node in table)
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains(comparewithmagnet))
                {
                    priority = PrioritizeMagnet(node.Attributes["href"].Value, "PirateBay");
                    if (priority > 130)
                    {
                        prioritizedmagnet = priority + "#$# " + node.Attributes["href"].Value;
                        _log.Write($"Prioritized Magnet recorded: {prioritizedmagnet}", "PirateBay", 4);
                        Magnets.Add(prioritizedmagnet);
                        foundmagnets++;
                    }
                }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundmagnets} via PirateBay");
        }

        private string BuildPirateBayUrl(string showname)
        {
            var url = "https://piratebay.bid/s/?q=";
            showname = Common.RemoveSpecialCharsInShowName(showname);
            showname = showname.Replace(" ", "+");
            url = url + showname + "&category=0&page=0&orderby=99";
            _log.Write($"URL PirateBay is {url}", "PirateBay", 4);
            return url;
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
                "PirateBay" => 100,
                "MagnetDL" => 110, // Does not have container info so +10 by default
                "RarbgAPI" => 130, // Typically has the better so +30 by default
                _ => 100
            };
            // Codex values
            if (magnet.ToLower().Contains("x264") || magnet.ToLower().Contains("h264"))
                prio += 60;
            else if (magnet.ToLower().Contains("xvid"))
                prio += 30;
            else if (magnet.ToLower().Contains("x265") || magnet.ToLower().Contains("h265"))
                prio += 65;
            else if (magnet.ToLower().Contains("hevc")) prio += 55;
            // Resolution values
            if (magnet.ToLower().Contains("1080p."))
                prio += 15;
            else if (magnet.ToLower().Contains("hdtv."))
                prio += 14;
            else if (magnet.ToLower().Contains("720p."))
                prio += 10;
            else if (magnet.ToLower().Contains("480p."))
                prio += 3;
            else if (magnet.ToLower().Contains("2160p.")) prio -= 75;
            // Container values
            if (magnet.ToLower().Contains(".mkv"))
                prio += 10;
            else if (magnet.ToLower().Contains(".mp3"))
                prio += 5;
            else if (magnet.ToLower().Contains(".avi")) prio += 3;

            // Wrong Languages
            if (magnet.ToLower().Contains(".italian.")) prio -= 75;

            return prio;
        }

        #endregion

        #endregion

        #region ShowRss

        public List<string> GetShowRssInfo()
        {
            //TODO Figure out how to log into ShowRss via webscrape and replace below string loading

            var showrsspath = Path.Combine(_appinfo.ConfigPath, "Inputs", "ShowRss.html");
            HtmlDocument showrsshtml = new();

            var showrssinfo = File.ReadAllText(showrsspath);
            showrsshtml.LoadHtml(showrssinfo);

            var table = showrsshtml.DocumentNode.SelectNodes("//li/a");
            List<string> titles = new();
            string showname;

            foreach (var node in table)
            {
                if (node.Attributes["class"] is null) continue;
                if (node.Attributes["class"].Value.ToLower().Contains("sh"))
                {
                    showname = Common.RemoveSpecialCharsInShowName(node.Attributes["title"].Value);
                    showname = Common.RemoveSuffixFromShowName(showname);
                    titles.Add(showname);
                }
            }

            return titles;
        }

        public bool ShowRssLogin()
        {
            var success = false;


            return success;
        }

        #endregion
    }

    public class Magnets
    {
        private readonly AppInfo _appinfo;

        #region Get Prioritized Magnet

        public Magnets(AppInfo info)
        {
            _appinfo = info;
        }

        public Tuple<bool, string> PerformShowEpisodeMagnetsSearch(string showname, int seasNum, int epiNum,
            TextFileHandler logger)
        {
            var log = logger;
            string seasepi;
            var magnet = "";
            Tuple<bool, string> result = new(false, "");

            if (epiNum == 1) //Search for whole season first
            {
                seasepi = Common.BuildSeasonOnly(seasNum);
                magnet = PerformFindMagnet(showname, seasepi, log);
                result = new Tuple<bool, string>(true, magnet);
            }

            if (magnet == "")
            {
                if (epiNum == 1)
                    log.Write(
                        $"No Magnet found for the whole season {seasNum} of {showname} now searching for episode 1");
                seasepi = Common.BuildSeasonEpisodeString(seasNum, epiNum);
                magnet = PerformFindMagnet(showname, seasepi, log);
                result = new Tuple<bool, string>(false, magnet);
            }

            return result;
        }

        public string PerformFindMagnet(string showname, string seasepi, TextFileHandler log)
        {
            using WebScrape seasonscrape = new(_appinfo);
            {
                seasonscrape.Magnets = new List<string>();
                seasonscrape.GetRarbgMagnets(showname, seasepi);
                seasonscrape.GetEztvMagnets(showname, seasepi);
                seasonscrape.GetMagnetDlMagnets(showname, seasepi);
                seasonscrape.GetPirateBayMagnets(showname, seasepi);

                if (seasonscrape.Magnets.Count > 0)
                {
                    log.Write($"Total Magnets found {seasonscrape.Magnets.Count}", "Getters", 4);
                    var temp = seasonscrape.Magnets[0].Split("#$#");
                    var magnet = temp[1];
                    return magnet;
                }
            }
            return "";
        }

        #endregion
    }
}
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
        private readonly AppInfo _appInfo;
        private readonly TextFileHandler _log;
        public List<string> Magnets = new();

        public WebScrape(AppInfo info)
        {
            _appInfo = info;
            _log = _appInfo.TxtFile;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public List<string> GetShowRssInfo()
        {
            //TODO Figure out how to log into ShowRss via webScrape and replace below string loading

            var showRssPath = Path.Combine(_appInfo.ConfigPath, "Inputs", "ShowRss.html");
            HtmlDocument showRssHtml = new();

            var showRssInfo = File.ReadAllText(showRssPath);
            showRssHtml.LoadHtml(showRssInfo);

            var table = showRssHtml.DocumentNode.SelectNodes("//li/a");
            List<string> titles = new();

            foreach (var node in table)
            {
                if (node.Attributes["class"] is null) continue;
                if (!node.Attributes["class"].Value.ToLower().Contains("sh")) continue;
                var showName = Common.RemoveSpecialCharsInShowName(node.Attributes["title"].Value);
                showName = Common.RemoveSuffixFromShowName(showName);
                titles.Add(showName);
            }

            return titles;
        }

        public bool ShowRssLogin()
        {
            return false;
        }

        public void GetEztvMagnets(string showName, string seasEpi)
        {
            var foundMagnets = 0;
            var html = BuildEztvUrl($"{showName}-{seasEpi}");

            var compareWithMagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") + "." +
                                    seasEpi + ".";
            _log.Write($"Compare string = {compareWithMagnet}", "Eztv", 4);

            HtmlWeb web = new();
            HtmlDocument htmlDoc;
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
                        node.Attributes["href"].Value.ToLower().Contains(compareWithMagnet))
                    {
                        var priority = PrioritizeMagnet(node.Attributes["href"].Value, "Eztv");
                        if (priority > 130)
                        {
                            var prioritizedMagnet = priority + "#$# " + node.Attributes["href"].Value;
                            _log.Write($"Prioritized Magnet recorded: {prioritizedMagnet}", "Eztv", 4);
                            Magnets.Add(prioritizedMagnet);
                            foundMagnets++;
                        }
                    }
            }
            else
            {
                _log.Write("No result returned from the webScape Eztv", "Eztv", 4);
                return;
            }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundMagnets} via EZTV", "", 4);
        }

        private string BuildEztvUrl(string showName)
        {
            var eztvUrl = "https://eztv.unblockninja.com/search/";
            showName = Common.RemoveSpecialCharsInShowName(showName);
            showName = showName.Replace(" ", "-");
            eztvUrl += showName;
            _log.Write($"URL EZTV is {eztvUrl}", "Eztv", 4);
            return eztvUrl;
        }

        public void GetMagnetDlMagnets(string showName, string seasEpi)
        {
            var foundMagnets = 0;
            var html = BuildMagnetDownloadUrl($"{showName}-{seasEpi}");

            var compareWithMagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") + "." +
                                    seasEpi + ".";
            _log.Write($"Compare string = {compareWithMagnet}", "MagnetDL", 4);

            HtmlWeb web = new();
            HtmlDocument htmlDoc;
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
                _log.Write("No result returned from the webScape MagnetDL", "MagnetDL", 4);
                return;
            }

            foreach (var node in table)
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains(compareWithMagnet))
                {
                    var priority = PrioritizeMagnet(node.Attributes["href"].Value, "MagnetDL");
                    if (priority > 130)
                    {
                        var prioritizedMagnet = priority + "#$# " + node.Attributes["href"].Value;
                        _log.Write($"Prioritized Magnet recorded: {prioritizedMagnet}", "MagnetDL", 4);
                        Magnets.Add(prioritizedMagnet);
                        foundMagnets++;
                    }
                }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundMagnets} via MagnetDL", "", 4);
        }

        private string BuildMagnetDownloadUrl(string showName)
        {
            var url = "https://www.magnetdl.com/";
            showName = Common.RemoveSpecialCharsInShowName(showName);
            showName = showName.Replace(" ", "-");
            url = url + "/" + showName[0].ToString().ToLower() + "/" + showName + "/";
            _log.Write($"URL MagnetDL is {url}", "MagnetDL", 4);
            return url;
        }

        public void GetRarbgMagnets(string showName, string seasEpi)
        {
            var foundMagnets = 0;
            using WebApi tvmApi = new(_appInfo);
            var compareWithMagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") + "." +
                                    seasEpi + ".";
            var result = tvmApi.GetRarbgMagnets(showName + " " + seasEpi);

            _log.Write($"Compare string = {compareWithMagnet}", "RarbgAPI", 4);
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
                return;
            }

            dynamic jsonContent = JsonConvert.DeserializeObject(content);
            if (jsonContent != null && jsonContent["torrent_results"] != null)
                foreach (var show in jsonContent["torrent_results"])
                {
                    string magnet = show["download"];
                    var priority = PrioritizeMagnet(magnet, "RarbgAPI");
                    if (priority <= 130 || !magnet.ToLower().Contains(compareWithMagnet)) continue;
                    Magnets.Add(priority + "#$# " + magnet);
                    foundMagnets++;
                    _log.Write($"Prioritized Magnet Recorded {priority}#$# {magnet}", "RarbgAPI", 4);
                }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundMagnets} via RarbgAPI", "", 4);
        }

        public void GetPirateBayMagnets(string showName, string seasEpi)
        {
            var foundMagnets = 0;
            var html = BuildPirateBayUrl($"{showName}+{seasEpi}");

            var compareWithMagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") + "." +
                                    seasEpi + ".";
            _log.Write($"Compare string = {compareWithMagnet}", "PirateBay", 4);

            HtmlWeb web = new();
            HtmlDocument htmlDoc;
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
                _log.Write("No result returned from the webScape PirateBay", "PirateBay", 4);
                return;
            }

            foreach (var node in table)
                if (node.Attributes["href"].Value.ToLower().Contains("magnet:") &&
                    node.Attributes["href"].Value.ToLower().Contains(compareWithMagnet))
                {
                    var priority = PrioritizeMagnet(node.Attributes["href"].Value, "PirateBay");
                    if (priority > 130)
                    {
                        var prioritizedMagnet = priority + "#$# " + node.Attributes["href"].Value;
                        _log.Write($"Prioritized Magnet recorded: {prioritizedMagnet}", "PirateBay", 4);
                        Magnets.Add(prioritizedMagnet);
                        foundMagnets++;
                    }
                }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundMagnets} via PirateBay", "", 4);
        }

        private string BuildPirateBayUrl(string showName)
        {
            var url = "https://piratebay.bid/s/?q=";
            showName = Common.RemoveSpecialCharsInShowName(showName);
            showName = showName.Replace(" ", "+");
            url = url + showName + "&category=0&page=0&orderby=99";
            _log.Write($"URL PirateBay is {url}", "PirateBay", 4);
            return url;
        }

        private static int PrioritizeMagnet(string magnet, string provider)
        {
            var priority = provider switch
            {
                "Eztv" or "EztvAPI" => 100,
                "PirateBay" => 100,
                "MagnetDL" => 110, // Does not have container info so +10 by default
                "RarbgAPI" => 130, // Typically has the better so +30 by default
                _ => 100
            };
            // Codex values
            if (magnet.ToLower().Contains("x264") || 
                magnet.ToLower().Contains("h264") || 
                magnet.ToLower().Contains("x.264") || 
                magnet.ToLower().Contains("h.264"))
                priority += 60;
            else if (magnet.ToLower().Contains("xvid"))
                priority += 30;
            else if ((magnet.ToLower().Contains("x265") 
                      || magnet.ToLower().Contains("h265") 
                      || magnet.ToLower().Contains("x.265") 
                      || magnet.ToLower().Contains("h.265")) 
                     && !magnet.ToLower().Contains("hvec"))
                priority += 65;
            else if (magnet.ToLower().Contains("hevc"))
                priority += 55;
            // Resolution values
            if (magnet.ToLower().Contains("1080p."))
                priority += 15;
            else if (magnet.ToLower().Contains("hdtv."))
                priority += 14;
            else if (magnet.ToLower().Contains("720p."))
                priority += 10;
            else if (magnet.ToLower().Contains("480p."))
                priority += 3;
            else if (magnet.ToLower().Contains("2160p.")) 
                priority += 13;
            // Container values
            if (magnet.ToLower().Contains(".mkv"))
                priority += 10;
            else if (magnet.ToLower().Contains(".mp3"))
                priority += 5;
            else if (magnet.ToLower().Contains(".avi")) priority += 3;

            // Wrong Languages
            if (magnet.ToLower().Contains(".italian.")) priority -= 75;

            return priority;
        }
    }

    public class Magnets
    {
        private readonly AppInfo _appInfo;

        public Magnets(AppInfo info)
        {
            _appInfo = info;
        }

        public Tuple<bool, string> PerformShowEpisodeMagnetsSearch(string showName, int seasNum, int epiNum,
            TextFileHandler logger)
        {
            var log = logger;
            string seasEpi;
            var magnet = "";
            Tuple<bool, string> result = new(false, "");

            if (epiNum == 1) //Search for whole season first
            {
                seasEpi = Common.BuildSeasonOnly(seasNum);
                magnet = PerformFindMagnet(showName, seasEpi, log);
                result = new Tuple<bool, string>(true, magnet);
            }

            if (magnet == "")
            {
                if (epiNum == 1)
                    log.Write(
                        $"No Magnet found for the whole season {seasNum} of {showName} now searching for episode 1");
                seasEpi = Common.BuildSeasonEpisodeString(seasNum, epiNum);
                magnet = PerformFindMagnet(showName, seasEpi, log);
                result = new Tuple<bool, string>(false, magnet);
            }

            return result;
        }

        public string PerformFindMagnet(string showName, string seasEpi, TextFileHandler log)
        {
            using WebScrape seasonScrape = new(_appInfo);
            {
                seasonScrape.Magnets = new List<string>();
                seasonScrape.GetRarbgMagnets(showName, seasEpi);
                seasonScrape.GetEztvMagnets(showName, seasEpi);
                seasonScrape.GetMagnetDlMagnets(showName, seasEpi);
                seasonScrape.GetPirateBayMagnets(showName, seasEpi);

                if (seasonScrape.Magnets.Count > 0)
                {
                    log.Write($"Total Magnets found {seasonScrape.Magnets.Count}", "Getters", 4);
                    var temp = seasonScrape.Magnets[0].Split("#$#");
                    var magnet = temp[1];
                    return magnet;
                }
            }
            return "";
        }
    }
}
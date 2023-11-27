using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common_Lib;
using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;

namespace Web_Lib;

public class WebScrape : IDisposable
{
    private readonly AppInfo         _appInfo;
    private readonly TextFileHandler _log;
    public           List<string>    Magnets = new();
    public WebScrape(AppInfo info)
    {
        _appInfo = info;
        _log     = _appInfo.TxtFile;
    }
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
    public List<string> GetShowRssInfo()
    {
        var          showRssPath = Path.Combine(_appInfo.ConfigPath!, "Inputs", "ShowRss.html");
        HtmlDocument showRssHtml = new();

        var showRssInfo = File.ReadAllText(showRssPath);
        showRssHtml.LoadHtml(showRssInfo);

        var          table  = showRssHtml.DocumentNode.SelectNodes("//li/a");
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
    public void GetEztvMagnets(string showName, string seasEpi, ChromeDriver browserDriver)
    {
        var foundMagnets = 0;
        var url          = BuildEztvUrl(showName, seasEpi);

        var compareWithMagnet = "dn="                                                           +
                                Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") +
                                "."                                                             +
                                seasEpi                                                         +
                                ".";
        _log.Write($"Compare string = {compareWithMagnet}", "Eztv", 4);

        var html = "";
        try
        {
            browserDriver.Navigate().GoToUrl(url);
            html = browserDriver.PageSource;
            if (string.IsNullOrEmpty(html)) _log.Write($"Error occurred in Eztv WebScrape {html}", "Acquire Media", 0);
        }
        catch (Exception e)
        {
            _log.Write($"Error occurred in Eztv WebScrape {e.Message}", "Acquire Media", 0);
        }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var magnetLinks = htmlDoc.DocumentNode.Descendants("a")
                                 .Where(a => a.Attributes["href"] != null &&
                                             a.Attributes["href"].Value.StartsWith("magnet", StringComparison.OrdinalIgnoreCase))
                                 .ToList();

        foreach (var magnetInfo in magnetLinks)
        {
            var mgi = magnetInfo.Attributes["href"].Value.ToLower();
            if (mgi.Contains(compareWithMagnet) == false ||
                mgi                             == "") continue;
            var magnetReplace = mgi.Replace("<a href=\"", "");
            var magnSplit     = magnetReplace.Split("><img src=");
            var magnet        = magnSplit[0];
            var priority      = PrioritizeMagnet(magnet, "Eztv");
            if (priority > 130)
            {
                var prioritizedMagnet = priority + "#$# " + magnet;
                _log.Write($"Prioritized Magnet recorded: {prioritizedMagnet}", "Eztv", 4);
                Magnets.Add(prioritizedMagnet);
                foundMagnets++;
            }
        }

        if (foundMagnets == 0)
        {
            _log.Write("No result returned from the WebScrape Eztv", "Acquire Media", 4);
            return;
        }

        Magnets.Sort();
        Magnets.Reverse();
        _log.Write($"Found {foundMagnets} via EZTV", "Acquire Media");
    }
    private string BuildEztvUrl(string showName, string seasEpi)
    {
        var eztvUrl = "https://eztv1.xyz/search/";
        showName =  Common.RemoveSpecialCharsInShowName(showName);
        showName =  showName.Replace(" ", "-");
        eztvUrl  += showName + "-" + seasEpi;
        _log.Write($"URL EZTV is {eztvUrl}", "Eztv", 4);
        return eztvUrl;
    }
    public void GetMagnetDlMagnets(string showName, string seasEpi)
    {
        var foundMagnets = 0;
        var html         = BuildMagnetDownloadUrl($"{showName}-{seasEpi}");

        var compareWithMagnet = "dn="                                                           +
                                Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") +
                                "."                                                             +
                                seasEpi                                                         +
                                ".";
        _log.Write($"Compare string = {compareWithMagnet}", "MagnetDL", 4);

        HtmlWeb      web = new();
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
            _log.Write("No result returned from the WebScrape MagnetDL", "Acquire Media", 4);
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
                    _log.Write($"Prioritized Magnet recorded: {prioritizedMagnet}", "AcquireMedia", 4);
                    Magnets.Add(prioritizedMagnet);
                    foundMagnets++;
                }
            }

        // if (foundMagnets == 0)
        // {
        //     _log.Write("No result returned from WebScrape MagnetDL", "Acquire Media");
        //     return;
        // }

        Magnets.Sort();
        Magnets.Reverse();
        _log.Write($"Found {foundMagnets} via MagnetDL", "Acquire Media");
    }
    private string BuildMagnetDownloadUrl(string showName)
    {
        var url = "https://www.magnetdl.com/";
        showName = Common.RemoveSpecialCharsInShowName(showName);
        showName = showName.Replace(" ", "-");
        url      = url + "/" + showName[0].ToString().ToLower() + "/" + showName + "/";
        _log.Write($"URL MagnetDL is {url}", "MagnetDL", 4);
        return url;
    }
    public void GetTorrentz2Magnets(string showName, string seasEpi)
    {
        try
        {
            var foundMagnets = 0;
            var url          = BuildTorrentZUrl($"{showName}+{seasEpi}");

            var compareWithMagnet = ""                                                              +
                                    Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") +
                                    "."                                                             +
                                    seasEpi                                                         +
                                    ".";

            var options = new ChromeOptions();
            options.AddArgument("--headless");
            //options.AddArgument("--disable-dev-shm-usage");
            var driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl(url);
            var html = driver.PageSource;
            driver.Quit();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            //htmlDoc.Load(@"/Users/dick/Desktop/test.html");

            var magnetLinks = htmlDoc.DocumentNode.Descendants("a")
                                     .Where(a => a.Attributes["href"] != null &&
                                                 a.Attributes["href"].Value.StartsWith("magnet", StringComparison.OrdinalIgnoreCase))
                                     .ToList();
            if (magnetLinks == null) return;
            foreach (var magnetInfo in magnetLinks)
            {
                var mgi = magnetInfo.Attributes["href"].Value.ToLower();
                if (mgi.Contains(compareWithMagnet) == false ||
                    mgi                             == "") continue;
                var magnetReplace = mgi.Replace("<a href=\"", "");
                var magnetSplit   = magnetReplace.Split("\n><i");
                var magnet        = magnetSplit[0];
                var priority      = PrioritizeMagnet(magnet, "TorrentZ");
                if (priority > 130)
                {
                    var prioritizedMagnet = priority + "#$# " + magnet;
                    _log.Write($"Prioritized Magnet recorded: {prioritizedMagnet}", "TorrentZ", 4);
                    Magnets.Add(prioritizedMagnet);
                    foundMagnets++;
                }
            }

            if (foundMagnets == 0)
            {
                _log.Write("No result returned from the WebScrape TorrentZ", "Acquire Media", 4);
                return;
            }

            Magnets.Sort();
            Magnets.Reverse();
            _log.Write($"Found {foundMagnets} via TorrentZ", "Acquire Media");
        }
        catch (Exception e)
        {
            _log.Write($"Error occurred in TorrentZ2 WebScrape {e}", "Acquire Media", 0);
        }
    }
    private string BuildTorrentZUrl(string showName)
    {
        var eztvUrl = "https://torrentz2.nz/search?q=";
        showName =  Common.RemoveSpecialCharsInShowName(showName);
        showName =  showName.Replace(" ", "+");
        eztvUrl  += showName;
        _log.Write($"URL EZTV is {eztvUrl}", "Eztv", 4);
        return eztvUrl;
    }
    public void GetPirateBayMagnets(string showName, string seasEpi, ChromeDriver browserDriver)
    {
        var foundMagnets = 0;
        var url          = BuildPirateBayUrl(showName, seasEpi);

        var compareWithMagnet = "dn="                                                           +
                                Common.RemoveSpecialCharsInShowName(showName).Replace(" ", "+") +
                                "+"                                                             +
                                seasEpi                                                         +
                                "+";
        _log.Write($"Compare string = {compareWithMagnet}", "PirateBay", 4);

        var html = "";
        try
        {
            browserDriver.Navigate().GoToUrl(url);
            html = browserDriver.PageSource;
            if (string.IsNullOrEmpty(html))
                _log.Write($"Error occurred in PirateBay Page Returned {html}", "Acquire Media", 0);
        }
        catch (Exception e)
        {
            _log.Write($"Error occurred in PirateBay WebScrape {e.Message}", "Acquire Media", 0);
        }

        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var magnetLinks = htmlDoc.DocumentNode.Descendants("a")
                                 .Where(a => a.Attributes["href"] != null &&
                                             a.Attributes["href"].Value.StartsWith("magnet", StringComparison.OrdinalIgnoreCase))
                                 .ToList();

        foreach (var magnetInfo in magnetLinks)
        {
            var mgi = magnetInfo.Attributes["href"].Value.ToLower();
            if (mgi.Contains(compareWithMagnet) == false ||
                mgi                             == "") continue;
            var magnetReplace = mgi.Replace("<a href=\"", "");
            var magnSplit     = magnetReplace.Split("><img src=");
            var magnet        = magnSplit[0];
            var priority      = PrioritizeMagnet(magnet, "PirateBay");
            if (priority > 130)
            {
                var prioritizedMagnet = priority + "#$# " + magnet;
                _log.Write($"Prioritized Magnet recorded: {prioritizedMagnet}", "PirateBay", 4);
                Magnets.Add(prioritizedMagnet);
                foundMagnets++;
            }
        }

        if (foundMagnets == 0)
        {
            _log.Write("No result returned from the WebScrape PirateBay", "Acquire Media", 4);
            return;
        }

        Magnets.Sort();
        Magnets.Reverse();
        _log.Write($"Found {foundMagnets} via PirateBay", "Acquire Media");
    }
    private string BuildPirateBayUrl(string showName, string seasEpi)
    {
        var url = "https://bayofpirates.xyz/search.php/?q=";
        showName = Common.RemoveSpecialCharsInShowName(showName);
        showName = showName.Replace(" ", "-");
        url      = url + showName + "-" + seasEpi; //+ "&cat=0";
        _log.Write($"URL PirateBay is {url}", "PirateBay", 4);
        return url;
    }
    private static int PrioritizeMagnet(string magnet, string provider)
    {
        var priority = provider switch
                       {
                           "Eztv" or "EztvAPI" => 110,
                           "PirateBay" => 115,
                           "MagnetDL" => 120, // Does not have container info so +10 by default
                           "TorrentZ" => 105,
                           _ => 0,
                       };
        // Codex values
        if (magnet.ToLower().Contains("x264")  ||
            magnet.ToLower().Contains("h264")  ||
            magnet.ToLower().Contains("x.264") ||
            magnet.ToLower().Contains("h.264"))
            priority += 60;
        // else if (magnet.ToLower().Contains("xvid"))
        //     priority += 30;
        else if ((magnet.ToLower().Contains("x265")  ||
                  magnet.ToLower().Contains("h265")  ||
                  magnet.ToLower().Contains("x.265") ||
                  magnet.ToLower().Contains("h.265")) &&
                 !magnet.ToLower().Contains("hvec"))
            priority += 65;
        else if (magnet.ToLower().Contains("hevc"))
            priority += 55;
        // Resolution values
        if (magnet.ToLower().Contains("2160p.") ||
            magnet.ToLower().Contains("4k.")    ||
            magnet.ToLower().Contains("2160p+") ||
            magnet.ToLower().Contains("4k+"))
            priority += 20;
        else if (magnet.ToLower().Contains("1080p.") || magnet.ToLower().Contains("1080p+"))
            priority += 18;
        else if (magnet.ToLower().Contains("hdtv.") || magnet.ToLower().Contains("hdtv+"))
            priority += 16;
        else if (magnet.ToLower().Contains("720p.") || magnet.ToLower().Contains("720p+"))
            priority += 2;
        // Container values
        if (magnet.ToLower().Contains(".mkv") || magnet.ToLower().Contains("+mka"))
            priority += 10;
        else if (magnet.ToLower().Contains(".mp4") || magnet.ToLower().Contains("+mp4"))
            priority += 5;
        // Wrong Languages
        if (magnet.ToLower().Contains(".italian.") ||
            magnet.ToLower().Contains(".ita.")     ||
            magnet.ToLower().Contains("+italian+") ||
            magnet.ToLower().Contains("+ita+")) priority -= 75;
        // Good Torrents
        if (magnet.ToLower().Contains("-ethel"))
            priority += 5;

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
    public Tuple<bool, string> PerformShowEpisodeMagnetsSearch(string showName,
        int                                                           seasNum,
        int                                                           epiNum,
        TextFileHandler                                               logger,
        ChromeDriver                                                  browserDriver)
    {
        var                 log = logger;
        string              seasEpi;
        var                 magnet = "";
        Tuple<bool, string> result = new(false, "");

        if (epiNum == 1) //Search for whole season first
        {
            seasEpi = Common.BuildSeasonOnly(seasNum);
            magnet  = PerformFindMagnet(showName, seasEpi, log, browserDriver);
            result  = new Tuple<bool, string>(true, magnet);
        }

        if (magnet == "")
        {
            if (epiNum == 1)
                log.Write(
                          $"No Magnet found for the whole season {seasNum} of {showName}");
            seasEpi = Common.BuildSeasonEpisodeString(seasNum, epiNum);
            magnet  = PerformFindMagnet(showName, seasEpi, log, browserDriver);
            result  = new Tuple<bool, string>(false, magnet);
        }

        return result;
    }
    private string PerformFindMagnet(string showName, string seasEpi, TextFileHandler log, ChromeDriver browserDriver)
    {
        using WebScrape seasonScrape = new(_appInfo);
        {
            seasonScrape.Magnets = new List<string>();

            //seasonScrape.GetTorrentz2Magnets(showName, seasEpi);
            seasonScrape.GetEztvMagnets(showName, seasEpi, browserDriver);
            seasonScrape.GetMagnetDlMagnets(showName, seasEpi);
            //seasonScrape.GetPirateBayMagnets(showName, seasEpi, browserDriver);


            switch (seasonScrape.Magnets.Count)
            {
                case > 0:
                {
                    log.Write($"Total Magnets found {seasonScrape.Magnets.Count}", "Acquire Media");
                    var temp   = seasonScrape.Magnets[0].Split("#$#");
                    var magnet = temp[1];
                    return magnet;
                }
            }
        }
        return "";
    }
}

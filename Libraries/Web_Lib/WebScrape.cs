using System;
using System.Collections.Generic;
using System.Linq;
using Common_Lib;
using DB_Lib_EF.Entities;
using HtmlAgilityPack;

namespace Web_Lib;

public class WebScrape : IDisposable
{
    private readonly string _thisProgram;
    public List<string> Magnets = new();
    public WebScrape(string info) { _thisProgram = info; }
    public void Dispose() { GC.SuppressFinalize(this); }

    public void GetMagnetDlMagnets(string showName, string seasEpi)
    {
        var foundMagnets = 0;
        //var html = BuildMagnetDownloadUrl($"{showName}-{seasEpi}");
        var html = BuildMagnetDownloadUrl($"{showName}+{seasEpi}");

        var compareWithMagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") + "." + seasEpi + ".";
        var compareWithMagnet2 = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", "+") + "+" + seasEpi + "+";
        LogModel.Record(_thisProgram, "WebScrape - MagnetDL", $"Compare string = {compareWithMagnet} and {compareWithMagnet2}", 5);

        HtmlWeb web = new();
        HtmlDocument htmlDoc;

        try
        {
            htmlDoc = web.Load(html);
        }
        catch (HtmlWebException e)
        {
            LogModel.Record(_thisProgram, "WebScrape - MagnetDL", $"Web.Load exception {e.Message}  ::: {e.InnerException}", 20);
            LogModel.Record(_thisProgram, "WebScrape - MagnetDL", $"Number of magnets for {showName} {seasEpi} found: {foundMagnets}", 2);

            return;
        }

        var table = htmlDoc.DocumentNode.SelectNodes("//td/a");

        if (table is null)
        {
            LogModel.Record(_thisProgram, "WebScrape - MagnetDL", $"Number of magnets for {showName} {seasEpi} found: 0", 2);

            return;
        }

        foreach (var node in table)
        {
            if (!node.Attributes["href"].Value.ToLower().Contains("magnet:") || (!node.Attributes["href"].Value.ToLower().Contains(compareWithMagnet) &&
                                                                                 !node.Attributes["href"].Value.ToLower().Contains(compareWithMagnet2)))
            {
                continue;
            }
            var priority = PrioritizeMagnet(node.Attributes["href"].Value, "MagnetDL");

            if (priority <= 130)
            {
                continue;
            }
            var prioritizedMagnet = priority + "#$# " + node.Attributes["href"].Value;
            LogModel.Record(_thisProgram, "WebScrape - MagnetDL", $"Prioritized Magnet recorded: {prioritizedMagnet}", 5);
            Magnets.Add(prioritizedMagnet);
            foundMagnets++;
        }

        LogModel.Record(_thisProgram, "WebScrape - MagnetDL", $"Number of magnets for {showName} {seasEpi} found: {foundMagnets}", 2);

        if (foundMagnets == 0)
        {
            return;
        }

        Magnets.Sort();
        Magnets.Reverse();
    }

    private static string BuildMagnetDownloadUrl(string showName)
    {
        //var url = "https://www.magnetdl.com/";
        var url = "https://magnetdl.ninjaproxy1.com/search/?q=";
        showName = Common.RemoveSpecialCharsInShowName(showName);
        //showName = showName.Replace(" ", "-");
        showName = showName.Replace(" ", "+");
        //url = url + "/" + showName[0].ToString().ToLower() + "/" + showName + "/";
        url += showName + "&m=1&x=0&y-0";

        return url;
    }

    public void GetPirateBayMagnets(string showName, string seasEpi)
    {
        var foundMagnets = 0;
        var url = BuildPirateBayUrl(showName, seasEpi);
        var compareWithMagnet = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", "+") + "+" + seasEpi + "+";
        var compareWithMagnet2 = "dn=" + Common.RemoveSpecialCharsInShowName(showName).Replace(" ", "%20") + "%20" + seasEpi + "%20";
        LogModel.Record(_thisProgram, "WebScrape - PirateBay", $"Compare string = {compareWithMagnet} and {compareWithMagnet2}", 5);
        var htmlDoc = new HtmlDocument();
        try
        {
            using var selenium = new Selenium(_thisProgram);
            selenium.Start();
            htmlDoc = selenium.GetPage(url);
            if (string.IsNullOrEmpty(htmlDoc.DocumentNode.InnerHtml))
            {
                LogModel.Record(_thisProgram, "WebScrape - PirateBay", "No HTML was returned", 6);
            }
            selenium.Stop();
        }
        catch (Exception e)
        {
            LogModel.Record(_thisProgram, "WebScrape - PirateBay", $"Error occurred in Eztv WebScrape {e.Message} ::: {e.InnerException}", 20);
        }

        var magnetLinks = htmlDoc.DocumentNode.Descendants("a")
                                 .Where(a => a.Attributes["href"] != null && a.Attributes["href"].Value.StartsWith("magnet", StringComparison.OrdinalIgnoreCase))
                                 .ToList();

        foreach (var prioritizedMagnet in from magnetInfo in magnetLinks
                                          select magnetInfo.Attributes["href"].Value.ToLower()
                                          into mgi
                                          where (mgi.ToLower().Contains(compareWithMagnet) || mgi.ToLower().Contains(compareWithMagnet2)) && mgi != ""
                                          select mgi.Replace("<a href=\"", "")
                                          into magnetReplace
                                          select magnetReplace.Split("><img src=")
                                          into magnetSplit
                                          select magnetSplit[0]
                                          into magnet
                                          let priority = PrioritizeMagnet(magnet, "PirateBay")
                                          where priority > 130
                                          select priority + "#$# " + magnet)
        {
            LogModel.Record(_thisProgram, "WebScrape - PirateBay", $"Prioritized Magnet recorded: {prioritizedMagnet}", 4);
            Magnets.Add(prioritizedMagnet);
            foundMagnets++;
        }

        LogModel.Record(_thisProgram, "WebScrape - PirateBay", $"Number of magnets for {showName} {seasEpi} found: {foundMagnets}", 2);

        if (foundMagnets == 0)
        {
            return;
        }

        Magnets.Sort();
        Magnets.Reverse();
    }

    private static string BuildPirateBayUrl(string showName, string seasEpi)
    {
        //var url = "https://bayofpirates.xyz/search.php/?q=";
        var url = "https://thepiratebay.org/search.php?q=";
        showName = Common.RemoveSpecialCharsInShowName(showName);
        showName = showName.Replace(" ", "+");
        url = url + showName + "+" + seasEpi + "&cat=0";

        return url;
    }

    public void GetTorrentz2Magnets(string showName, string seasEpi)
    {
        try
        {
            var foundMagnets = 0;
            var url = BuildTorrentZUrl($"{showName}+{seasEpi}");
            var compareWithMagnet = Common.RemoveSpecialCharsInShowName(showName).Replace(" ", ".") + "." + seasEpi + ".";
            var compareWithMagnet2 = Common.RemoveSpecialCharsInShowName(showName) + " " + seasEpi;
            LogModel.Record(_thisProgram, "WebScrape - TorrentZ2", $"Compare string = {compareWithMagnet}", 5);

            var htmlDoc = new HtmlDocument();
            try
            {
                using var selenium = new Selenium(_thisProgram);
                selenium.Start();
                htmlDoc = selenium.GetPage(url);
                if (string.IsNullOrEmpty(htmlDoc.DocumentNode.InnerHtml))
                {
                    LogModel.Record(_thisProgram, "WebScrape - TorrentZ2", "No HTML was returned", 6);
                }
                selenium.Stop();
            }
            catch (Exception e)
            {
                LogModel.Record(_thisProgram, "WebScrape - TorrentZ2", $"Error occurred in Eztv WebScrape {e.Message} ::: {e.InnerException}", 20);
            }

            var torrents = new Dictionary<string, string>();
            foreach (var dl in htmlDoc.DocumentNode.Descendants("dl"))
            {
                // ReSharper disable once StringLiteralTypo
                var name = dl.Descendants("dt").FirstOrDefault()?.Descendants("a")?.FirstOrDefault()?.InnerText;
                if (name == null) continue;
                if (!name.ToLower().Contains(compareWithMagnet.ToLower()) && !name.ToLower().Contains(compareWithMagnet2.ToLower()))
                {
                    continue;
                }
                var magnetLink = dl.Descendants("dd").FirstOrDefault()?.Descendants("a")?.FirstOrDefault()?.GetAttributeValue("href", "");
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(magnetLink) && magnetLink.StartsWith("magnet:?"))
                {
                    torrents.TryAdd(name, magnetLink);
                }
            }

            foreach (var magnetInfo in torrents)
            {
                var magnet = magnetInfo.Key;
                var priority = PrioritizeMagnet(magnet, "TorrentZ");
                if (priority <= 130)
                {
                    continue;
                }

                var prioritizedMagnet = priority + "#$# " + magnetInfo.Value;
                LogModel.Record(_thisProgram, "WebScrape - TorrentZ", $"Prioritized Magnet recorded for {magnetInfo.Key} ::: {prioritizedMagnet}", 5);
                Magnets.Add(prioritizedMagnet);
                foundMagnets++;
            }
            LogModel.Record(_thisProgram, "WebScrape - TorrentZ2", $"Number of magnets for {showName} {seasEpi} found: {foundMagnets}", 2);

            if (foundMagnets == 0)
            {
                return;
            }

            Magnets.Sort();
            Magnets.Reverse();
        }
        catch (Exception e)
        {
            LogModel.Record(_thisProgram, "WebScrape - TorrentZ2", $"Error: {e.Message}  ::: {e.InnerException}", 20);
        }
    }

    private static string BuildTorrentZUrl(string showName)
    {
        var eztvUrl = "https://torrentz2.nz/search?q=";
        showName = Common.RemoveSpecialCharsInShowName(showName);
        showName = showName.Replace(" ", "+");
        eztvUrl += showName;

        return eztvUrl;
    }

    private static int PrioritizeMagnet(string magnet, string provider)
    {
        magnet = magnet.Replace("%20", ".");
        var priority = provider switch
        {
            //"Eztv" or "EztvAPI" => 110,
            "PirateBay" => 110, // Does not have container info so +10 by default
            "MagnetDL" => 115,  // Does not have container info so +10 by default and MagnetDl is preferred
            "TorrentZ" => 100,
            _ => 0,
        };

        // Codex values
        if (magnet.ToLower().Contains("x264") || magnet.ToLower().Contains("h264") || magnet.ToLower().Contains("x.264") || magnet.ToLower().Contains("h.264"))
        {
            priority += 15;
        }
        else
        {
            if ((magnet.ToLower().Contains("x265") || magnet.ToLower().Contains("h265") || magnet.ToLower().Contains("x.265") || magnet.ToLower().Contains("h.265")))
            {
                priority += 25;
            }
        }

        // Resolution values
        if (magnet.ToLower().Contains("2160p.") || magnet.ToLower().Contains("4k.") || magnet.ToLower().Contains("2160p+") || magnet.ToLower().Contains("4k+"))
        {
            priority += 30;
        } else
        {
            if (magnet.ToLower().Contains("1080p.") || magnet.ToLower().Contains("1080p+"))
            {
                priority += 25;
            } else
            {
                if (magnet.ToLower().Contains("hdtv.") || magnet.ToLower().Contains("hdtv+"))
                {
                    priority += 15;
                } else
                    if (magnet.ToLower().Contains("720p.") || magnet.ToLower().Contains("720p+"))
                    {
                        priority += 10;
                    } else
                    {
                        priority -= 15;
                    }
            }
        }

        // Container values
        if (magnet.ToLower().Contains(".mkv") || magnet.ToLower().Contains("+mkv"))
        {
            priority += 10;
        } else
        {
            if (magnet.ToLower().Contains(".mp4") || magnet.ToLower().Contains("+mp4"))
            {
                priority += 5;
            }
        }

        // Wrong Languages
        if (magnet.ToLower().Contains(".italian.") || magnet.ToLower().Contains(".ita.") || magnet.ToLower().Contains("+italian+") || magnet.ToLower().Contains("+ita+"))
        {
            priority -= 75;
        }

        // Good Torrents
        if (magnet.ToLower().Contains("-ethel"))
        {
            priority += 5;
        }

        // Bad Torrents
        if (magnet.ToLower().Contains("-minx") || magnet.ToLower().Contains("torrenting.com"))
        {
            priority -= 75;
        }

        return priority;
    }
}
public class Magnets
{
    private readonly string _thisProgram;
    public Magnets(string info) { _thisProgram = info; }

    public Tuple<bool, string> PerformShowEpisodeMagnetsSearch(string showName, int seasNumber, int epiNumber)
    {
        string seasEpi;
        var magnet = "";
        Tuple<bool, string> result = new(false, "");

        if (epiNumber == 1) //Search for whole season first
        {
            seasEpi = Common.BuildSeasonOnly(seasNumber);
            magnet = PerformFindMagnet(showName, seasEpi);
            result = new Tuple<bool, string>(true, magnet);
        }

        switch (magnet)
        {
            case "":
                seasEpi = Common.BuildSeasonEpisodeString(seasNumber, epiNumber);
                magnet = PerformFindMagnet(showName, seasEpi);
                result = new Tuple<bool, string>(false, magnet);
                break;
        }

        return result;
    }

    private string PerformFindMagnet(string showName, string seasEpi)
    {
        using WebScrape seasonScrape = new(_thisProgram);
        seasonScrape.Magnets = new List<string>();
        //seasonScrape.GetMagnetDlMagnets(showName, seasEpi);
        seasonScrape.GetPirateBayMagnets(showName, seasEpi);
        //seasonScrape.GetTorrentz2Magnets(showName, seasEpi);

        switch (seasonScrape.Magnets.Count)
        {
            case > 0:
            {
                var temp = seasonScrape.Magnets[0].Split("#$#");
                var magnet = temp[1];

                return magnet;
            }
        }

        return "";
    }
}

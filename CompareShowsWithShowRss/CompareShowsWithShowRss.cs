using System.IO;
using HtmlAgilityPack;
using DB_Lib_EF.Entities;
using System.Collections.Generic;

using DB_Lib_EF.Models.MariaDB;

namespace CompareShowsWithShowRss;

internal static class CompareShowsWithShowRss
{
    private static void Main()
    {
        const string thisProgram = "Compare ShowRss";
        LogModel.Start(thisProgram);

        // Read the HTML file
        var htmlContent = File.ReadAllText("/media/psf/TVMazeLinux/Inputs/allShowRss.html");

        // Load the content to HtmlDocument
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        // Get all option elements
        var options = htmlDoc.DocumentNode.SelectNodes("//option");

        var showNamesFound = new List<string>();

        // Process each option
        if (options != null)
        {
            foreach (var option in options)
            {
                var showName = option.InnerText;
                showNamesFound.Add(showName);
            }
        }

        using var db = new TvMaze();

        var foundCount = 0;

        foreach (var rssShowName in showNamesFound)
        {
            var compareShowName = rssShowName.Replace("&amp;", "&").Replace("&#039;", "'");
            var showRec         = db.Shows.Where(s => (s.ShowName == compareShowName || s.CleanedShowName == compareShowName || s.AltShowname == compareShowName) && s.Finder != "Skip");

            if (showRec == null) continue;

            if (showRec.Count() > 1)
            {
                foreach (var show in showRec)
                {
                    LogModel.Record(thisProgram, "Main", $"Multiple records found for show: {compareShowName}, ShowId: {show.Id}, ShowName: {show.ShowName}", 2);

                    continue;
                }
            } else
            {
                if (showRec.Count() == 0)
                {
                    LogModel.Record(thisProgram, "Main", $"No record found for show: {compareShowName}", 3);

                    continue;
                }

                foundCount++;

                foreach (var show in showRec)
                {
                    LogModel.Record(thisProgram, "Main", $"Found Combination for show: {compareShowName}, ShowId: {show.Id}, ShowName: {show.ShowName}", 1);

                    show.Finder = "ShowRss";
                }

                db.SaveChanges();
            }
        }

        LogModel.Stop(thisProgram);
    }
}

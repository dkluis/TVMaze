using System.IO;

using HtmlAgilityPack;

using DB_Lib_EF.Entities;

using System.Collections.Generic;

using Common_Lib;

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

        LogModel.Record(thisProgram, "Main", $"Found {showNamesFound.Count} records at ShowRss.info", 1);

        using var db = new TvMaze();

        var foundCount = 0;
        var multiCount = 0;
        var notCount   = 0;

        foreach (var rssShowName in showNamesFound)
        {
            if (string.IsNullOrEmpty(rssShowName) || string.IsNullOrWhiteSpace(rssShowName)) continue;
            var compareShowName        = rssShowName.Replace("&amp;", "&").Replace("&#039;", "'");
            var compareCleanedShowName = Common.RemoveSpecialCharsInShowName(rssShowName);

            var showRec = db.Shows.Where(s => (s.ShowName        == compareShowName         ||
                                               s.CleanedShowName == compareCleanedShowName  ||
                                               s.AltShowname     == compareShowName         ||
                                               s.AltShowname     == compareCleanedShowName) &&
                                               s.Finder          != "Skip");

            if (showRec == null) continue;

            if (showRec.Count() > 1)
            {
                multiCount++;

                foreach (var show in showRec)
                {
                    LogModel.Record(thisProgram, "Main", $"Multiple records found for show: {compareShowName}, ShowId: {show.Id}, ShowName: {show.ShowName}", 3);
                }
            } else
            {
                notCount++;

                if (showRec.Count() == 0)
                {
                    LogModel.Record(thisProgram, "Main", $"No record found for show: {compareShowName}", 4);

                    continue;
                }

                foundCount++;

                foreach (var show in showRec)
                {
                    LogModel.Record(thisProgram, "Main", $"Found Combination for show: {compareShowName}, ShowId: {show.Id}, ShowName: {show.ShowName}", 2);

                    show.Finder = "ShowRss";
                }

                db.SaveChanges();
            }
        }

        LogModel.Record(thisProgram, "Main", $"Processed: Found {foundCount} and Multiple {multiCount} and Not Found {notCount} records", 1);
        LogModel.Stop(thisProgram);
    }
}

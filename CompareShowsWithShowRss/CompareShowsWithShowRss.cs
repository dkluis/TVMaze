using System.IO;

using HtmlAgilityPack;

using DB_Lib_EF.Entities;

using System.Collections.Generic;

using Common_Lib;

using DB_Lib_EF.Models.MariaDB;

using Web_Lib;

namespace CompareShowsWithShowRss;

internal static class CompareShowsWithShowRss
{
    private static void Main()
    {
        const string thisProgram = "Compare ShowRss";
        LogModel.Start(thisProgram);

        // Get the latest from the website
        var sel = new Selenium(thisProgram);
        sel.Start();
        var htmlDoc =sel.GetPage(@"https://showrss.info/browse");
        sel.Stop();

        var dates        = htmlDoc.DocumentNode.SelectNodes("//strong");
        var showRssDates = new List<string>();

        foreach (var date in dates)
        {
            var showRssDate = date.InnerText;

            if (showRssDate.Contains("/"))
            {
                var dateStr                  = DateTime.ParseExact(showRssDate, "MM/dd/yyyy", null);
                var reformattedDateString = dateStr.ToString("yyyy/MM/dd");
                showRssDates.Add(reformattedDateString);
            }
        }

        if (showRssDates.Count < 0)
        {
            LogModel.Record(thisProgram, "Main", "No Updates found at ShowRss Browse", 1);
            LogModel.Stop(thisProgram);
            Environment.Exit(9);
        }

        showRssDates.OrderDescending();
        var       lastDateFound   = showRssDates[0];
        using var db              = new TvMaze();
        var       lastShowRssDate = db.Configurations.Single(c => c.Key == "LastShowRssDate").Value;

        if (lastShowRssDate != lastDateFound)
        {
            db.Configurations.Single(c => c.Key == "LastShowRssDate").Value = lastDateFound;
            db.SaveChanges();
            LogModel.Record(thisProgram, "Main", $"Updated LastShowRssDate to {lastDateFound}", 1);
        } else
        {
            LogModel.Record(thisProgram, "Main", $"Last Update Date {lastDateFound} and {lastShowRssDate} is already processed", 1);
            LogModel.Stop(thisProgram);
            Environment.Exit(9);
        }

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
                if (showRec.Count() == 0)
                {
                    LogModel.Record(thisProgram, "Main", $"No record found for show: {compareShowName}", 3);
                    notCount++;

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

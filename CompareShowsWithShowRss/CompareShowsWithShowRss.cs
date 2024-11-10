using Common_Lib;
using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;
using HtmlAgilityPack;
using Web_Lib;

namespace CompareShowsWithShowRss;

internal static class CompareShowsWithShowRss
{
    private static void Main()
    {
        const string thisProgram = "Compare ShowRss";
        LogModel.Start(thisProgram);

        var htmlData = GetHtmlAsync("https://showrss.info/browse");
        var htmlDoc = new HtmlDocument();
        if (htmlData.Result == "")
        {
            LogModel.Record(thisProgram, "Main", $"Could not find HTML data for", 1);
            LogModel.Stop(thisProgram);
            Environment.Exit(99);
        }
        htmlDoc.LoadHtml(htmlData.Result);

        var dates        = htmlDoc.DocumentNode.SelectNodes("//strong");
        var showRssDates = new List<string>();

        foreach (var date in dates)
        {
            var showRssDate = date.InnerText;

            if (showRssDate.Contains("/"))
            {
                var dateStr               = DateTime.ParseExact(showRssDate, "MM/dd/yyyy", null);
                var reformattedDateString = dateStr.ToString("yyyy/MM/dd");
                showRssDates.Add(reformattedDateString);
            }
        }

        if (showRssDates.Count < 0)
        {
            LogModel.Record(thisProgram, "Main", "No Updates found at ShowRss Browse");
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
            LogModel.Record(thisProgram, "Main", $"Updated LastShowRssDate to {lastDateFound}");
        } else
        {
            LogModel.Record(thisProgram, "Main", $"Last Update Date {lastDateFound} and {lastShowRssDate} is already processed");
            LogModel.Stop(thisProgram);
            Environment.Exit(9);
        }

        // Get all option elements
        var options        = htmlDoc.DocumentNode.SelectNodes("//option");
        var showNamesFound = new List<string>();

        // Process each option
        if (options != null)
            foreach (var option in options)
            {
                var showName = option.InnerText;
                showNamesFound.Add(showName);
            }

        LogModel.Record(thisProgram, "Main", $"Found {showNamesFound.Count} records at ShowRss.info");

        var foundCount = 0;
        var multiCount = 0;
        var notCount   = 0;

        foreach (var rssShowName in showNamesFound)
        {
            if (string.IsNullOrEmpty(rssShowName) || string.IsNullOrWhiteSpace(rssShowName)) continue;
            var compareShowName        = rssShowName.Replace("&amp;", "&").Replace("&#039;", "'");
            var compareCleanedShowName = Common.RemoveSpecialCharsInShowName(rssShowName).Replace("ʻ", "");

            var showRec = db.Shows.Where(s => (s.ShowName == compareShowName || s.CleanedShowName == compareCleanedShowName || s.AltShowname == compareShowName || s.AltShowname == compareCleanedShowName) && s.Finder != "Skip");

            if (showRec == null) continue;

            if (showRec.Count() > 1)
            {
                multiCount++;

                foreach (var show in showRec) LogModel.Record(thisProgram, "Main", $"Multiple records found for show: {compareShowName}, ShowId: {show.TvmShowId}, ShowName: {show.ShowName}", 3);
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
                    LogModel.Record(thisProgram, "Main", $"Found Combination for show: {compareShowName}, ShowId: {show.TvmShowId}, ShowName: {show.ShowName}", 2);

                    show.Finder = "ShowRss";
                }

                db.SaveChanges();
            }
        }

        LogModel.Record(thisProgram, "Main", $"Processed: Found {foundCount} and Multiple {multiCount} and Not Found {notCount} records");
        LogModel.Stop(thisProgram);
    }

    private static async Task<string> GetHtmlAsync(string url)
    {
        using var client = new HttpClient();
        try
        {
            var response = await client.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var htmlContent = await response.Content.ReadAsStringAsync();
            return htmlContent;
        }
        catch (HttpRequestException e)
        {
            LogModel.Record("Compare ShowRss", "GetHtml", $"HTML Exception {e.Message} ::: {e.InnerException}", 20);
            return "";
        }
    }
}

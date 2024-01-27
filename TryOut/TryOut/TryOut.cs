using System.Linq;

using Common_Lib;

using DB_Lib_EF.Models.MariaDB;

namespace TryOut;

internal static class TryOut
{
    private static void Main()
    {
        const string thisProgram = "TryOut";
        AppInfo      appInfo     = new("TVMaze", thisProgram, "DbAlternate");

        //LogModel.Start(thisProgram);

        var testString    = "Australia''s Ocean Odyssey: A Journey Down the East Australian Current";
        var onlyString    = testString.Replace("''", "'");
        var cleanedString = Common.RemoveSpecialCharsInShowName(testString);

        var db    = new TvMaze();
        var shows = db.Shows.Where(s => s.ShowName.Contains("''") || s.CleanedShowName.Contains("''") || s.AltShowname.Contains("''")).ToList();

        foreach (var show in shows)
        {
            show.ShowName        = show.ShowName.Replace("''", "'");
            show.CleanedShowName = show.CleanedShowName.Replace("''", "'");
            show.AltShowname     = show.AltShowname.Replace("''", "'");
        }

        db.SaveChanges();

        //LogModel.Stop(thisProgram);
    }
}

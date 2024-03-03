using System.Linq;

using Common_Lib;

using DB_Lib_EF.Models.MariaDB;

using Entities_Lib;

namespace TryOut;

internal static class TryOut
{
    private static void Main()
    {
        const string thisProgram = "TryOut";
        AppInfo      appInfo     = new("TVMaze", thisProgram, "DbAlternate");

        //LogModel.Start(thisProgram);

        var test = "Allegiance.S01E01";

        var result = GeneralMethods.FindShowEpisodeInfo("TryOut", test);

        //LogModel.Stop(thisProgram);
    }
}

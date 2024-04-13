using Common_Lib;
using DB_Lib_EF.Entities;
using Entities_Lib;

namespace CleanupPlexMedia;

internal static class CleanupPlexMedia
{
    private static void Main()
    {
        const string thisProgram = "Cleanup Plex Media";
        AppInfo      appInfo     = new("TVMaze", thisProgram, "DbAlternate");
        LogModel.Start(thisProgram);

        MediaFileHandler mfh              = new(appInfo);
        List<string>     showDirsToDelete = new();
        var              tvShowDirs       = Directory.GetDirectories(mfh.PlexMediaTvShows);
        var              tvKimShowDirs    = Directory.GetDirectories(mfh.PlexMediaKimTvShows);
        var              tvDickShowDirs   = Directory.GetDirectories(mfh.PlexMediaDickTvShows);
        var              allTvShowDirs    = new string[tvShowDirs.Length + tvKimShowDirs.Length + tvDickShowDirs.Length];
        tvShowDirs.CopyTo(allTvShowDirs, 0);
        tvKimShowDirs.CopyTo(allTvShowDirs, tvShowDirs.Length);
        tvDickShowDirs.CopyTo(allTvShowDirs, tvShowDirs.Length + tvKimShowDirs.Length);

        LogModel.Record(thisProgram, "Main", $"Directory Counts:  TV Shows: {tvShowDirs.Length}, Kim's TV Shows {tvKimShowDirs.Length}, Dick's TvShows {tvDickShowDirs.Length}");

        foreach (var dir in allTvShowDirs)
        {
            var seasonDirs = Directory.GetDirectories(dir);

            foreach (var seasonDir in seasonDirs)
            {
                var files = Directory.GetFiles(seasonDir);

                if (files.Length != 0)
                {
                    LogModel.Record(thisProgram, "Main", $"{seasonDir} has files {files.Length}", 5);

                    continue;
                }

                const bool deleteDir = true;

                try
                {
                    Directory.Delete(seasonDir);
                }
                catch (Exception ex)
                {
                    LogModel.Record(thisProgram, "Main", $"Exception on Delete of {seasonDir}: {ex.Message}  ::: {ex.InnerException}", 20);
                }

                if (deleteDir) showDirsToDelete.Add(dir);
                LogModel.Record(thisProgram, "Main", $"Deleted of {seasonDir}");
            }
        }

        foreach (var dir in showDirsToDelete)
        {
            if (Directory.GetDirectories(dir).Length == 0)
                try
                {
                    Directory.Delete(dir);
                }
                catch (Exception ex)
                {
                    LogModel.Record(thisProgram, "Main", $"Exception on Delete of {dir}: {ex.Message}  ::: {ex.InnerException}", 20);
                }

            LogModel.Record(thisProgram, "Main", $"Deleted of {dir}");
        }

        LogModel.Stop(thisProgram);
    }
}

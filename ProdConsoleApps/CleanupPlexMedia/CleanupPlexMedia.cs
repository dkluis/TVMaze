using Common_Lib;

using DB_Lib_EF.Entities;

using Entities_Lib;

namespace CleanupPlexMedia;

internal static class CleanupPlexMedia
{
    private static void Main()
    {
        const string thisProgram = "Cleanup Plex Media";
        Console.WriteLine($"{DateTime.Now}: {thisProgram}");
        AppInfo appInfo = new("TVMaze", thisProgram, "DbAlternate");
        var     log     = appInfo.TxtFile;
        log.Start();
        LogModel.Start(thisProgram, "Cleanup Plex Media");

        MediaFileHandler mfh              = new(appInfo);
        List<string>     showDirsToDelete = new();
        var              tvShowDirs       = Directory.GetDirectories(mfh.PlexMediaTvShows);
        var              tvKimShowDirs    = Directory.GetDirectories(mfh.PlexMediaKimTvShows);
        var              tvDickShowDirs   = Directory.GetDirectories(mfh.PlexMediaDickTvShows);
        var              allTvShowDirs    = new string[tvShowDirs.Length + tvKimShowDirs.Length + tvDickShowDirs.Length];
        tvShowDirs.CopyTo(allTvShowDirs, 0);
        tvKimShowDirs.CopyTo(allTvShowDirs, tvShowDirs.Length);
        tvDickShowDirs.CopyTo(allTvShowDirs, tvShowDirs.Length + tvKimShowDirs.Length);

        LogModel.Record(thisProgram, "Cleanup Plex Media", $"Directory Counts:  TV Shows: {tvShowDirs.Length}, Kim's TV Shows {tvKimShowDirs.Length}, Dick's TvShows {tvDickShowDirs.Length}");

        foreach (var dir in allTvShowDirs)
        {
            var seasonDirs = Directory.GetDirectories(dir);

            foreach (var seasonDir in seasonDirs)
            {
                var files = Directory.GetFiles(seasonDir);

                if (files.Length != 0)
                {
                    LogModel.Record(thisProgram, "Cleanup Plex Media", $"{seasonDir} has files {files.Length}", 5);

                    continue;
                }

                const bool deleteDir = true;

                try
                {
                    Directory.Delete(seasonDir);
                }
                catch (Exception ex)
                {
                    LogModel.Record(thisProgram, "Cleanup Plex Media", $"Exception on Delete of {seasonDir}: {ex.Message}", 6);
                }

                if (deleteDir) showDirsToDelete.Add(dir);
                log.Write($"Deleted directory: {seasonDir}");
                LogModel.Record(thisProgram, "Cleanup Plex Media", $"Deleted of {seasonDir}");
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
                    LogModel.Record(thisProgram, "Cleanup Plex Media", $"Exception on Delete of {dir}: {ex.Message}", 6);
                }

            log.Write($"Deleted directory: {dir}");
            LogModel.Record(thisProgram, "Cleanup Plex Media", $"Deleted of {dir}");
        }

        log.Stop();
        LogModel.Stop(thisProgram, "Cleanup Plex Media");
    }
}

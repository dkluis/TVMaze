using Common_Lib;
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

        MediaFileHandler mfh              = new(appInfo);
        List<string>     showDirsToDelete = new();
        var              tvShowDirs       = Directory.GetDirectories(mfh.PlexMediaTvShows);
        var              tvKimShowDirs    = Directory.GetDirectories(mfh.PlexMediaKimTvShows);
        var              tvDickShowDirs   = Directory.GetDirectories(mfh.PlexMediaDickTvShows);
        var              allTvShowDirs    = new string[tvShowDirs.Length + tvKimShowDirs.Length + tvDickShowDirs.Length];
        tvShowDirs.CopyTo(allTvShowDirs, 0);
        tvKimShowDirs.CopyTo(allTvShowDirs, tvShowDirs.Length);
        tvDickShowDirs.CopyTo(allTvShowDirs, tvShowDirs.Length + tvKimShowDirs.Length);
        foreach (var dir in allTvShowDirs)
        {
            var deleteDir  = false;
            var seasonDirs = Directory.GetDirectories(dir);
            foreach (var seasonDir in seasonDirs)
            {
                var files = Directory.GetFiles(seasonDir);
                if (files.Length == 0)
                {
                    deleteDir = true;
                    try
                    {
                        Directory.Delete(seasonDir);
                    }
                    catch (Exception ex)
                    {
                        log.Write($"Delete of {seasonDir} went wrong {ex}");
                    }

                    if (deleteDir) showDirsToDelete.Add(dir);
                    log.Write($"Deleted directory: {seasonDir}");
                } else
                {
                    deleteDir = false;
                }
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
                    log.Write($"Delete of {dir} went wrong {ex}");
                }

            log.Write($"Deleted directory: {dir}");
        }

        log.Stop();
    }
}

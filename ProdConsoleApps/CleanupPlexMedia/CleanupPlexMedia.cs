using System;
using System.Collections.Generic;
using System.IO;
using Common_Lib;
using Entities_Lib;

namespace CleanupPlexMedia
{
    internal class CleanupPlexMedia
    {
        private static void Main()
        {
            var This_Program = "Cleanup Plex Media";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            var log = appinfo.TxtFile;
            log.Start();

            MediaFileHandler mfh = new(appinfo);

            List<string> showDirsToDelete = new();
            var deleteDir = false;

            var tvshowdirs = Directory.GetDirectories(mfh.PlexMediaTvShows);
            foreach (var dir in tvshowdirs)
            {
                deleteDir = false;
                var seasondirs = Directory.GetDirectories(dir);
                foreach (var sdir in seasondirs)
                {
                    var files = Directory.GetFiles(sdir);
                    if (files.Length == 0)
                    {
                        deleteDir = true;
                        try
                        {
                            Directory.Delete(sdir);
                        }
                        catch (Exception ex)
                        {
                            log.Write($"Delete of {sdir} went wrong {ex}");
                        }

                        log.Write($"Deleted directory: {sdir}");
                    }
                    else
                    {
                        deleteDir = false;
                    }
                }

                if (deleteDir) showDirsToDelete.Add(dir);
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
}
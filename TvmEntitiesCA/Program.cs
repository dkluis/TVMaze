using Common_Lib;
using System.Collections.Generic;

namespace TvmEntities
{
    class Program
    {
        static void Main()
        {
            AppInfo appinfo = new("TVMaze", "Tvm Entities", "DbProduction");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            log.Write($"Log Level at 1", "", 1);
            log.Write($"Log Level at 2", null, 2);
            log.Write($"Log Level at 3");
            log.Write($"Log Level at 1 with another function string", "Another Function with a lot more charactes", 1);

            log.Write($"Appinfo DbConnection is {appinfo.DbConnection}");
            log.Write($"Appinfo Application is {appinfo.Application}");
            log.EmptyLine(5);

            //log.Elapsed();

            #region Write a config file (Do Not Use Anymore)
            // Do NOT use anymore
            /*
            AppInfo app1 = new("TVMaze", "Tvm Entities", "DbProduction");
            TextFileHandler config = app1.TxtFile;
            config.EmptyLine();
            config.WriteNoHead("{");
            config.WriteNoHead("\"First Record\": \"Some Info\",");
            config.WriteNoHead("\"Second Record\": \"Some Info\"");
            config.WriteNoHead("}");
            log.Write("Created the config file");
            log.Elapsed();

            AppInfo app2 = new("TVMaze", "Tvm Entities", "DbProduction");
            TextFileHandler config2 = app2.TxtFile;
            config2.EmptyLine();
            config2.WriteNoHead("{ ", false);
            config2.WriteNoHead("\"First Record\": \"Some Info\", ", false);
            config2.WriteNoHead("\"Second Record\": \"Some Info\" ", false);
            config2.WriteNoHead("}", false);
            log.Write("Created the config file");
            log.Elapsed();
            */
            #endregion


            #region Test Show Class in general

            //using (Show show = new("New-Test-DB", log))
            using (Show show = new(appinfo))
            {
                show.FillViaTvmaze(53057);

                log.Write($"Show: Exists TVM {show.showExistOnTvm}, Followed: {show.isFollowed}, Filled: {show.isFilled}: " +
                    $"Id {show.Id}, TvmShowId {show.TvmShowId}, ShowName {show.ShowName}, Cleaned {show.CleanedShowName}, Finder {show.Finder}, Alt {show.AltShowName}, " +
                    $"U Date {show.UpdateDate}, P Date {show.PremiereDate} Status for Tvm: {show.TvmStatus}, Status {show.ShowStatus}");

                log.Write($"Country {show.TvmCountry}, Image {show.TvmImage}, Imdb {show.TvmImdb}, Language {show.TvmLanguage}, Network {show.TvmNetwork}, Summary {show.TvmSummary}");

                if (show.isForReview)
                {
                    log.Write($"Insert Result is {show.DbInsert()}");
                }
                else
                {
                    log.Write($"Show is NOT rated for Review");
                }
                show.CloseDB();
            }

            #endregion


            #region Testing Searching TVMaze with Showname and returning list of show classes.

            var exectime = new System.Diagnostics.Stopwatch();
            exectime.Start();

            // SearchShowsOnTvmaze showsearch = new("New-Test-DB", log, "Lost");
            SearchShowsOnTvmaze showsearch = new(appinfo, "Lost");
            List<Show> showsFound = showsearch.Found;
            foreach (Show showFound in showsFound)
            {
                log.Write($"Show Found TvmShowid {showFound.TvmShowId}, {showFound.ShowName}");
            }
            exectime.Stop();
            log.Write($"SearchShow Exec time: {exectime.ElapsedMilliseconds} ms.", "ShowSearchOnTvmaze", 0);

            #endregion


            log.Stop();
        }
    }
}


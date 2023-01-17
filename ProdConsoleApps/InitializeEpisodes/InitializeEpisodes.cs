using System;
using System.Collections.Generic;
using Common_Lib;
using Entities_Lib;

namespace InitializeEpisodes;

internal class InitializeEpisodes
{
    private static void Main()
    {
        var thisProgram = "Init Episode Table";
        Console.WriteLine($"{DateTime.Now}:  {thisProgram}");
        AppInfo appinfo = new("TVMaze", thisProgram, "DbAlternate");
        var log = appinfo.TxtFile;
        log.Start();

        //Get All Followed Shows
        List<int> allfollowed = new();
        SearchAllFollowed sal = new();
        allfollowed = sal.Find(appinfo);
        if (allfollowed.Count == 0)
        {
            log.Write("No Followed Shows Found, exiting program", "", 0);
            Environment.Exit(99);
        }

        log.Write($"Found {allfollowed.Count} Show to get Episodes for");

        //Process All Episodes for the Followed Shows
        var idxalleps = 0;
        foreach (var showid in allfollowed)
        {
            var idxepsbyshow = 0;
            EpisodesByShow epsbyshow = new();
            var ebs = epsbyshow.Find(appinfo, showid);
            foreach (var eps in ebs)
            {
                eps.DbInsert();
                log.Write(
                    $"Inserted Episode {eps.TvmShowId}, {eps.ShowName}, {eps.TvmEpisodeId}, {eps.SeasonEpisode}",
                    "", 4);
                idxalleps++;
                idxepsbyshow++;
            }

            log.Write($"Number of Episodes for Show {showid}: {idxepsbyshow}", "", 2);
        }

        log.Write($"Number of All Episodes for All Shows: {idxalleps}");
        log.Stop();
    }
}

using System;
using Common_Lib;
using Entities_Lib;
using Web_Lib;
using DB_Lib;

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Threading;

using CodeHollow.FeedReader;
using System.Threading.Tasks;

namespace RefreshOneShow
{
    class RefreshOneShow
    {
        static void Main()
        {
            string This_Program = "Refresh One Show";
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            int TheShowToRefresh = 57079;

            MariaDB Mdbr = new(appinfo);
            MySqlConnector.MySqlDataReader rdr;

            rdr = Mdbr.ExecQuery($"select `TvmShowId`, `ShowName` from Shows where `TvmShowId` = {TheShowToRefresh} order by `TvmShowId` desc");

            while (rdr.Read())
            {
                using (ShowAndEpisodes sae = new(appinfo))
                {
                    log.Write($"Working on Show {rdr[0]} {rdr[1]}", "", 2);
                    sae.Refresh(int.Parse(rdr[0].ToString()));
                }
            }

            log.Stop();
        }
    }
}
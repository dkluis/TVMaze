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

namespace TryOut
{
    class TryingOut
    {
        static void Main()
        {
            string This_Program = "Trying Out";
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            /*
            using (MariaDB Mdb = new(appinfo))
            {
                MySqlConnector.MySqlDataReader rdr = Mdb.ExecQuery($"select `TvmShowId` from Shows where `Finder` ='ShowRss'");
                while (rdr.Read())
                {
                    using (Show show = new(appinfo))
                    {
                        //int showid = 32791;
                        int showid = int.Parse(rdr["TvmShowId"].ToString());
                        log.Write($"Working on Refreshing Show {showid}", "", 2);
                        show.FillViaTvmaze(showid);
                        show.DbUpdate();
                    }
                }
            }
            */

            /*
            WebAPI showRss = new(appinfo);
            HttpClientHandler hch = showRss.ShowRssLogin("user", "password");
            */

            log.Stop();
        }
    }
}
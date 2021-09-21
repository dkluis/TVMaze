using System;
using System.Diagnostics;

using Common_Lib;
using Web_Lib;
using Entities_Lib;
using DB_Lib;
using MySqlConnector;

namespace AcquireMedia
{
    class Program
    {
        static void Main(string[] args)
        {
            string This_Program = "Acquire Media";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appinfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appinfo.TxtFile;
            log.Start();

            WebScrape webSrape = new(appinfo);
            Magnets media = new(appinfo);

            MySqlDataReader rdr;
            using (GetEpisodesToBeAcquired gea = new())
                rdr = gea.Find(appinfo);

            string magnet;
            Tuple<bool, string> result;
            bool isSeason = false;
            int showid = 0;
            int season = 0;
            string showname = "";

            while (rdr.Read())
            {
                if (isSeason && showid == int.Parse(rdr["TvmShowId"].ToString())) { continue; } else { isSeason = false; showid = 0; }
                if (rdr["AltShowName"].ToString() != "") { showname = rdr["AltShowName"].ToString().Replace("(", "").Replace(")", "");  } else { showname = rdr["ShowName"].ToString(); }
                result = media.PerformShowEpisodeMagnetsSearch(showname, int.Parse(rdr["Season"].ToString()), int.Parse(rdr["Episode"].ToString()), log);
                int episodeid = int.Parse(rdr["TvmEpisodeId"].ToString());
                magnet = result.Item2;
                isSeason = result.Item1;

                if (magnet != "")
                {
                    string[] temp = magnet.Split("tr=");
                    //temp = temp[1].Split("dn=");
                    log.Write($"Found Magnet for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]} Processin Whole Season is {isSeason}: {temp[0]}");
                }
                else
                {
                    log.Write($"Magnet Not Found for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]}");
                    continue;
                }

                using (Process AcquireMediaScript = new())
                {
                    AcquireMediaScript.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                    AcquireMediaScript.StartInfo.Arguments = magnet;
                    AcquireMediaScript.StartInfo.UseShellExecute = true;
                    AcquireMediaScript.StartInfo.RedirectStandardOutput = false;
                    bool started = AcquireMediaScript.Start();
                    AcquireMediaScript.WaitForExit();
                }

                if (!isSeason)
                {
                    using (Episode episode = new(appinfo))
                    {
                        episode.FillViaTvmaze(episodeid);
                        episode.PlexStatus = "Acquired";
                        episode.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                        episode.DbUpdate();
                    }
                    using (WebAPI wai = new(appinfo)) { wai.PutEpisodeToAcquired(episodeid); }
                }
                else
                {
                    showid = int.Parse(rdr["TvmShowId"].ToString());
                    season = int.Parse(rdr["Season"].ToString());
                    using (MariaDB Mdb = new(appinfo))
                    {
                        MySqlDataReader seasrdr = Mdb.ExecQuery($"select * from Episodes where `TvmShowId` = {showid} and `Season` = {season}");
                        while (seasrdr.Read())
                        {
                            int seasepiid = int.Parse(seasrdr["TvmEpisodeId"].ToString());
                            using (Episode seasepi = new(appinfo))
                            {
                                seasepi.FillViaTvmaze(seasepiid);
                                seasepi.PlexStatus = "Acquired";
                                seasepi.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                                seasepi.DbUpdate();
                            }
                            using (WebAPI wai = new(appinfo)) { wai.PutEpisodeToAcquired(seasepiid); }
                        }
                    }
                }
            }

            log.Stop();
        }
    }
}
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
        private static void Main()
        {
            string This_Program = "Acquire Media";
            Console.WriteLine($"{DateTime.Now}: {This_Program}");
            AppInfo appInfo = new("TVMaze", This_Program, "DbAlternate");
            TextFileHandler log = appInfo.TxtFile;
            log.Start();

            Magnets media = new(appInfo);
            MySqlDataReader rdr;
            using (GetEpisodesToBeAcquired gea = new())
                rdr = gea.Find(appInfo);

            string magnet;
            Tuple<bool, string> result;
            bool isSeason = false;
            int showId = 0;

            while (rdr.Read())
            {
                if (isSeason && showId == int.Parse(rdr["TvmShowId"].ToString()!))
                {
                    continue;
                }
                else
                {
                    isSeason = false;
                    showId = 0;
                }

                String showName = "";
                if (rdr["AltShowName"].ToString() != "")
                {
                    showName = rdr["AltShowName"].ToString()!.Replace("(", "").Replace(")", "");
                }
                else
                {
                    showName = rdr["ShowName"].ToString();
                }

                result = media.PerformShowEpisodeMagnetsSearch(showName, int.Parse(rdr["Season"].ToString()!),
                    int.Parse(rdr["Episode"].ToString()!), log);
                int episodeId = int.Parse(rdr["TvmEpisodeId"].ToString()!);
                magnet = result.Item2;
                isSeason = result.Item1;

                if (magnet != "")
                {
                    string[] temp = magnet.Split("tr=");
                    log.Write(
                        $"Found Magnet for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]} Processing Whole Season is {isSeason}: {temp[0]}");
                }
                else
                {
                    log.Write($"Magnet Not Found for {rdr["ShowName"]}, {rdr["Season"]}-{rdr["Episode"]}");
                    continue;
                }

                using (Process acquireMediaScript = new())
                {
                    acquireMediaScript.StartInfo.FileName = "/Users/dick/TVMaze/Scripts/AcquireMediaViaTransmission.sh";
                    acquireMediaScript.StartInfo.Arguments = magnet;
                    acquireMediaScript.StartInfo.UseShellExecute = true;
                    acquireMediaScript.StartInfo.RedirectStandardOutput = false;
                    acquireMediaScript.Start();
                    acquireMediaScript.WaitForExit();
                }

                if (!isSeason)
                {
                    using (Episode episode = new(appInfo))
                    {
                        episode.FillViaTvmaze(episodeId);
                        episode.PlexStatus = "Acquired";
                        episode.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                        episode.DbUpdate();
                    }

                    using (WebAPI wai = new(appInfo))
                    {
                        wai.PutEpisodeToAcquired(episodeId);
                    }
                }
                else
                {
                    showId = int.Parse(rdr["TvmShowId"].ToString()!);
                    int season = int.Parse(rdr["Season"].ToString()!);
                    using (MariaDB mdb = new(appInfo))
                    {
                        MySqlDataReader seasRdr =
                            mdb.ExecQuery(
                                $"select * from Episodes where `TvmShowId` = {showId} and `Season` = {season}");
                        while (seasRdr.Read())
                        {
                            int seasEpiId = int.Parse(seasRdr["TvmEpisodeId"].ToString()!);
                            using (Episode seasEpi = new(appInfo))
                            {
                                seasEpi.FillViaTvmaze(seasEpiId);
                                seasEpi.PlexStatus = "Acquired";
                                seasEpi.PlexDate = DateTime.Now.ToString("yyyy-MM-dd");
                                seasEpi.DbUpdate();
                            }

                            using (WebAPI wai = new(appInfo))
                            {
                                wai.PutEpisodeToAcquired(seasEpiId);
                            }
                        }
                    }
                }
            }

            log.Stop();
        }
    }
}
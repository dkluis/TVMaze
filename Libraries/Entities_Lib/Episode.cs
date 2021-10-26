using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using Web_Lib;

namespace Entities_Lib
{
    public class Episode : IDisposable
    {
        private readonly AppInfo Appinfo;
        private readonly TextFileHandler log;

        private readonly MariaDB Mdb;
        public bool isAutoDelete;
        public bool isDBFilled;

        public bool isFilled;
        public bool isJsonFilled;
        public bool isOnTvmaze;

        public Episode(AppInfo appinfo)
        {
            Appinfo = appinfo;
            Mdb = new MariaDB(appinfo);
            log = appinfo.TxtFile;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            Id = 0;
            TvmShowId = 0;
            TvmEpisodeId = 0;
            TvmUrl = "";
            ShowName = "";
            SeasonEpisode = "";
            SeasonNum = 0;
            EpisodeNum = 0;
            BroadcastDate = null;
            PlexStatus = " ";
            PlexDate = null;
            UpdateDate = "1970-01-01";

            MediaType = "";
            CleanedShowName = "";
            AltShowName = "";

            TvmType = "";
            TvmSummary = "";
            TvmImage = "";
            TvmRunTime = 0;

            isFilled = false;
            isJsonFilled = false;
            isDBFilled = false;
            isOnTvmaze = false;
            isAutoDelete = false;
        }

        public void FillViaTvmaze(int episodeid)
        {
            using (WebAPI je = new(Appinfo))
            {
                FillViaJson(je.ConvertHttpToJObject(je.GetEpisode(episodeid)));
                FillViaDb(episodeid);
                WebAPI fem = new(Appinfo);
                FillEpiMarks(fem.ConvertHttpToJObject(fem.GetEpisodeMarks(episodeid)));
            }
        }

        private void FillViaJson(JObject episode)
        {
            Id = 0;

            PlexStatus = " ";
            PlexDate = null;

            TvmShowId = int.Parse(episode["_embedded"]["show"]["id"].ToString());
            TvmEpisodeId = int.Parse(episode["id"].ToString());
            ShowName = episode["_embedded"]["show"]["name"].ToString();
            TvmUrl = episode["url"].ToString();
            SeasonNum = int.Parse(episode["season"].ToString());
            EpisodeNum = int.Parse(episode["number"].ToString());
            SeasonEpisode = Common.BuildSeasonEpisodeString(SeasonNum, EpisodeNum);
            if (episode["airdate"] is not null) BroadcastDate = episode["airdate"].ToString();
            TvmType = episode["type"].ToString();
            if (episode["summary"] is not null) TvmSummary = episode["summary"].ToString();
            if (episode["image"] is not null && episode["image"].ToString() != "")
                if (episode["image"]["medium"] is not null && episode["image"]["medium"].ToString() != "")
                    TvmImage = episode["image"]["medium"].ToString();
            if (episode["runtime"] is not null && episode["runtime"].ToString() != "")
                TvmRunTime = int.Parse(episode["runtime"].ToString());

            isJsonFilled = true;
        }

        private void FillEpiMarks(JObject epm)
        {
            if (epm is not null && epm.ToString() != "{}")
            {
                /*
                 * 0 = watched, 1 = acquired, 2 = skipped 
                */
                switch (epm["type"].ToString())
                {
                    case "0":
                        PlexStatus = "Watched";
                        break;
                    case "1":
                        PlexStatus = "Acquired";
                        break;
                    case "2":
                        PlexStatus = "Skipped";
                        break;
                    default:
                        PlexStatus = null;
                        break;
                }

                if (epm["marked_at"] is not null)
                    PlexDate = Common.ConvertEpochToDate(int.Parse(epm["marked_at"].ToString()));
            }
        }

        private void FillViaDb(int episode)
        {
            var rdr = Mdb.ExecQuery($"select * from episodesfullinfo where `TvmEpisodeId` = {episode};");
            while (rdr.Read())
            {
                Id = int.Parse(rdr["Id"].ToString());
                MediaType = rdr["MediaType"].ToString();
                CleanedShowName = rdr["CleanedShowname"].ToString();
                AltShowName = rdr["AltShowName"].ToString();
                UpdateDate = rdr["UpdateDate"].ToString();
                if (rdr["AutoDelete"].ToString() == "Yes") isAutoDelete = true;
                isDBFilled = true;
            }

            Mdb.Close();
        }

        public bool DbInsert()
        {
            Mdb.success = true;

            var values = "";
            var sqlpre = "insert into episodes values (";
            var sqlsuf = ");";

            values += $"{0}, ";
            values += $"{TvmShowId}, ";
            values += $"{TvmEpisodeId}, ";
            values += $"'{TvmUrl}', ";
            values += $"'{SeasonEpisode}', ";
            values += $"{SeasonNum}, ";
            values += $"{EpisodeNum}, ";
            if (BroadcastDate == "") BroadcastDate = null;
            if (BroadcastDate is null)
                values += "null, ";
            else
                values += $"'{BroadcastDate}', ";
            values += $"'{PlexStatus}', ";
            if (PlexDate is null)
                values += "null, ";
            else
                values += $"'{PlexDate}', ";
            values += $"'{DateTime.Now.ToString("yyyy-MM-dd")}' ";
            var rows = Mdb.ExecNonQuery(sqlpre + values + sqlsuf);
            log.Write($"DbInsert for Episode: {TvmEpisodeId}", "", 4);
            Mdb.Close();
            if (rows == 0) Mdb.success = false;
            ;
            return Mdb.success;
        }

        public bool DbUpdate()
        {
            Mdb.success = true;

            var values = "";
            var sqlpre = "update episodes set ";
            var sqlsuf = $"where `Id` = {Id};";

            if (BroadcastDate == "") BroadcastDate = null;
            if (BroadcastDate is null)
                values += "`BroadcastDate` = null, ";
            else
                values += $"`BroadcastDate` = '{BroadcastDate}', ";
            values += $"`PlexStatus` = '{PlexStatus}', ";
            values += $"`Season` = {SeasonNum}, ";
            values += $"`Episode` = {EpisodeNum}, ";
            values += $"`SeasonEpisode` = '{SeasonEpisode}', ";
            if (PlexDate is null)
                values += "`PlexDate` = null, ";
            else
                values += $"`PlexDate` = '{PlexDate}', ";
            values += $"`UpdateDate` = '{DateTime.Now.ToString("yyyy-MM-dd")}' ";

            var rows = Mdb.ExecNonQuery(sqlpre + values + sqlsuf);
            log.Write($"DbUpdate for Episode: {TvmEpisodeId}", "", 4);
            Mdb.Close();
            if (rows == 0) Mdb.success = false;
            return Mdb.success;
        }

        #region DB Record Definition

        public int Id;
        public int TvmShowId;
        public int TvmEpisodeId;
        public string TvmUrl = "";
        public string ShowName = "";
        public string SeasonEpisode = "";
        public int SeasonNum;
        public int EpisodeNum;
        public string BroadcastDate;
        public string PlexStatus = " ";
        public string PlexDate;
        public string UpdateDate = "1970-01-01";


        public string MediaType = "";
        public string CleanedShowName = "";
        public string AltShowName = "";

        #endregion

        #region Tvm Record Definiton (without what is in DB record)

        public string TvmType;
        public int TvmRunTime;
        public string TvmImage;
        public string TvmSummary;

        #endregion
    }

    public class EpisodesByShow : IDisposable
    {
        public List<Episode> episodesbyshow = new();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public List<Episode> Find(AppInfo appinfo, int showid)
        {
            JArray epsbyshow = new();
            using (WebAPI wa = new(appinfo))
            {
                epsbyshow = wa.ConvertHttpToJArray(wa.GetEpisodesByShow(showid));
                if (epsbyshow is null) return episodesbyshow;
            }


            foreach (var ep in epsbyshow)
                using (Episode episode = new(appinfo))
                {
                    if (ep is null) continue;
                    appinfo.TxtFile.Write($"Working on Episode {ep["id"]}", "", 4);
                    episode.FillViaTvmaze(int.Parse(ep["id"].ToString()));
                    episodesbyshow.Add(episode);
                }

            return episodesbyshow;
        }
    }

    public class EpisodeSearch : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int Find(AppInfo appinfo, int showid, string seasonepisode)
        {
            var epiid = 0;
            MariaDB Mdb = new(appinfo);

            var rdr = Mdb.ExecQuery(
                $"select `TvmEpisodeId` from Episodes where `TvmShowId` = {showid} and `SeasonEpisode` = '{seasonepisode}'; ");
            while (rdr.Read()) epiid = int.Parse(rdr[0].ToString());

            return epiid;
        }
    }

    public class GetEpisodesToBeAcquired : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public MySqlDataReader Find(AppInfo appinfo)
        {
            MariaDB Mdb = new(appinfo);
            var rdr = Mdb.ExecQuery("select * from episodestoacquire order by `TvmShowId`, `Season`, `Episode`");
            return rdr;
        }
    }
}
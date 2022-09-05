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
        private readonly AppInfo _appInfo;
        private readonly TextFileHandler _log;

        private readonly MariaDb _mdb;
        public bool IsAutoDelete;
        public bool IsDbFilled;

        public bool IsFilled;
        public bool IsJsonFilled;
        public bool IsOnTvmaze;

        public Episode(AppInfo appInfo)
        {
            _appInfo = appInfo;
            _mdb = new MariaDb(appInfo);
            _log = appInfo.TxtFile;
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

            IsFilled = false;
            IsJsonFilled = false;
            IsDbFilled = false;
            IsOnTvmaze = false;
            IsAutoDelete = false;
        }

        public void FillViaTvmaze(int episodeId)
        {
            using WebApi je = new(_appInfo);
            FillViaJson(je.ConvertHttpToJObject(je.GetEpisode(episodeId)));
            FillViaDb(episodeId);
            using WebApi fem = new(_appInfo);
            FillEpiMarks(fem.ConvertHttpToJObject(fem.GetEpisodeMarks(episodeId)));
        }

        private void FillViaJson(JObject episode)
        {
            Id = 0;
            PlexStatus = " ";
            PlexDate = null;
            if (episode is null)
            {
                IsJsonFilled = false;
                return;
            }
            
            if (episode["_embedded"]?["show"] != null)
            {
                if (episode["_embedded"]["show"]["id"] is not null)
                    TvmShowId = int.Parse(episode["_embedded"]["show"]["id"].ToString());
                if (episode["_embedded"]["show"]["name"] is not null)
                    ShowName = episode["_embedded"]["show"]["name"].ToString();
            }

            TvmEpisodeId = int.Parse(episode["id"].ToString());
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

            IsJsonFilled = true;
        }

        private void FillEpiMarks(JObject epm)
        {
            if (epm is not null && epm.ToString() != "{}")
            {
                /*
                    0 = watched, 1 = acquired, 2 = skipped 
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

        public void FillViaDb(int episode)
        {
            var rdr = _mdb.ExecQuery($"select * from episodesfullinfo where `TvmEpisodeId` = {episode};");
            while (rdr.Read())
            {
                Id = int.Parse(rdr["Id"].ToString()!);
                MediaType = rdr["MediaType"].ToString();
                CleanedShowName = rdr["CleanedShowname"].ToString();
                AltShowName = rdr["AltShowName"].ToString();
                UpdateDate = rdr["UpdateDate"].ToString();
                if (rdr["AutoDelete"].ToString() == "Yes") IsAutoDelete = true;
                IsDbFilled = true;
            }

            _mdb.Close();
        }

        public bool DbInsert()
        {
            _mdb.Success = true;

            var values = "";
            const string ins = "insert";
            var sqlPre = $"{ins} into episodes values (";
            const string sqlSuf = ");";

            values += "0, ";
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
            values += $"'{DateTime.Now:yyyy-MM-dd}' ";
            var rows = _mdb.ExecNonQuery(sqlPre + values + sqlSuf);
            _log.Write($"DbInsert for Episode: {TvmEpisodeId}", "", 4);
            _mdb.Close();
            if (rows == 0) _mdb.Success = false;
            return _mdb.Success;
        }

        public bool DbUpdate()
        {
            _mdb.Success = true;
            var values = "";
            var sqlPre = "update episodes set ";
            var sqlSuf = $"where `Id` = {Id};";

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
            values += $"`UpdateDate` = '{DateTime.Now:yyyy-MM-dd}' ";

            var rows = _mdb.ExecNonQuery(sqlPre + values + sqlSuf);
            _log.Write($"DbUpdate for Episode: {TvmEpisodeId}", "", 4);
            _mdb.Close();
            if (rows == 0) _mdb.Success = false;
            return _mdb.Success;
        }

        public bool DbDelete()
        {
            _mdb.Success = true;
            var rows = _mdb.ExecNonQuery($"delete from Episodes where `TvmEpisodeId` = {TvmEpisodeId}");
            _log.Write($"DbDelete for Episode: {TvmEpisodeId}", "", 4);
            _mdb.Close();
            if (rows != 0) return _mdb.Success;
            return _mdb.Success = false;
        }

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

        public string TvmType;
        public int TvmRunTime;
        public string TvmImage;
        public string TvmSummary;
    }

    public class EpisodesByShow : IDisposable
    {
        private readonly List<Episode> _episodesByShowList = new();

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public List<Episode> Find(AppInfo appInfo, int showId)
        {
            JArray epsByShow;
            using (WebApi wa = new(appInfo))
            {
                epsByShow = wa.ConvertHttpToJArray(wa.GetEpisodesByShow(showId));
                if (epsByShow is null) return _episodesByShowList;
            }


            foreach (var ep in epsByShow)
            {
                using Episode episode = new(appInfo);
                if (ep is null) continue;
                appInfo.TxtFile.Write($"Working on Episode {ep["id"]}", "", 4);
                episode.FillViaTvmaze(int.Parse(ep["id"]!.ToString()));
                _episodesByShowList.Add(episode);
            }

            return _episodesByShowList;
        }
    }

    public class EpisodeSearch : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public int Find(AppInfo appInfo, int showId, string seasonEpisode)
        {
            var epiId = 0;
            MariaDb mdb = new(appInfo);

            var rdr = mdb.ExecQuery(
                $"select `TvmEpisodeId` from Episodes where `TvmShowId` = {showId} and `SeasonEpisode` = '{seasonEpisode}'; ");
            while (rdr.Read()) epiId = int.Parse(rdr[0].ToString()!);

            return epiId;
        }
    }

    public class GetEpisodesToBeAcquired : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public MySqlDataReader Find(AppInfo appInfo)
        {
            MariaDb mdb = new(appInfo);
            var rdr = mdb.ExecQuery("select * from episodestoacquire order by `TvmShowId`, `Season`, `Episode`");
            return rdr;
        }
    }
}
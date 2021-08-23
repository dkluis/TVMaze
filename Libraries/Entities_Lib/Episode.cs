using Common_Lib;
using DB_Lib;
using Web_Lib;

using MySqlConnector;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace Entities_Lib
{
    public class Episode : IDisposable
    {
        #region DB Record Definition

        public Int32  Id = 0;
        public Int32  TvmShowId = 0;
        public Int32  TvmEpisodeId = 0;
        public string TvmUrl = "";
        public string ShowName = "";
        public string SeasonEpisode = "";
        public int    SeasonNum = 0;
        public int    EpisodeNum = 0;
        public string BroadcastDate;
        public string PlexStatus = " ";
        public string PlexDate;

        #endregion

        #region Tvm Record Definiton (without what is in DB record)

        public string TvmType;
        public int    TvmRunTime;
        public string TvmImage;
        public string TvmSummary;

        #endregion

        public bool isFilled;
        public bool isDBFilled;
        public bool isJsonFilled;
        public bool isOnTvmaze;

        private readonly MariaDB Mdb;
        private readonly TextFileHandler log;
        private readonly AppInfo Appinfo;

        public Episode(AppInfo appinfo)
        {
            Appinfo = appinfo;
            Mdb = new(appinfo);
            log = appinfo.TxtFile;
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

            TvmType = "";
            TvmSummary = "";
            TvmImage = "";
            TvmRunTime = 0;

            isFilled = false;
            isJsonFilled = false;
            isDBFilled = false;
            isOnTvmaze = false;
        }

        public void FillViaTvmaze(int episodeid)
        {
            using (WebAPI je = new(Appinfo))
            {
                FillViaJson(je.ConvertHttpToJObject(je.GetEpisode(episodeid)));
                FillEpiMarks(je.ConvertHttpToJObject(je.GetEpisodeMarks(episodeid)));
                FillViaDb(episodeid);
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
            if (episode["airdate"] is not null) { BroadcastDate = episode["airdate"].ToString(); }
       

            TvmType = episode["type"].ToString();
            TvmSummary = episode["summary"].ToString();
            if (episode["image"] is not null) { TvmImage = episode["image"]["medium"].ToString(); }
            TvmRunTime = int.Parse(episode["runtime"].ToString());

            isJsonFilled = true;
        }

        private void FillEpiMarks(JObject epm)
        {
            if (epm is not null)
            {

            }
        }

        private void FillViaDb(int episode)
        {
            MySqlDataReader rdr = Mdb.ExecQuery("select * from Episodes where `TvmEpisodeId` = episode;");
            while (rdr.Read())
            {
                Id = int.Parse(rdr["Id"].ToString());
                isDBFilled = true;
            }
        }

        public bool DbInsert()
        {
            Mdb.success = true;

            string values = "";
            string sqlpre = $"insert into episodes values (";
            string sqlsuf = $");";

            // values += $"'{}', ";  for strings
            // values += $"{}, ";    for ints
            // values += $".... );"' for last value
            values += $"{0}, ";
            values += $"{TvmShowId}, ";
            values += $"{TvmEpisodeId}, ";
            values += $"'{TvmUrl}', ";
            values += $"'{SeasonEpisode}', ";
            values += $"{SeasonNum}, ";
            values += $"{EpisodeNum}, ";
            if (BroadcastDate is null) { values += $"null "; } else { values += $"'{BroadcastDate}', "; }
            values += $"'{PlexStatus}', ";
            if (PlexDate is null) { values += $"null "; } else { values += $"'{PlexDate}' "; }
            //values += $"'{DateTime.Now:yyyy-MM-dd}' ";
            int rows = Mdb.ExecNonQuery(sqlpre + values + sqlsuf);
            log.Write($"DbInsert for Episode: {TvmEpisodeId}", "", 4);
            Mdb.Close();
            if (rows == 0) { Mdb.success = false; };
            return Mdb.success;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

    }
}

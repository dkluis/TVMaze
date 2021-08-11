using Common_Lib;
using DB_Lib;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System;
using Web_Lib;

namespace TvmEntities
{
    public class Show : IDisposable
    {
        #region Record Definition

        public Int32 Id = 0;
        public Int32 TvmShowId = 0;
        public string TvmStatus = " ";
        public string TvmUrl = "";
        public string ShowName = "";
        public string ShowStatus = " ";
        public string PremiereDate = "1970-01-01";
        public string Finder = " ";
        public string CleanedShowName = "";
        public string AltShowName = "";
        public string UpdateDate = "1900-01-01";

        #endregion

        public bool isFilled;
        public bool showExistOnTvm;
        public bool isFollowed;
        private readonly MariaDB Mdb;
        private readonly Logger log;
        private readonly string connection;

        public Show(string conninfo, Logger logger)
        {
            Mdb = new(conninfo, logger);
            log = logger;
            connection = conninfo;
        }

        public void Reset()
        {
            Id = 0;
            TvmShowId = 0;
            TvmStatus = " ";
            TvmUrl = "";
            ShowName = "";
            ShowStatus = " ";
            PremiereDate = "1970-01-01";
            Finder = " ";
            CleanedShowName = "";
            AltShowName = "";
            UpdateDate = "1900-01-01";
            isFilled = false;
            showExistOnTvm = false;
            isFollowed = false;
        }

        public void FillViaJson(JObject showjson)
        {
            if (showjson["id"] is not null)
            {
                showExistOnTvm = true;
                TvmShowId = Int32.Parse(showjson["id"].ToString());
                using (TvmCommonSql tcs = new(connection, log))
                    Id = tcs.GetIdViaShowid(TvmShowId);
                if (Id != 0)
                {
                    isFollowed = true;
                }
                TvmUrl = showjson["url"].ToString();
                ShowName = showjson["name"].ToString();
                ShowStatus = showjson["status"].ToString();
                PremiereDate = Convert.ToDateTime(showjson["premiered"]).ToString("yyyy-MM-dd");
                // Finder
                // TvmStatus
                CleanedShowName = Common.RemoveSpecialCharsInShowname(ShowName);
                // AltShowName
                // UpdateDate
                isFilled = true;
            }
        }

        public void FillViaDB(Int32 showid, bool JsonIsDone)
        {
            if (!isFollowed && JsonIsDone) { return; }
            
            using (MySqlDataReader rdr = Mdb.ExecQuery($"select * from shows where `TvmShowId` = {showid};"))
            {
                while (rdr.Read())
                {
                    isFollowed = true;
                    if (JsonIsDone)
                    {
                        Finder = rdr["Finder"].ToString();
                        AltShowName = rdr["AltShowName"].ToString();
                        UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                        TvmStatus = rdr["TvmStatus"].ToString();
                        continue;
                    }
                    Id = Int32.Parse(rdr["Id"].ToString());
                    TvmShowId = Int32.Parse(rdr["TvmShowId"].ToString());
                    TvmUrl = rdr["TvmUrl"].ToString();
                    ShowName = rdr["ShowName"].ToString();
                    ShowStatus = rdr["ShowStatus"].ToString();
                    PremiereDate = Convert.ToDateTime(rdr["PremiereDate"]).ToString("yyyy-MM-dd");
                    CleanedShowName = rdr["CleanedShowName"].ToString();
                    isFilled = true;
                }
            }
        }

        public void FillViaDB(MySqlDataReader showrdr)
        {

        }

        public void FillViaTvmaze(Int32 showid)
        {
            using WebAPI js = new(log);
            FillViaJson(js.ConvertHttpToJObject(js.GetShow(showid)));
            if (isFollowed) { FillViaDB(showid, true); }
        }

        public bool DbUpdate()
        {
            return false;
        }

        public bool DbInsert()
        {
            return false;
        }

        public bool TvmUpdateFollowed(bool followed)
        {
            return false;
        }

        public void Dispose()
        {
            Mdb.Close();
            GC.SuppressFinalize(this);
        }
    }
}

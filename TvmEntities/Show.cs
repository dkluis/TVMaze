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
        #region DB Record Definition

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

        #region Tvm Record Definiton (without what is in DB record)

        public string TvmType;
        public string TvmLanguage;
        public string TvmOfficialSite;
        public string TvmNetwork;
        public string TvmCountry;
        public string TvmImdb;
        public string TvmImage;
        public string TvmSummary;

        #endregion

        public bool isFilled;
        public bool showExistOnTvm;
        public bool isFollowed;

        private readonly string connection;
        private readonly MariaDB Mdb;
        private readonly Logger log;

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

            TvmType = "";
            TvmLanguage = "";
            TvmOfficialSite = "";
            TvmNetwork = "";
            TvmCountry = "";
            TvmImdb = "";
            TvmImage = "";
            TvmSummary = "";

            isFilled = false;
            showExistOnTvm = false;
            isFollowed = false;
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

        private void FillViaJson(JObject showjson)
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
                if (showjson["premiered"] is not null) { PremiereDate = Convert.ToDateTime(showjson["premiered"]).ToString("yyyy-MM-dd"); }
                // Finder
                // TvmStatus
                CleanedShowName = Common.RemoveSpecialCharsInShowname(ShowName);
                // AltShowName
                // UpdateDate

                if (showjson["type"] is not null) { TvmType = showjson["type"].ToString(); }
                if (showjson["language"] is not null) { TvmLanguage = showjson["language"].ToString(); }
                if (showjson["officialSite"] is not null) { TvmOfficialSite = showjson["officialSite"].ToString(); }
                if (showjson["network"]["name"] is not null) { TvmNetwork = showjson["network"]["name"].ToString(); }
                if (showjson["network"]["country"]["name"] is not null) { TvmCountry = showjson["network"]["country"]["name"].ToString(); }
                if (showjson["externals"]["imdb"] is not null) { TvmImdb = showjson["externals"]["imdb"].ToString(); }
                if (showjson["image"]["medium"] is not null) { TvmImage = showjson["image"]["medium"].ToString(); }
                if (showjson["summary"] is not null) { TvmSummary = showjson["summary"].ToString(); }
                isFilled = true;
            }
        }

        private void FillViaDB(Int32 showid, bool JsonIsDone)
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

        public void Dispose()
        {
            Mdb.Close();
            GC.SuppressFinalize(this);
        }
    }
}

using Common_Lib;
using DB_Lib;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
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
        public string ShowStatus = "";
        public string PremiereDate = "";
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
        public bool isForReview;

        private readonly MariaDB Mdb;
        private readonly TextFileHandler log;
        private readonly AppInfo Appinfo;

        public Show(AppInfo appinfo)
        {
            Appinfo = appinfo;
            Mdb = new(appinfo);
            log = appinfo.TxtFile;
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
            if (!isFollowed) { ValidateForReview(); }
        }

        public bool DbUpdate()
        {
            if (!isFollowed || !isFilled) { return false; }  // Show does not exist in the DB 

            // updfields += $"`` = '{}', ";
            string updfields = "";
            string sqlpre = $"update shows set ";
            updfields += $"`Finder` = '{Finder}', ";
            updfields += $"`ShowName` = '{ShowName}', ";
            updfields += $"`AltShowName` = '{AltShowName}', ";
            updfields += $"`CleanedShowName` = '{CleanedShowName}', ";
            updfields += $"`UpdateDate` = '{DateTime.Now.Date:yyyy-MM-dd}' ";
            string sqlsuf = $"where `TvmShowId` = {TvmShowId};";
            Mdb.ExecNonQuery(sqlpre + updfields + sqlsuf);
            return true;
        }

        public bool DbInsert()
        {
            if (isFollowed || !isFilled) { return false; }
            // values += $"'{}', ";  for strings
            // values += $"{}, ";    for ints
            string values = "";
            string sqlpre = $"insert into shows values (";
            string sqlsuf = $");";
            values += $"{0}, ";
            values += $"{TvmShowId}, ";
            values += $"'New', ";
            values += $"'{TvmUrl}', ";
            values += $"'{ShowName}', ";
            values += $"'{ShowStatus}', ";
            values += $"'{PremiereDate}', ";
            values += $"'{Finder}', ";
            values += $"'{CleanedShowName}', ";
            values += $"'{AltShowName}', ";
            values += $"'{DateTime.Now:yyyy-MM-dd}' ";
            Mdb.ExecNonQuery(sqlpre + values + sqlsuf);
            return true;
        }

        private void FillViaJson(JObject showjson)
        {
            if (showjson["id"] is not null)
            {
                showExistOnTvm = true;
                TvmShowId = Int32.Parse(showjson["id"].ToString());
                using (TvmCommonSql tcs = new(Appinfo))
                    Id = tcs.GetIdViaShowid(TvmShowId);
                if (Id != 0)
                {
                    isFollowed = true;
                }
                TvmUrl = showjson["url"].ToString();
                ShowName = showjson["name"].ToString();
                ShowStatus = showjson["status"].ToString();
                if (showjson["premiered"] is not null)
                {
                    PremiereDate = "1900-01-01";
                    string date = showjson["premiered"].ToString();
                    if (date != "")
                    {
                        PremiereDate = Convert.ToDateTime(showjson["premiered"]).ToString("yyyy-MM-dd");
                    }
                }
                // Finder
                // TvmStatus
                CleanedShowName = Common.RemoveSpecialCharsInShowname(ShowName);
                // AltShowName
                // UpdateDate

                if (showjson["type"] is not null) { TvmType = showjson["type"].ToString(); }
                if (showjson["language"] is not null) { TvmLanguage = showjson["language"].ToString(); }
                if (showjson["officialSite"] is not null) { TvmOfficialSite = showjson["officialSite"].ToString(); }
                if (showjson["network"].ToString() != "")
                {
                    if (showjson["network"]["name"] is not null) { TvmNetwork = showjson["network"]["name"].ToString(); }
                    if (showjson["network"]["country"]["name"] is not null) { TvmCountry = showjson["network"]["country"]["name"].ToString(); }
                }

                if (showjson["externals"]["imdb"] is not null) { TvmImdb = showjson["externals"]["imdb"].ToString(); }
                if (showjson["image"].ToString() != "")
                {
                    if (showjson["image"]["medium"] is not null) { TvmImage = showjson["image"]["medium"].ToString(); }
                }

                if (showjson["summary"] is not null)
                {
                    if (showjson["image"].ToString() != "")
                    { TvmSummary = showjson["summary"].ToString(); }
                    else
                    { TvmSummary = "##Unknow##"; }
                }
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

        private bool ValidateForReview()
        {
            if (!isFilled) { isForReview = false; return false; }

            isForReview = false;

            if (TvmLanguage != "English" && TvmNetwork != "NetFlix") { return false; } else { if (TvmLanguage != "English") { return false; } }
            if (ShowStatus == "Ended") { return false; } // && PremiereDate < Convert)
            switch (TvmType)
            {
                case "Sport":
                case "News":
                case "Variety":
                case "Game Show":
                case "":
                    return false;
            }
            switch (TvmNetwork)
            {
                case "YouTube":
                case "YouTube Premium":
                case "Facebook Watch":
                case "":
                    return false;
            }

            isForReview = true;
            return isForReview;
        }

        public void CloseDB()
        {
            Mdb.Close();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class SearchShowsOnTvmaze
    {
        public List<Show> Found = new();

        public SearchShowsOnTvmaze(AppInfo appinfo, string showname)
        {
            int idx = 0;
            var exectime = new System.Diagnostics.Stopwatch();
            TextFileHandler log = appinfo.TxtFile;

            while (idx < 20)
            {
                exectime.Restart();
                idx++;
                using (Show show = new(appinfo))
                {
                    show.FillViaTvmaze(idx);
                    // if (show.Id != 0) { Found.Add(show); } else { logger.Write($"ShowId {idx} not found or timed out"); }
                    Found.Add(show);
                }
                Thread.Sleep(1500);
                exectime.Stop();
                log.Write($"SearchShow Exec time: {exectime.ElapsedMilliseconds} ms.");
            }

        }

    }

}

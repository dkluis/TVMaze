using Common_Lib;
using DB_Lib;
using Web_Lib;

using MySqlConnector;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;


namespace Entities_Lib
{
    public class Show : IDisposable
    {
        #region DB Record Definition

        public int Id = 0;
        public int TvmShowId = 0;
        public string TvmStatus = " ";
        public string TvmUrl = "";
        public string ShowName = "";
        public string ShowStatus = "";
        public string PremiereDate = "";
        public string Finder = "Multi";
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
        public bool isDBFilled;
        public bool isJsonFilled;
        public bool isOnTvmaze;
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
            Finder = "Multi";
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
            isJsonFilled = false;
            isDBFilled = false;
            isOnTvmaze = false;
            isFollowed = false;
        }

        public void FillViaTvmaze(int showid)
        {
            using WebAPI js = new(Appinfo);
            FillViaJson(js.ConvertHttpToJObject(js.GetShow(showid)));
            FillViaDB(showid, true);
            if (!isFollowed && !isDBFilled) { ValidateForReview(); }
        }

        public bool DbUpdate()
        {
            // updfields += $"`` = '{}', ";
            Mdb.success = true;
            string updfields = "";
            string sqlpre = $"update shows set ";
            updfields += $"`Finder` = '{Finder}', ";
            updfields += $"`ShowName` = '{ShowName.Replace("'", "''")}', ";
            updfields += $"`AltShowName` = '{AltShowName.Replace("'", "''")}', ";
            updfields += $"`CleanedShowName` = '{CleanedShowName.Replace("'", "''")}', ";
            updfields += $"`UpdateDate` = '{DateTime.Now.Date:yyyy-MM-dd}' ";
            string sqlsuf = $"where `TvmShowId` = {TvmShowId};";
            int rows = Mdb.ExecNonQuery(sqlpre + updfields + sqlsuf);
            log.Write($"DbUpdate for Show: {TvmShowId}", "", 4);
            Mdb.Close();
            if (rows == 0) { Mdb.success = false; }
            return Mdb.success;
        }

        public bool DbInsert()
        {
            if (!isForReview && !isFollowed)
            {
                log.Write($"New Show {TvmUrl} Ignored due to Review Rules");
                Mdb.success = true;
                return Mdb.success;
            }

            Mdb.success = true;

            string values = "";
            string sqlpre = $"insert into shows values (";
            string sqlsuf = $");";

            // values += $"'{}', ";  for strings
            // values += $"{}, ";    for ints
            // values += $".... );"' for last value
            values += $"{0}, ";
            values += $"{TvmShowId}, ";
            if (isFollowed) { values += $"'Following', "; } else { values += $"'New', "; }
            values += $"'{TvmUrl}', ";
            values += $"'{ShowName.Replace("'", "''")}', ";
            values += $"'{ShowStatus}', ";
            values += $"'{PremiereDate}', ";
            values += $"'{Finder}', ";
            values += $"'{CleanedShowName.Replace("'", "''")}', ";
            values += $"'{AltShowName.Replace("'", "''")}', ";
            values += $"'{DateTime.Now:yyyy-MM-dd}' ";
            int rows = Mdb.ExecNonQuery(sqlpre + values + sqlsuf);
            log.Write($"DbInsert for Show: {TvmShowId}", "", 4);
            Mdb.Close();
            if (rows == 0) { Mdb.success = false; }
            return Mdb.success;
        }

        public bool DbDelete()
        {
            Mdb.success = true;
            int rows = Mdb.ExecNonQuery($"delete from Shows where `TvmShowId` = {TvmShowId}");
            log.Write($"DbDelete for Show: {TvmShowId}", "", 4);
            Mdb.Close();
            if (rows == 0) { Mdb.success = false; }
            return Mdb.success;
        }

        private void FillViaJson(JObject showjson)
        {
            if (showjson["id"] is not null)
            {
                isOnTvmaze = true;
                TvmShowId = int.Parse(showjson["id"].ToString());
                using (TvmCommonSql tcs = new(Appinfo))
                {
                    isFollowed = tcs.IsShowIdFollowed(TvmShowId);
                    if (isFollowed) { TvmStatus = "Following"; } else { TvmStatus = "New"; }
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
                isJsonFilled = true;
                if (isDBFilled) { isFilled = true; }
            }
        }

        private void FillViaDB(int showid, bool JsonIsDone)
        {
            using (MySqlDataReader rdr = Mdb.ExecQuery($"select * from shows where `TvmShowId` = {showid};"))
            {
                isDBFilled = false;
                while (rdr.Read())
                {
                    isDBFilled = true;
                    if (JsonIsDone)
                    {
                        TvmStatus = rdr["TvmStatus"].ToString();
                        Finder = rdr["Finder"].ToString();
                        if ( rdr["TvmStatus"].ToString() == "New")
                        {
                            CheckTvm ct = new();
                            bool Followed = ct.IsFollowedShow(Appinfo, showid);
                            if (Followed) { TvmStatus = "Followed"; }
                            isFollowed = Followed;
                        }
                        AltShowName = rdr["AltShowName"].ToString();
                        UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                        TvmStatus = rdr["TvmStatus"].ToString();
                    }
                    else
                    {
                        Id = int.Parse(rdr["Id"].ToString());
                        TvmShowId = int.Parse(rdr["TvmShowId"].ToString());
                        TvmUrl = rdr["TvmUrl"].ToString();
                        ShowName = rdr["ShowName"].ToString();
                        ShowStatus = rdr["ShowStatus"].ToString();
                        PremiereDate = Convert.ToDateTime(rdr["PremiereDate"]).ToString("yyyy-MM-dd");
                        CleanedShowName = rdr["CleanedShowName"].ToString();
                    }
                    if (isJsonFilled) { isFilled = true; }
                }
            }
        }

        private bool ValidateForReview()
        {
            isForReview = false;
            if (TvmNetwork is not null)
            {
                if (TvmNetwork.ToLower() != "netflix" &&
                    TvmNetwork.ToLower() != "amazon prime video" &&
                    TvmNetwork.ToLower() != "hbo max" &&
                    TvmNetwork.ToLower() != "hbo" &&
                    TvmNetwork.ToLower() != "hulu" &&
                    TvmNetwork.ToLower() != "disney+")
                {
                    if (TvmLanguage != "")
                    {
                        if (TvmLanguage != "English")
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (TvmLanguage != "")
                {
                    if (TvmLanguage != "English")
                    {
                        return false;
                    }
                }
            }

            if (ShowStatus == "Ended") 
            {
                string compdate = Convert.ToDateTime(DateTime.Now).ToString("yyyyy");
                if (!PremiereDate.Contains(compdate)) { return false; }
            }

            switch (TvmType.ToLower())
            {
                case "sport":
                case "news":
                case "variety":
                case "game show":
                case "talk show":
                    return false;
            }
            if (TvmNetwork is not null)
            {
                switch (TvmNetwork.ToLower())
                {
                    case "youTube":
                    case "youTube premium":
                    case "facebook watch":
                    case "nick jr.":
                    case "espn":
                    case "abc kids":
                    case "disney Junior":
                    case "":
                        return false;
                }
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
                    Found.Add(show);
                }
                exectime.Stop();
                log.Write($"SearchShow Exec time: {exectime.ElapsedMilliseconds} ms.", "", 4);
            }
        }
    }

    public class SearchShowsViaNames
    {
        private List<int> Found = new();

        public List<int> Find(AppInfo appinfo, string showname)
        {
            Found = new();
            using (MariaDB Mdbr = new(appinfo))
            {
                showname = showname.Replace("'", "''");
                string sql = $"select `Id`, `TvmShowId`, `ShowName` from Shows where (`ShowName` = '{showname}' or `CleanedShowName` = '{showname}' or `AltShowName` = '{showname}');";
                MySqlDataReader rdr = Mdbr.ExecQuery(sql);
                if (rdr is null) { return Found; }
                if (!rdr.HasRows) { return Found; }
                while (rdr.Read())
                {
                    Found.Add(int.Parse(rdr["TvmShowId"].ToString()));
                }
            }
            return Found;
        }
    }

    public class SearchAllFollowed
    {
        private List<int> AllFollowed = new();

        public List<int> Find(AppInfo appinfo)
        {
            using (MariaDB Mdbr = new(appinfo))
            {
                string sql = $"select `Id`, `TvmShowId`, `ShowName` from Shows where `TvmStatus` = 'Following' order by `TvmShowId`;";
                MySqlDataReader rdr = Mdbr.ExecQuery(sql);
                if (rdr is null) { return AllFollowed; }
                if (!rdr.HasRows) { return AllFollowed; }
                while (rdr.Read())
                {
                    AllFollowed.Add(int.Parse(rdr["TvmShowId"].ToString()));
                }
            }
            return AllFollowed;
        }
    }

    public class UpdateFinder
    {
        public void ToShowRss(AppInfo appinfo, int showid)
        {
            using (MariaDB Mdbw = new(appinfo))
            {
                string sql = $"update shows set `Finder` = 'ShowRss' where `TvmShowId` = {showid};";
                appinfo.TxtFile.Write($"Executing: {sql}", "UpdateFinder", 4);
                if (Mdbw.ExecNonQuery(sql) == 0) { appinfo.TxtFile.Write($"Update of Finder unsuccessful {sql}", "", 4); }
            }
        }
    }

    public class UpdateTvmStatus : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void ToFollowed(AppInfo appinfo, int showid)
        {
            using (MariaDB Mdbw = new(appinfo))
            {
                string sql = $"update shows set `TvmStatus` = 'Following' where `TvmShowId` = {showid};";
                appinfo.TxtFile.Write($"Executing: {sql}", "UpdateFollowed", 4);
                if (Mdbw.ExecNonQuery(sql) == 0) { appinfo.TxtFile.Write($"Update to Following unsuccessful {sql}", "", 4); }
            }
        }

        public void ToReview(AppInfo appinfo, int showid)
        {
            using (MariaDB Mdbw = new(appinfo))
            {
                string sql = $"update shows set `TvmStatus` = 'Reviewing' where `TvmShowId` = {showid};";
                appinfo.TxtFile.Write($"Executing: {sql}", "UpdateFollowed", 4);
                if (Mdbw.ExecNonQuery(sql) == 0) { appinfo.TxtFile.Write($"Update to Review unsuccessful {sql}", "", 4); }
            }
        }

        public void ToUnDecided(AppInfo appinfo, int showid)
        {
            using (MariaDB Mdbw = new(appinfo))
            {
                string sql = $"update shows set `TvmStatus` = 'Undecided' where `TvmShowId` = {showid};";
                appinfo.TxtFile.Write($"Executing: {sql}", "UpdateFollowed", 4);
                if (Mdbw.ExecNonQuery(sql) == 0) { appinfo.TxtFile.Write($"Update to Undecided unsuccessful {sql}", "", 4); }
            }
        }
    }

    public class CheckTvm
    {
        public bool IsFollowedShow(AppInfo appinfo, int showid)
        {
            bool isFollowed = false;
            using (WebAPI webapi = new(appinfo))
            {
                isFollowed = webapi.CheckForFollowedShow(showid);
            }

            return isFollowed;
        }
    }

    public class CheckDb
    {
        public int FollowedCount(AppInfo appinfo)
        {
            int records = 0;
            using (MariaDB Mdbr = new(appinfo))
            {
                MySqlDataReader rdr = Mdbr.ExecQuery($"select count(*) from Followed");
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        records = int.Parse(rdr[0].ToString());
                    }
                }
                return records;
            }
        }
    }
}
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
        public string MediaType = "TS";
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
        public int    TvmUpdatedEpoch;

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
            MediaType = "TS";
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
            TvmUpdatedEpoch = 1;

            isFilled = false;
            isJsonFilled = false;
            isDBFilled = false;
            isOnTvmaze = false;
            isFollowed = false;
        }

        public void FillViaTvmaze(int showid)
        {
            int LastEvaluatedShow;
            using (TvmCommonSql ge = new(Appinfo)) { LastEvaluatedShow = ge.GetLastEvaluatedShow(); }
            using WebAPI js = new(Appinfo);
            FillViaJson(js.ConvertHttpToJObject(js.GetShow(showid)));
            FillViaDB(showid, true);
            if (!isFollowed && !isDBFilled) { ValidateForReview(LastEvaluatedShow); }
        }

        public bool DbUpdate()
        {
            // updfields += $"`` = '{}', ";
            Mdb.success = true;
            string updfields = "";
            string sqlpre = $"update shows set ";
            if (TvmStatus == "Reviewing") { TvmStatus = "New"; }
            updfields += $"`TvmStatus` = '{TvmStatus}', ";
            updfields += $"`Finder` = '{Finder}', ";
            updfields += $"`ShowStatus` = '{ShowStatus}', ";
            updfields += $"`MediaType` = '{MediaType}', ";
            updfields += $"`ShowName` = '{ShowName.Replace("'", "''")}', ";
            updfields += $"`AltShowName` = '{AltShowName.Replace("'", "''")}', ";
            updfields += $"`CleanedShowName` = '{CleanedShowName.Replace("'", "''")}', ";
            updfields += $"`PremiereDate` = '{PremiereDate}', ";
            updfields += $"`UpdateDate` = '{DateTime.Now.Date:yyyy-MM-dd}' ";
            string sqlsuf = $"where `TvmShowId` = {TvmShowId};";
            int rows = Mdb.ExecNonQuery(sqlpre + updfields + sqlsuf);
            log.Write($"DbUpdate for Show: {TvmShowId}", "", 4);
            Mdb.Close();
            if (rows == 0) { Mdb.success = false; }
            return Mdb.success;
        }

        public bool DbInsert(bool OverRide = false)
        {
            if (!isForReview && !isFollowed && !OverRide)
            {
                log.Write($"New Show {TvmUrl} is rejected because isForReview and isFollowed are set to false");
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
            values += $"'{MediaType}', ";
            values += $"'{CleanedShowName.Replace("'", "''")}', ";
            if (AltShowName == "" && ShowName.Contains(":")) { AltShowName = ShowName.Replace(":", ""); } 
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

        public bool DbDelete(int showid)
        {
            Mdb.success = true;
            int rows = Mdb.ExecNonQuery($"delete from Shows where `TvmShowId` = {showid}");
            log.Write($"DbDelete for Show: {showid}", "", 4);
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

                CleanedShowName = Common.RemoveSpecialCharsInShowname(ShowName);

                if (showjson["type"] is not null) { TvmType = showjson["type"].ToString(); }
                if (showjson["language"] is not null) { TvmLanguage = showjson["language"].ToString(); }
                if (showjson["officialSite"] is not null) { TvmOfficialSite = showjson["officialSite"].ToString(); }

                if (showjson["network"].ToString() != "")
                {
                    if (showjson["network"]["name"] is not null) { TvmNetwork = showjson["network"]["name"].ToString(); }
                    if (showjson["network"]["country"] is not null)
                    {
                        if (showjson["network"]["country"]["name"] is not null ) { TvmCountry = showjson["network"]["country"]["name"].ToString(); }
                    }
                }

                if (showjson["webChannel"].ToString() != "")
                {
                    if (showjson["webChannel"]["name"] is not null) { TvmNetwork = showjson["webChannel"]["name"].ToString(); }
                    if (showjson["webChannel"]["country"].ToString() != "")
                    {
                        if (showjson["webChannel"]["country"]["name"] is not null) { TvmCountry = showjson["webChannel"]["country"]["name"].ToString(); }
                    }
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

                if (showjson["updated"] is not null) { TvmUpdatedEpoch = int.Parse(showjson["updated"].ToString()); }

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
                            bool Following = ct.IsFollowedShow(Appinfo, showid);
                            if (Following) { TvmStatus = "Following"; }
                            isFollowed = Following;
                        }
                        AltShowName = rdr["AltShowName"].ToString();
                        if (AltShowName == "" && ShowName.Contains(":")) { AltShowName = ShowName.Replace(":", ""); }
                        UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                        MediaType = rdr["MediaType"].ToString();
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
                        if (AltShowName == "" && ShowName.Contains(":")) { AltShowName = ShowName.Replace(":", ""); }
                    }
                    if (isJsonFilled) { isFilled = true; }
                }
            }
        }

        private bool ValidateForReview(int lastshowevaluated)
        {
            isForReview = false;
            if (TvmShowId <= lastshowevaluated) { return isForReview; }

            if (TvmNetwork is not null)
            {
                if (TvmNetwork.ToLower() is not "netflix" and
                    not "amazon prime video" and
                    not "hbo max" and
                    not "hbo" and
                    not "hulu" and
                    not "disney+")
                {
                    if (TvmLanguage != "")
                    {
                        if (TvmLanguage is not "English" and not "Dutch")
                        {
                            log.Write($"Rejected {TvmShowId} due to Language {TvmLanguage} and  {TvmNetwork}");
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (TvmLanguage != "")
                {
                    if (TvmLanguage is not "English" and not "Dutch")
                    {
                        log.Write($"Rejected {TvmShowId} due to Language {TvmLanguage} and  {TvmNetwork}");
                        return false;
                    }
                }
            }

            if (ShowStatus is "Ended" or "Running")
            {
                string compdate = Convert.ToDateTime(DateTime.Now).ToString("yyyy");
                if (!PremiereDate.Contains(compdate) && PremiereDate != "1900-01-01")
                {
                    log.Write($"Rejected {TvmShowId} due to Premiere Date {PremiereDate}, Comp Date {compdate} and Status {ShowStatus}");
                    return false;
                }
            }

            switch (TvmType.ToLower())
            {
                case "sport":
                case "news":
                case "variety":
                case "game show":
                case "talk show":
                case "panel show":
                    log.Write($"Rejected {TvmShowId} due to Type {TvmType}");
                    return false;
            }
            if (TvmNetwork is not null)
            {
                switch (TvmNetwork.ToLower())
                {
                    case "youtube":
                    case "youtube premium":
                    case "facebook watch":
                    case "nick jr.":
                    case "espn":
                    case "abc kids":
                    case "disney Junior":
                    case "food network":
                    //case "":
                        log.Write($"Rejected {TvmShowId} due to Network {TvmNetwork}");
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

        public List<int> Find(AppInfo appinfo, string showname, string cleanedshowname = "", string altshowname = "")
        {
            Found = new();
            showname = showname.Replace("'", "''");
            altshowname = altshowname.Replace("'", "''");
            if (cleanedshowname == "") { cleanedshowname = Common.RemoveSuffixFromShowname(Common.RemoveSpecialCharsInShowname(showname)); }
            if (altshowname == "") { altshowname = showname; }
            using (MariaDB Mdbr = new(appinfo))
            {
                string sql = $"select `Id`, `TvmShowId`, `ShowName` from Shows where (`ShowName` = '{showname}' or `CleanedShowName` = '{cleanedshowname}' or `AltShowName` = '{altshowname}');";
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

        public List<int> Find(AppInfo appinfo, string option = "Following")
        {
            using (MariaDB Mdbr = new(appinfo))
            {
                string sql = $"select `Id`, `TvmShowId`, `ShowName` from Shows where `TvmStatus` = '{option}' order by `TvmShowId`;";
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
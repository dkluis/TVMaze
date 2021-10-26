using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;
using Newtonsoft.Json.Linq;
using Web_Lib;

namespace Entities_Lib
{
    public class Show : IDisposable
    {
        private readonly AppInfo _appinfo;
        private readonly TextFileHandler _log;

        private readonly MariaDB _mdb;
        public string AltShowName = "";
        public string CleanedShowName = "";
        public string Finder = "Multi";
        public int Id;

        public bool IsDbFilled;
        public bool IsFollowed;
        public bool IsForReview;
        public bool IsJsonFilled;
        public string MediaType = "TS";
        public string PremiereDate = "";
        public string ShowName = "";
        public string ShowStatus = "";
        public string TvmCountry;
        public string TvmImage;
        public string TvmImdb;
        public string TvmLanguage;
        public string TvmNetwork;
        public string TvmOfficialSite;
        public int TvmShowId;
        public string TvmStatus = " ";
        public string TvmSummary;

        public string TvmType;
        public int TvmUpdatedEpoch;
        public string TvmUrl = "";
        public string UpdateDate = "1900-01-01";

        public Show(AppInfo appinfo)
        {
            _appinfo = appinfo;
            _mdb = new MariaDB(appinfo);
            _log = appinfo.TxtFile;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
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

            IsJsonFilled = false;
            IsDbFilled = false;
            IsFollowed = false;
        }

        public void FillViaTvmaze(int showid)
        {
            int lastEvaluatedShow;
            using (TvmCommonSql ge = new(_appinfo))
            {
                lastEvaluatedShow = ge.GetLastEvaluatedShow();
            }

            using WebAPI js = new(_appinfo);
            FillViaJson(js.ConvertHttpToJObject(js.GetShow(showid)));
            FillViaDb(showid, true);
            if (!IsFollowed && !IsDbFilled) ValidateForReview(lastEvaluatedShow);
        }

        public bool DbUpdate()
        {
            _mdb.success = true;
            var updateFields = "";
            var sqlPrefix = "update shows set ";
            if (TvmStatus == "Reviewing" && TvmSummary != "") TvmStatus = "New";

            updateFields += $"`TvmStatus` = '{TvmStatus}', ";
            updateFields += $"`Finder` = '{Finder}', ";
            updateFields += $"`ShowStatus` = '{ShowStatus}', ";
            updateFields += $"`MediaType` = '{MediaType}', ";
            updateFields += $"`ShowName` = '{ShowName.Replace("'", "''")}', ";
            updateFields += $"`AltShowName` = '{AltShowName.Replace("'", "''")}', ";
            updateFields += $"`CleanedShowName` = '{CleanedShowName.Replace("'", "''")}', ";
            updateFields += $"`PremiereDate` = '{PremiereDate}', ";
            updateFields += $"`UpdateDate` = '{DateTime.Now.Date:yyyy-MM-dd}' ";
            var sqlSuffix = $"where `TvmShowId` = {TvmShowId};";
            var rows = _mdb.ExecNonQuery(sqlPrefix + updateFields + sqlSuffix);
            _log.Write($"DbUpdate for Show: {TvmShowId}", "", 4);
            _mdb.Close();
            if (rows == 0) _mdb.success = false;

            return _mdb.success;
        }

        public bool DbInsert(bool overRide = false, string callingApp = "")
        {
            if (!IsForReview && !IsFollowed && !overRide)
            {
                _log.Write($"New Show {TvmUrl} is rejected because isForReview and isFollowed are set to false");
                _mdb.success = true;
                return _mdb.success;
            }


            _mdb.success = true;

            var values = "";
            var sqlpre = "insert into shows values (";
            var sqlsuf = ");";

            values += "0, ";
            values += $"{TvmShowId}, ";

            if (callingApp == "UpdateShowEpochs")
                values += "'New', ";
            else if (IsFollowed)
                values += "'Following', ";
            else
                values += "'New', ";

            values += $"'{TvmUrl}', ";
            values += $"'{ShowName.Replace("'", "''")}', ";
            values += $"'{ShowStatus}', ";
            values += $"'{PremiereDate}', ";
            values += $"'{Finder}', ";
            values += $"'{MediaType}', ";
            values += $"'{CleanedShowName.Replace("'", "''")}', ";
            if (AltShowName == "" && ShowName.Contains(":")) AltShowName = ShowName.Replace(":", "");

            values += $"'{AltShowName.Replace("'", "''")}', ";
            values += $"'{DateTime.Now:yyyy-MM-dd}' ";
            var rows = _mdb.ExecNonQuery(sqlpre + values + sqlsuf);
            _log.Write($"DbInsert for Show: {TvmShowId}", "", 4);
            _mdb.Close();
            if (rows == 0) _mdb.success = false;

            return _mdb.success;
        }

        public bool DbDelete()
        {
            _mdb.success = true;
            var rows = _mdb.ExecNonQuery($"delete from Shows where `TvmShowId` = {TvmShowId}");
            _log.Write($"DbDelete for Show: {TvmShowId}", "", 4);
            _mdb.Close();
            if (rows == 0) _mdb.success = false;

            return _mdb.success;
        }

        public bool DbDelete(int showid)
        {
            _mdb.success = true;
            var rows = _mdb.ExecNonQuery($"delete from Shows where `TvmShowId` = {showid}");
            _log.Write($"DbDelete for Show: {showid}", "", 4);
            _mdb.Close();
            if (rows == 0) _mdb.success = false;

            return _mdb.success;
        }

        private void FillViaJson(JObject showjson)
        {
            if (showjson["id"] is not null)
            {
                TvmShowId = int.Parse(showjson["id"].ToString());
                using (TvmCommonSql tcs = new(_appinfo))
                {
                    IsFollowed = tcs.IsShowIdFollowed(TvmShowId);
                    if (IsFollowed)
                        TvmStatus = "Following";
                    else
                        TvmStatus = "New";
                }

                TvmUrl = showjson["url"].ToString();
                ShowName = showjson["name"].ToString();
                ShowStatus = showjson["status"].ToString();
                if (showjson["premiered"] is not null)
                {
                    PremiereDate = "1900-01-01";
                    var date = showjson["premiered"].ToString();
                    if (date != "") PremiereDate = Convert.ToDateTime(showjson["premiered"]).ToString("yyyy-MM-dd");
                }

                CleanedShowName = Common.RemoveSpecialCharsInShowname(ShowName);

                if (showjson["type"] is not null) TvmType = showjson["type"].ToString();

                if (showjson["language"] is not null) TvmLanguage = showjson["language"].ToString();

                if (showjson["officialSite"] is not null) TvmOfficialSite = showjson["officialSite"].ToString();

                if (showjson["network"].ToString() != "")
                {
                    if (showjson["network"]["name"] is not null) TvmNetwork = showjson["network"]["name"].ToString();

                    if (showjson["network"]["country"] is not null)
                        if (showjson["network"]["country"]["name"] is not null)
                            TvmCountry = showjson["network"]["country"]["name"].ToString();
                }

                if (showjson["webChannel"].ToString() != "")
                {
                    if (showjson["webChannel"]["name"] is not null)
                        TvmNetwork = showjson["webChannel"]["name"].ToString();

                    if (showjson["webChannel"]["country"]!.ToString() != "")
                        if (showjson["webChannel"]["country"]["name"] is not null)
                            TvmCountry = showjson["webChannel"]["country"]["name"].ToString();
                }

                if (showjson["externals"]["imdb"] is not null) TvmImdb = showjson["externals"]["imdb"].ToString();

                if (showjson["image"].ToString() != "")
                    if (showjson["image"]["medium"] is not null)
                        TvmImage = showjson["image"]["medium"].ToString();

                if (showjson["summary"] is not null)
                    TvmSummary = showjson["summary"].ToString();
                else
                    TvmSummary = "";

                if (showjson["updated"] is not null) TvmUpdatedEpoch = int.Parse(showjson["updated"].ToString());

                IsJsonFilled = true;
                if (IsDbFilled)
                {
                }
            }
        }

        private void FillViaDb(int showId, bool jsonIsDone)
        {
            using var rdr = _mdb.ExecQuery($"select * from shows where `TvmShowId` = {showId};");
            IsDbFilled = false;
            while (rdr.Read())
            {
                IsDbFilled = true;
                if (jsonIsDone)
                {
                    TvmStatus = rdr["TvmStatus"].ToString();
                    Finder = rdr["Finder"].ToString();
                    AltShowName = rdr["AltShowName"].ToString();
                    if (AltShowName == "" && ShowName.Contains(":")) AltShowName = ShowName.Replace(":", "");
                    UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                    MediaType = rdr["MediaType"].ToString();
                }
                else
                {
                    Id = int.Parse(rdr["Id"].ToString()!);
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString()!);
                    TvmUrl = rdr["TvmUrl"].ToString();
                    ShowName = rdr["ShowName"].ToString();
                    ShowStatus = rdr["ShowStatus"].ToString();
                    PremiereDate = Convert.ToDateTime(rdr["PremiereDate"]).ToString("yyyy-MM-dd");
                    CleanedShowName = rdr["CleanedShowName"].ToString();
                    if (AltShowName == "" && ShowName!.Contains(":")) AltShowName = ShowName.Replace(":", "");
                }

                if (IsJsonFilled)
                {
                }
            }
        }

        private void ValidateForReview(int lastShowEvaluated)
        {
            IsForReview = false;
            if (TvmShowId <= lastShowEvaluated) return;

            if (TvmNetwork is not null)
            {
                if (TvmNetwork.ToLower() is not "netflix" and
                    not "amazon prime video" and
                    not "hbo max" and
                    not "hbo" and
                    not "hulu" and
                    not "disney+")
                    if (TvmLanguage != "")
                        if (TvmLanguage is not "English" and not "Dutch")
                        {
                            _log.Write($"Rejected {TvmShowId} due to Language {TvmLanguage} and  {TvmNetwork}");
                            return;
                        }
            }
            else
            {
                if (TvmLanguage != "")
                    if (TvmLanguage is not "English" and not "Dutch")
                    {
                        _log.Write($"Rejected {TvmShowId} due to Language {TvmLanguage} and  {TvmNetwork}");
                        return;
                    }
            }

            if (ShowStatus is "Ended" or "Running")
            {
                var compdate = Convert.ToDateTime(DateTime.Now).ToString("yyyy");
                if (!PremiereDate.Contains(compdate) && PremiereDate != "1900-01-01")
                {
                    _log.Write(
                        $"Rejected {TvmShowId} due to Premiere Date {PremiereDate}, Comp Date {compdate} and Status {ShowStatus}");
                    return;
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
                    _log.Write($"Rejected {TvmShowId} due to Type {TvmType}");
                    return;
            }

            if (TvmNetwork is not null)
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
                        _log.Write($"Rejected {TvmShowId} due to Network {TvmNetwork}");
                        return;
                }
            
            IsForReview = true;
        }

        public void CloseDb()
        {
            _mdb.Close();
        }
    }

    public class SearchShowsViaNames
    {
        private List<int> _found = new();

        public List<int> Find(AppInfo appInfo, string showName, string cleanedShowName = "", string altShowName = "")
        {
            _found = new List<int>();
            showName = showName.Replace("'", "''");
            altShowName = altShowName.Replace("'", "''");
            if (cleanedShowName == "")
                cleanedShowName = Common.RemoveSuffixFromShowname(Common.RemoveSpecialCharsInShowname(showName));

            if (altShowName == "") altShowName = showName;

            using (MariaDB mDbR = new(appInfo))
            {
                var sql =
                    $"select `Id`, `TvmShowId`, `ShowName` from Shows where (`ShowName` = '{showName}' or " +
                    $"`CleanedShowName` = '{cleanedShowName}' or `AltShowName` = '{altShowName}');";
                var rdr = mDbR.ExecQuery(sql);
                if (rdr is null) return _found;

                if (!rdr.HasRows) return _found;

                while (rdr.Read()) _found.Add(int.Parse(rdr["TvmShowId"].ToString()!));
            }

            return _found;
        }
    }

    public class SearchAllFollowed
    {
        private readonly List<int> _allFollowed = new();

        public List<int> Find(AppInfo appinfo, string option = "Following")
        {
            using (MariaDB mDbR = new(appinfo))
            {
                var sql =
                    $"select `Id`, `TvmShowId`, `ShowName` from Shows where `TvmStatus` = '{option}' order by `TvmShowId`;";
                var rdr = mDbR.ExecQuery(sql);
                if (rdr is null) return _allFollowed;

                if (!rdr.HasRows) return _allFollowed;

                while (rdr.Read()) _allFollowed.Add(int.Parse(rdr["TvmShowId"].ToString()!));
            }

            return _allFollowed;
        }
    }

    public class UpdateFinder
    {
        public void ToShowRss(AppInfo appinfo, int showid)
        {
            using (MariaDB mDbW = new(appinfo))
            {
                var sql = $"update shows set `Finder` = 'ShowRss' where `TvmShowId` = {showid};";
                appinfo.TxtFile.Write($"Executing: {sql}", "UpdateFinder", 4);
                if (mDbW.ExecNonQuery(sql) == 0) appinfo.TxtFile.Write($"Update of Finder unsuccessful {sql}", "", 4);
            }
        }
    }

    public class UpdateTvmStatus : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void ToFollowed(AppInfo appInfo, int showId)
        {
            using (MariaDB mDbW = new(appInfo))
            {
                var sql = $"update shows set `TvmStatus` = 'Following' where `TvmShowId` = {showId};";
                appInfo.TxtFile.Write($"Executing: {sql}", "UpdateFollowed", 4);
                if (mDbW.ExecNonQuery(sql) == 0)
                    appInfo.TxtFile.Write($"Update to Following unsuccessful {sql}", "", 4);
            }
        }

        public void ToReview(AppInfo appInfo, int showId)
        {
            using MariaDB mDbW = new(appInfo);
            var sql = $"update shows set `TvmStatus` = 'Reviewing' where `TvmShowId` = {showId};";
            appInfo.TxtFile.Write($"Executing: {sql}", "UpdateFollowed", 4);
            if (mDbW.ExecNonQuery(sql) == 0) appInfo.TxtFile.Write($"Update to Review unsuccessful {sql}", "", 4);
        }
    }

    public class CheckTvm
    {
        public bool IsFollowedShow(AppInfo appinfo, int showid)
        {
            bool isFollowed;
            using WebAPI webapi = new(appinfo);
            isFollowed = webapi.CheckForFollowedShow(showid);

            return isFollowed;
        }
    }

    public class CheckDb
    {
        public int FollowedCount(AppInfo appInfo)
        {
            var records = 0;
            using MariaDB mDbR = new(appInfo);
            var rdr = mDbR.ExecQuery("select count(*) from Followed");
            while (rdr.Read()) records = int.Parse(rdr[0].ToString()!);
            return records;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using Common_Lib;
using DB_Lib;
using DB_Lib.Tvmaze;
using Newtonsoft.Json.Linq;
using Web_Lib;

namespace Entities_Lib;

public class Show : IDisposable
{
    private readonly AppInfo         _appInfo;
    private readonly TextFileHandler _log;
    private readonly MariaDb         _mdb;
    public           string          AltShowName     = "";
    public           string          CleanedShowName = "";
    public           string          Finder          = "Multi";
    public           int             Id;
    public           bool            IsDbFilled;
    public           bool            IsFollowed;
    public           bool            IsForReview;
    public           bool            IsJsonFilled;
    public           string          MediaType    = "TS";
    public           string          PremiereDate = "";
    public           string          ShowName     = "";
    public           string          ShowStatus   = "";
    public           string?         TvmCountry;
    public           string?         TvmImage;
    public           string?         TvmImdb;
    public           string?         TvmLanguage;
    public           string?         TvmNetwork;
    public           string?         TvmOfficialSite;
    public           int             TvmShowId;
    public           string          TvmStatus = " ";
    public           string?         TvmSummary;
    public           string?         TvmType;
    public           int             TvmUpdatedEpoch;
    public           string          TvmUrl     = "";
    public           string          UpdateDate = "1900-01-01";
    public Show(AppInfo appInfo)
    {
        _appInfo = appInfo;
        _mdb     = new MariaDb(appInfo);
        _log     = appInfo.TxtFile;
    }
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
    public void Reset()
    {
        Id              = 0;
        TvmShowId       = 0;
        TvmStatus       = " ";
        TvmUrl          = "";
        ShowName        = "";
        ShowStatus      = " ";
        PremiereDate    = "1970-01-01";
        Finder          = "Multi";
        MediaType       = "TS";
        CleanedShowName = "";
        AltShowName     = "";
        UpdateDate      = "1900-01-01";

        TvmType         = "";
        TvmLanguage     = "";
        TvmOfficialSite = "";
        TvmNetwork      = "";
        TvmCountry      = "";
        TvmImdb         = "";
        TvmImage        = "";
        TvmSummary      = "";
        TvmUpdatedEpoch = 1;

        IsJsonFilled = false;
        IsDbFilled   = false;
        IsFollowed   = false;
    }
    public void FillViaTvmaze(int showId)
    {
        using TvmCommonSql ge                = new(_appInfo);
        var                lastEvaluatedShow = ge.GetLastEvaluatedShow();
        using WebApi       js                = new(_appInfo);
        FillViaJson(js.ConvertHttpToJObject(js.GetShow(showId)));
        FillViaDb(showId, true);
        if (!IsFollowed && !IsDbFilled) ValidateForReview(lastEvaluatedShow);
    }
    public bool DbUpdate()
    {
        _mdb.Success = true;
        var          updateFields                                   = "";
        const string sqlPrefix                                      = "update shows set ";
        if (TvmStatus == "Reviewing" && TvmSummary != "") TvmStatus = "New";
        var now                                                     = DateTime.Now.Date.ToString("yyyy-MM-dd");
        UpdateDate = UpdateDate == "2200-01-01" ? UpdateDate : now;

        updateFields += $"`TvmStatus` = '{TvmStatus}', ";
        updateFields += $"`Finder` = '{Finder}', ";
        updateFields += $"`ShowStatus` = '{ShowStatus}', ";
        updateFields += $"`MediaType` = '{MediaType}', ";
        updateFields += $"`ShowName` = '{ShowName.Replace("'", "''")}', ";
        updateFields += $"`AltShowName` = '{AltShowName.Replace("'", "''")}', ";
        updateFields += $"`CleanedShowName` = '{CleanedShowName.Replace("'", "''")}', ";
        updateFields += $"`PremiereDate` = '{PremiereDate}', ";
        updateFields += $"`UpdateDate` = '{UpdateDate}' ";
        var sqlSuffix = $"where `TvmShowId` = {TvmShowId};";
        var rows      = _mdb.ExecNonQuery(sqlPrefix + updateFields + sqlSuffix);
        _log.Write($"DbUpdate for Show: {TvmShowId}", "", 4);
        _mdb.Close();
        if (rows == 0) _mdb.Success = false;

        return _mdb.Success;
    }
    public bool DbInsert(bool overRide = false, string callingApp = "")
    {
        if (!IsForReview && !IsFollowed && !overRide)
        {
            _log.Write($"New Show {TvmUrl} is rejected because isForReview and isFollowed are set to false");
            _mdb.Success = true;
            return _mdb.Success;
        }


        _mdb.Success = true;

        var          values = "";
        const string sqlPre = "insert into shows values (";
        const string sqlSuf = ");";

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
        if (AltShowName == "" && ShowName.Contains(':')) AltShowName = ShowName.Replace(":", "");

        values += $"'{AltShowName.Replace("'", "''")}', ";
        values += $"'{DateTime.Now:yyyy-MM-dd}' ";
        var rows = _mdb.ExecNonQuery(sqlPre + values + sqlSuf);
        _log.Write($"DbInsert for Show: {TvmShowId}", "", 4);
        _mdb.Close();
        if (rows == 0) _mdb.Success = false;

        return _mdb.Success;
    }
    public bool DbDelete()
    {
        _mdb.Success = true;
        var rows = _mdb.ExecNonQuery($"delete from Shows where `TvmShowId` = {TvmShowId}");
        _log.Write($"DbDelete for Show: {TvmShowId}", "", 4);
        _mdb.Close();
        if (rows == 0) _mdb.Success = false;

        return _mdb.Success;
    }
    public bool DbDelete(int showId)
    {
        _mdb.Success = true;
        var rows = _mdb.ExecNonQuery($"delete from Shows where `TvmShowId` = {showId}");
        _log.Write($"DbDelete for Show: {showId}", "", 4);
        _mdb.Close();
        if (rows == 0) _mdb.Success = false;

        return _mdb.Success;
    }
    private void FillViaJson(JObject showJson)
    {
        if (showJson["id"] is not null)
        {
            TvmShowId = int.Parse(showJson["id"]!.ToString());
            using TvmCommonSql tcs = new(_appInfo);
            IsFollowed = tcs.IsShowIdFollowed(TvmShowId);
            TvmStatus  = IsFollowed ? "Following" : "New";

            TvmUrl     = showJson["url"]!.ToString();
            ShowName   = showJson["name"]!.ToString();
            ShowStatus = showJson["status"]!.ToString();
            if (showJson["premiered"] is not null)
            {
                PremiereDate = "1900-01-01";
                var date                     = showJson["premiered"]!.ToString();
                if (date != "") PremiereDate = Convert.ToDateTime(showJson["premiered"]).ToString("yyyy-MM-dd");
            }

            CleanedShowName = Common.RemoveSpecialCharsInShowName(ShowName);

            if (showJson["type"] is not null) TvmType = showJson["type"]!.ToString();

            if (showJson["language"] is not null) TvmLanguage = showJson["language"]!.ToString();

            if (showJson["officialSite"] is not null) TvmOfficialSite = showJson["officialSite"]!.ToString();

            if (showJson["network"]?.ToString() != "")
            {
                if (showJson["network"]!["name"] is not null) TvmNetwork = showJson["network"]!["name"]!.ToString();

                if (showJson["network"]!["country"]?["name"] != null)
                    TvmCountry = showJson["network"]!["country"]!["name"]!.ToString();
            }

            if (showJson["webChannel"]?.ToString() != "")
            {
                if (showJson["webChannel"]!["name"] is not null)
                    TvmNetwork = showJson["webChannel"]!["name"]!.ToString();

                if (showJson["webChannel"]!["country"]!.ToString() != "")
                    if (showJson["webChannel"]!["country"]!["name"] is not null)
                        TvmCountry = showJson["webChannel"]!["country"]!["name"]!.ToString();
            }

            if (showJson["externals"]?["imdb"] is not null) TvmImdb = showJson["externals"]!["imdb"]!.ToString();

            if (showJson["image"]?.ToString() != "")
                if (showJson["image"]!["medium"] is not null)
                    TvmImage = showJson["image"]!["medium"]!.ToString();

            TvmSummary = showJson["summary"] is not null ? showJson["summary"]!.ToString() : "";
            if (showJson["updated"] is not null) TvmUpdatedEpoch = int.Parse(showJson["updated"]!.ToString());

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
                TvmStatus   = rdr["TvmStatus"].ToString()!;
                Finder      = rdr["Finder"].ToString()!;
                AltShowName = rdr["AltShowName"].ToString()!;
                if (AltShowName == "" && ShowName!.Contains(":")) AltShowName = ShowName.Replace(":", "");
                UpdateDate = Convert.ToDateTime(rdr["UpdateDate"]).ToString("yyyy-MM-dd");
                MediaType  = rdr["MediaType"].ToString()!;
            } else
            {
                Id              = int.Parse(rdr["Id"].ToString()!);
                TvmShowId       = int.Parse(rdr["TvmShowId"].ToString()!);
                TvmUrl          = rdr["TvmUrl"].ToString()!;
                ShowName        = rdr["ShowName"].ToString()!;
                ShowStatus      = rdr["ShowStatus"].ToString()!;
                PremiereDate    = Convert.ToDateTime(rdr["PremiereDate"]).ToString("yyyy-MM-dd");
                CleanedShowName = rdr["CleanedShowName"].ToString()!;
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

        if (!string.IsNullOrEmpty(TvmLanguage) && !string.IsNullOrWhiteSpace(TvmLanguage))
            if (TvmLanguage.ToLower() != "english")
            {
                _log.Write($"Rejected {TvmShowId} due to Language {TvmLanguage} and  {TvmNetwork}");
                return;
            }

        if (ShowStatus is "Ended" or "Running")
        {
            var compareDate  = DateOnly.FromDateTime(DateTime.Now).AddYears(-1);
            var premiereDate = DateOnly.Parse(PremiereDate);
            if (premiereDate < compareDate && PremiereDate != "1900-01-01")
            {
                _log.Write(
                           $"Rejected {TvmShowId} due to Premiere Date {premiereDate}, Comp Date {compareDate} and Status {ShowStatus}");
                return;
            }
        }

        switch (TvmType!.ToLower())
        {
            case "sport":
            case "news":
            case "variety":
            case "game show":
            case "talk show":
            case "panel show":
            case "award show":
            case "reality":
                _log.Write($"Rejected {TvmShowId} due to Type {TvmType}");
                return;
        }

        if (TvmNetwork is not null)
        {
            switch (TvmNetwork.ToLower())
            {
                // ReSharper disable StringLiteralTypo
                case "youtube":
                case "youtube premium":
                case "facebook watch":
                case "nick jr.":
                case "espn":
                case "abc kids":
                case "disney Junior":
                case "food network":
                case "chaupal":
                case "youku":
                case "svt play":
                case "tencent qq":
                case "cctv-1":
                case "iqiyi":
                case "mbn":
                case "okko":
                case "ntv":
                case "ena":
                case "premier":
                case "u+ mobile tv":
                case "bilibili":
                case "antena 3":
                case "htb":
                case "dmc":
                case "ktk":
                    // ReSharper restore StringLiteralTypo

                    _log.Write($"Rejected {TvmShowId} due to Network {TvmNetwork}");
                    return;
            }

            switch (TvmNetwork)
            {
                // ReSharper disable StringLiteralTypo
                case "TB Центр":
                case "Домашний":
                case "КиноПоиск HD":
                case "ТВ Центр":
                case "Первый канал":
                case "ЦТ СССР":
                    // ReSharper restore StringLiteralTypo
                    _log.Write($"Rejected {TvmShowId} due to Network {TvmNetwork}");
                    return;
            }
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
        _found      = new List<int>();
        showName    = showName.Replace("'", "''").Replace(" ", " ");
        altShowName = altShowName.Replace("'", "''").Replace(" ", " ");
        if (cleanedShowName == "")
            cleanedShowName = Common.RemoveSuffixFromShowName(Common.RemoveSpecialCharsInShowName(showName));

        if (altShowName == "") altShowName = showName;

        using MariaDb mDbR = new(appInfo);
        var sql = $"select `Id`, `TvmShowId`, `ShowName`, `ShowStatus` from Shows where " +
                  $"(`ShowName` = '{showName}' or `CleanedShowName` = '{cleanedShowName}' or `AltShowName` = '{altShowName}') and `ShowStatus` != 'Ended';";
        var rdr = mDbR.ExecQuery(sql);

        if (!rdr.HasRows)
        {
            mDbR.Close();
            sql =
                $"select `Id`, `TvmShowId`, `ShowName`, `ShowStatus` from Shows where (`ShowName` = '{showName}' or " +
                $"`CleanedShowName` = '{cleanedShowName}' or `AltShowName` = '{altShowName}');";
            rdr = mDbR.ExecQuery(sql);
        }

        var splitShowName                         = altShowName.Split(" (");
        if (splitShowName.Length > 0) altShowName = splitShowName[0];
        if (!rdr.HasRows)
        {
            mDbR.Close();
            sql =
                $"select `Id`, `TvmShowId`, `ShowName`, `ShowStatus` from Shows where (`ShowName` = '{showName}' or " +
                $"`CleanedShowName` = '{cleanedShowName}' or `AltShowName` = '{altShowName}');";
            rdr = mDbR.ExecQuery(sql);
        }

        if (!rdr.HasRows) return _found;
        while (rdr.Read()) _found.Add(int.Parse(rdr["TvmShowId"].ToString()!));

        return _found;
    }
}
public class SearchAllFollowed
{
    private readonly List<int> _allFollowed = new();
    public List<int> Find(AppInfo appInfo, string option = "Following")
    {
        using MariaDb mDbR = new(appInfo);
        var sql =
            $"select `Id`, `TvmShowId`, `ShowName` from Shows where `TvmStatus` = '{option}' order by `TvmShowId`;";
        var rdr = mDbR.ExecQuery(sql);
        if (rdr is null) return _allFollowed;

        if (!rdr.HasRows) return _allFollowed;

        while (rdr.Read()) _allFollowed.Add(int.Parse(rdr["TvmShowId"].ToString()!));

        return _allFollowed;
    }
}
public class UpdateFinder
{
    public static void ToShowRss(AppInfo appInfo, int showId)
    {
        using MariaDb mDbW = new(appInfo);
        var           sql  = $"update shows set `Finder` = 'ShowRss' where `TvmShowId` = {showId};";
        appInfo.TxtFile.Write($"Executing: {sql}", "UpdateFinder", 4);
        if (mDbW.ExecNonQuery(sql) == 0) appInfo.TxtFile.Write($"Update of Finder unsuccessful {sql}", "", 4);
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
        using MariaDb mDbW = new(appInfo);
        var sql =
            $"update shows set `TvmStatus` = 'Following' where `TvmShowId` = {showId} and `TvmStatus` != 'Skipping';";
        appInfo.TxtFile.Write($"Executing: {sql}", "UpdateFollowed", 4);
        if (mDbW.ExecNonQuery(sql) == 0)
            appInfo.TxtFile.Write($"Update to Following unsuccessful {sql}", "", 4);
    }
    public void ToReview(AppInfo appInfo, int showId)
    {
        using MariaDb mDbW = new(appInfo);
        var           sql  = $"update shows set `TvmStatus` = 'Reviewing' where `TvmShowId` = {showId};";
        appInfo.TxtFile.Write($"Executing: {sql}", "UpdateFollowed", 4);
        if (mDbW.ExecNonQuery(sql) == 0) appInfo.TxtFile.Write($"Update to Review unsuccessful {sql}", "", 4);
    }
}
public class CheckTvm
{
    public bool IsFollowedShow(AppInfo appInfo, int showId)
    {
        using WebApi webapi     = new(appInfo);
        var          isFollowed = webapi.CheckForFollowedShow(showId);
        return isFollowed;
    }
}
public class CheckDb
{
    public static int FollowedCount(AppInfo appInfo)
    {
        var           records      = 0;
        using MariaDb mDbR         = new(appInfo);
        var           rdr          = mDbR.ExecQuery("select count(*) from Followed");
        while (rdr.Read()) records = int.Parse(rdr[0].ToString()!);
        return records;
    }
}

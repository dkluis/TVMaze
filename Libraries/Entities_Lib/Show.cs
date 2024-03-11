using System;
using System.Collections.Generic;
using System.Linq;

using Common_Lib;

using DB_Lib_EF.Entities;
using DB_Lib_EF.Models.MariaDB;

using DB_Lib.Tvmaze;

using Newtonsoft.Json.Linq;

using Web_Lib;

namespace Entities_Lib;

public class Show : IDisposable
{
    private readonly AppInfo _appInfo;
    private          bool    _success;
    public           string  AltShowName     = "";
    public           string  CleanedShowName = "";
    public           string  Finder          = "Multi";
    public           int     Id;
    public           bool    IsDbFilled;
    public           bool    IsFollowed;
    public           bool    IsForReview;
    public           bool    IsJsonFilled;
    public           string  MediaType    = "TS";
    public           string  PremiereDate = "";
    public           string  ShowName     = "";
    public           string  ShowStatus   = "";
    public           string? TvmCountry;
    public           string? TvmImage;
    public           string? TvmImdb;
    public           string? TvmLanguage;
    public           string? TvmNetwork;
    public           string? TvmOfficialSite;
    public           int     TvmShowId;
    public           string  TvmStatus = " ";
    public           string? TvmSummary;
    public           string? TvmType;
    public           int     TvmUpdatedEpoch;
    public           string  TvmUrl     = "";
    public           string  UpdateDate = "1900-01-01";

    public Show(AppInfo appInfo)
    {
        _appInfo = appInfo;
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
        var jFill = FillViaJson(js.ConvertHttpToJObject(js.GetShow(showId)));
        FillViaDb(showId, jFill);
        if (!IsFollowed && !IsDbFilled) ValidateForReview(lastEvaluatedShow);
    }

    public bool DbUpdate()
    {
        try
        {
            using var db        = new TvMaze();
            var       checkShow = db.Shows.SingleOrDefault(s => s.TvmShowId == TvmShowId);
            var       nowDate   = DateTime.Now.Date.ToString("yyyy-MM-dd");
            UpdateDate = UpdateDate == "2200-01-01" ? UpdateDate : nowDate;

            if (checkShow != null)
            {
                checkShow.TvmStatus       = TvmStatus;
                checkShow.Finder          = Finder;
                checkShow.ShowStatus      = ShowStatus;
                checkShow.MediaType       = MediaType;
                checkShow.ShowName        = ShowName;
                checkShow.AltShowname     = AltShowName;
                checkShow.CleanedShowName = CleanedShowName.Replace("ʻ", "");
                checkShow.PremiereDate    = DateOnly.Parse(PremiereDate);
                checkShow.UpdateDate      = DateOnly.Parse(UpdateDate);
                db.SaveChanges();

                return true;
            } else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            LogModel.Record(_appInfo.Program, "Show - DbUpdate", $"Error: {e.Message} ::: {e.InnerException}", 20);

            return false;
        }
    }

    public bool DbInsert(bool overRide = false, string callingApp = "")
    {
        if (!IsForReview && !IsFollowed && !overRide)
        {
            LogModel.Record(_appInfo.Program, "Show Entity", "New Show {TvmUrl} is rejected because isForReview and isFollowed are set to false", 3);
            _success = true;

            return _success;
        }

        try
        {
            _success = true;

            using var db      = new TvMaze();
            var       showRec = new DB_Lib_EF.Models.MariaDB.Show();
            showRec.TvmShowId       = TvmShowId;
            showRec.TvmStatus       = callingApp == "UpdateShowEpochs" ? "New" : IsFollowed ? "Following" : "New";
            showRec.TvmUrl          = TvmUrl;
            showRec.ShowName        = ShowName;
            showRec.ShowStatus      = ShowStatus;
            showRec.PremiereDate    = DateOnly.Parse(PremiereDate);
            showRec.Finder          = Finder;
            showRec.MediaType       = MediaType;
            showRec.CleanedShowName = CleanedShowName.Replace("ʻ", "");
            if (AltShowName == "" && ShowName.Contains(':')) AltShowName = ShowName.Replace(":", "");
            showRec.AltShowname = AltShowName;
            showRec.UpdateDate  = DateOnly.Parse(DateTime.Now.ToString("yyyy-MM-dd"));
            db.Shows.Add(showRec);
            db.SaveChanges();

            return _success;
        }
        catch (Exception e)
        {
            LogModel.Record(_appInfo.Program, "Show Entity", $"Error: {e.Message} ::: {e.InnerException}", 20);
            _success = false;

            return _success;
        }
    }

    public bool DbDelete()
    {
        _success = true;

        try
        {
            using var db           = new TvMaze();
            var       showToDelete = db.Shows.SingleOrDefault(s => s.TvmShowId == TvmShowId);

            if (showToDelete == null)
            {
                return false;
            }

            var episodesToClear = db.Episodes.Where(e => e.TvmShowId == TvmShowId && (e.PlexStatus != " " || e.PlexDate != null)).ToList();

            foreach (var episode in episodesToClear)
            {
                var wa     = new WebApi(_appInfo);
                var result = wa.PutEpisodesToUnmarked(episode.TvmEpisodeId);

                if (!result.IsSuccessStatusCode)
                {
                    LogModel.Record(_appInfo.Program, "Show Entity", $"Failed to Unmark Episode {episode.TvmEpisodeId} {episode.SeasonEpisode} for {episode.TvmShowId}", 20);
                }
            }

            db.Remove(showToDelete);
            db.SaveChanges();

            return _success;
        }
        catch (Exception e)
        {
            LogModel.Record(_appInfo.Program, "Show Entity", $"Error: {e.Message} ::: {e.InnerException}", 20);
            _success = false;

            return _success;
        }
    }

    public bool DbDelete(int showId)
    {
        TvmShowId = showId;
        DbDelete();

        return _success;
    }

    private bool FillViaJson(JObject showJson)
    {
        IsJsonFilled = false;

        if (showJson["id"] is not null)
        {
            IsJsonFilled = true;
            TvmShowId    = int.Parse(showJson["id"]!.ToString());
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

            CleanedShowName = Common.RemoveSpecialCharsInShowName(ShowName).Replace("ʻ", "");

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

            if (IsDbFilled)
            {
            }
        }

        return IsJsonFilled;
    }

    private void FillViaDb(int showId, bool jsonIsDone)
    {
        try
        {
            using var db      = new TvMaze();
            var       showRec = db.Shows.SingleOrDefault(s => s.TvmShowId == showId);

            if (showRec == null)
            {
                IsDbFilled = false;
            } else
            {
                IsDbFilled = true;
            }

            if (jsonIsDone && IsDbFilled)
            {
                TvmStatus   = showRec!.TvmStatus;
                Finder      = showRec.Finder;
                AltShowName = showRec.AltShowname;
                if (AltShowName == "" && ShowName.Contains(":")) AltShowName = ShowName.Replace(":", "");
                UpdateDate = showRec.UpdateDate.ToString();
                MediaType  = showRec.MediaType!;
            } else if (IsDbFilled)
            {
                Id              = showRec!.Id;
                TvmShowId       = showRec.TvmShowId;
                TvmUrl          = showRec.TvmUrl!;
                ShowName        = showRec.ShowName;
                ShowStatus      = showRec.ShowStatus;
                PremiereDate    = showRec.PremiereDate.ToString();
                CleanedShowName = showRec.CleanedShowName.Replace("ʻ", "");
                UpdateDate      = showRec.UpdateDate.ToString();
                if (AltShowName == "" && ShowName.Contains(":")) AltShowName = ShowName.Replace(":", "");
            }
        }
        catch (Exception e)
        {
            LogModel.Record(_appInfo.Program, $"Show Entity", $"Error: {e.Message} ::: {e.InnerException}", 20);
        }
    }

    private void ValidateForReview(int lastShowEvaluated)
    {
        IsForReview = false;

        if (TvmShowId <= lastShowEvaluated) return;

        if (!string.IsNullOrEmpty(TvmLanguage) && !string.IsNullOrWhiteSpace(TvmLanguage))
            if (TvmLanguage.ToLower() != "english")
            {
                LogModel.Record(_appInfo.Program, "Show Entity", $"Rejected {TvmShowId} due to Language {TvmLanguage} and  {TvmNetwork}: {TvmUrl}");

                return;
            }

        if (ShowStatus is "Ended" or "Running")
        {
            var compareDate  = DateOnly.FromDateTime(DateTime.Now).AddYears(-1);
            var premiereDate = DateOnly.Parse(PremiereDate);

            if (premiereDate < compareDate && PremiereDate != "1900-01-01")
            {
                LogModel.Record(_appInfo.Program, "Show Entity", $"Rejected {TvmShowId} due to Premiere Date {premiereDate}, Comp Date {compareDate} and Status {ShowStatus}: {TvmUrl}");

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
                LogModel.Record(_appInfo.Program, "Show Entity", $"Rejected {TvmShowId} due to Type {TvmType}:  {TvmUrl}");

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
                case "rutube":
                case "ard mediathek":
                case "MBS":
                    // ReSharper restore StringLiteralTypo
                    LogModel.Record(_appInfo.Program, "Show Entity", $"Rejected {TvmShowId} due to Network {TvmNetwork}:  {TvmUrl}");

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
                    LogModel.Record(_appInfo.Program, "Show Entity", $"Rejected {TvmShowId} due to Network {TvmNetwork}: {TvmUrl}");

                    return;
            }
        }

        IsForReview = true;
    }
}

public class SearchShowsViaNames
{
    private List<int> _found = new();

    public List<int> Find(AppInfo appInfo, string showName, string cleanedShowName = "", string altShowName = "")
    {
        try
        {
            _found      = new List<int>();
            showName    = showName.Replace(" ", " ");
            if (altShowName == "") altShowName = showName;
            altShowName = altShowName.Replace(" ", " ").Replace(":", "").Replace("\u2019", "\u0027");
            if (cleanedShowName == "") cleanedShowName = Common.RemoveSuffixFromShowName(Common.RemoveSpecialCharsInShowName(showName));

            var       tvmStatusList = new List<string> {"Ended", "Skipping"};
            var       dateStr       = "1900-01-01";
            var       premDate      = DateOnly.Parse(dateStr);
            using var db            = new TvMaze();

            var shows = db.Shows
                          .Where(s => (s.ShowName.ToLower() == showName.ToLower() || s.CleanedShowName.ToLower() == cleanedShowName.ToLower() || s.AltShowname.ToLower() == altShowName.ToLower()) &&
                                      !tvmStatusList.Contains(s.TvmStatus)                                                                                                                         &&
                                      s.PremiereDate != premDate)
                          .ToList();

            if (shows != null && shows.Count > 0)
            {
                foreach (var show in shows)
                {
                    _found.Add(show.TvmShowId);
                }
            } else
            {
                shows = db.Shows.Where(s => (s.ShowName.ToLower() == showName.ToLower() || s.CleanedShowName.ToLower() == cleanedShowName.ToLower() || s.AltShowname.ToLower() == altShowName.ToLower()) &&
                                            s.ShowStatus != "Skipping")
                          .ToList();

                if (shows != null && shows.Count > 0)
                {
                    foreach (var show in shows)
                    {
                        _found.Add(show.TvmShowId);
                    }
                } else
                {
                    var splitShowName                         = altShowName.Split(" (");
                    if (splitShowName.Length > 0) altShowName = splitShowName[0];

                    shows = db.Shows.Where(s => (s.ShowName.ToLower() == showName.ToLower() || s.CleanedShowName.ToLower() == cleanedShowName.ToLower() || s.AltShowname.ToLower() == altShowName.ToLower())
                                             && s.ShowStatus != "Skipping")
                              .ToList();

                    if (shows != null && shows.Count > 0)
                    {
                        foreach (var show in shows)
                        {
                            _found.Add(show.TvmShowId);
                        }
                    }
                }
            }

            return _found;
        }
        catch (Exception e)
        {
            LogModel.Record(appInfo.Program, "Show Entity", $"Error during Find {e.Message} :::{e.InnerException}", 20);

            return _found;
        }
    }
}

public class SearchAllFollowed
{
    private readonly List<int> _allFollowed = new();

    public List<int> Find(AppInfo appInfo, string option = "Following")
    {
        using var db    = new TvMaze();
        var       shows = db.Shows.Where(s => s.TvmStatus == option).OrderBy(s => s.TvmShowId).Select(s => s.TvmShowId);
        _allFollowed.AddRange(shows);

        return _allFollowed;
    }
}

public class UpdateFinder
{
    public static void ToShowRss(AppInfo appInfo, int showId)
    {
        try
        {
            using var db   = new TvMaze();
            var       show = db.Shows.SingleOrDefault(s => s.TvmShowId == showId);

            if (show == null)
            {
                LogModel.Record(appInfo.Program, "Show Entity", $"To ShowRss: Could not find {showId}", 20);

                return;
            }

            show.Finder = "ShowRss";
            db.SaveChanges();
        }
        catch (Exception e)
        {
            LogModel.Record(appInfo.Program, "Show Entity", $"To ShowRss Error: {e.Message} ::: {e.InnerException}", 20);
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
        try
        {
            using var db   = new TvMaze();
            var       show = db.Shows.SingleOrDefault(s => s.TvmShowId == showId);

            if (show == null)
            {
                LogModel.Record(appInfo.Program, "Show Entity", $"To Followed: Could not find {showId}", 20);

                return;
            } else
            {
                if (show.TvmStatus == "Skipping") return;
            }

            show.TvmStatus = "Following";
            db.SaveChanges();
        }
        catch (Exception e)
        {
            LogModel.Record(appInfo.Program, "Show Entity", $"To Followed Error: {e.Message} ::: {e.InnerException}", 20);
        }
    }

    public void ToReview(AppInfo appInfo, int showId)
    {
        try
        {
            using var db   = new TvMaze();
            var       show = db.Shows.SingleOrDefault(s => s.TvmShowId == showId);

            if (show == null)
            {
                LogModel.Record(appInfo.Program, "Show Entity", $"To Reviewing: Could not find {showId}", 20);

                return;
            }

            show.TvmStatus = "Reviewing";
            db.SaveChanges();
        }
        catch (Exception e)
        {
            LogModel.Record(appInfo.Program, "Show Entity", $"To Reviewing Error: {e.Message} ::: {e.InnerException}", 20);
        }
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
        using var db = new TvMaze();

        return db.Followeds.Count();
    }
}

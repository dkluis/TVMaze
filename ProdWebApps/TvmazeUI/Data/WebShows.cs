using Common_Lib;
using DB_Lib;

namespace TvmazeUI.Data
{
    public class WebShows
    {
        public readonly AppInfo AppInfo = new("Tvmaze", "WebUI", "DbAlternate");

        public List<ShowsInfo> GetShowsByTvmStatus(string tvmStatus)
        {
            MariaDb mdbShows = new(AppInfo);
            List<ShowsInfo> newShows = new();
            var sql = $"select * from Shows where `TvmStatus` = '{tvmStatus}' order by `TvmShowId` desc";
            var rdr = mdbShows.ExecQuery(sql);
            while (rdr.Read())
            {
                ShowsInfo rec = new()
                {
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString()!),
                    ShowName = rdr["ShowName"].ToString()!,
                    TvmStatus = rdr["TvmStatus"].ToString()!,
                    TvmUrl = rdr["TvmUrl"].ToString()!,
                    Finder = rdr["Finder"].ToString()!,
                    AltShowName = rdr["AltShowName"].ToString()!,
                    MediaType = rdr["MediaType"].ToString()!,
                    UpdateDate = rdr["UpdateDate"].ToString()!
                };
                newShows.Add(rec);
            }

            return newShows;
        }

        public List<ShowsInfo> FindShows(string? showName)
        {
            MariaDb mdbShows = new(AppInfo);
            List<ShowsInfo> newShows = new();
            showName = showName?.Replace("'", "''");
            var sql =
                $"select * from Shows where `ShowName` like '%{showName}%' or `AltShowName` like '%{showName}%' order by `TvmShowId` desc limit 150";
            var rdr = mdbShows.ExecQuery(sql);
            while (rdr.Read())
            {
                ShowsInfo rec = new()
                {
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString()!),
                    ShowName = rdr["ShowName"].ToString()!,
                    TvmStatus = rdr["TvmStatus"].ToString()!,
                    ShowStatus = rdr["ShowStatus"].ToString()!,
                    TvmUrl = rdr["TvmUrl"].ToString()!,
                    Finder = rdr["Finder"].ToString()!,
                    AltShowName = rdr["AltShowName"].ToString()!,
                    MediaType = rdr["MediaType"].ToString()!,
                    UpdateDate = rdr["UpdateDate"].ToString()!
                };
                newShows.Add(rec);
            }

            return newShows;
        }

        public bool DeleteShow(int showId)
        {
            MariaDb mdbShows = new(AppInfo);
            var sql = $"delete from Shows where `TvmShowId` = {showId}";
            var resultRows = mdbShows.ExecNonQuery(sql);
            return resultRows > 0;
        }

        public bool SetTvmStatusShow(int showId, string newStatus)
        {
            MariaDb mdbShows = new(AppInfo);
            var sql = $"update shows set `TvmStatus` = '{newStatus}' where `TvmShowId` = {showId}";
            var resultRows = mdbShows.ExecNonQuery(sql);
            if (resultRows > 0)
                return true;
            return false;
        }

        public bool SetMtAndAsnShow(int showId, string mediaType, string altShowName)
        {
            MariaDb mdbShows = new(AppInfo);
            altShowName = altShowName.Replace("'", "''");
            var sql =
                $"update shows set `AltShowName` = '{altShowName}', `MediaType` = '{mediaType}' where `TvmShowId` = {showId}";
            var resultRows = mdbShows.ExecNonQuery(sql);
            if (resultRows > 0) return true;

            AppInfo.TxtFile.Write($"Edit ShowName and MediaType unsuccessful:  MediaType = {mediaType}");
            return false;
        }
    }

    public class ShowsInfo
    {
        public string? AltShowName;
        public string? Finder;
        public string? MediaType;
        public string? ShowName;
        public string? ShowStatus;
        public int TvmShowId;
        public string? TvmStatus;
        public string? TvmUrl;
        public string? UpdateDate;
    }
}
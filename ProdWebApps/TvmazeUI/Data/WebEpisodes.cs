using Common_Lib;
using DB_Lib;

namespace TvmazeUI.Data
{
    public class WebEpisodes
    {
        public readonly AppInfo AppInfo = new("Tvmaze", "WebUI", "DbAlternate");

        public List<EpisodeInfo> GetEpisodes(string showName, string season, string episode)
        {
            MariaDb mdbEpisodes = new(AppInfo);
            List<EpisodeInfo> episodeInfoList = new();
            showName = showName.Replace("'", "''");
            var sql =
                $"select * from episodesfullinfo where (`ShowName` like '%{showName}%' or `AltShowName` like '%{showName}%')";
            if (int.TryParse(season, out var seasonInt)) sql = sql + $" and `Season` = {seasonInt}";
            if (int.TryParse(episode, out var episodeInt)) sql = sql + $" and `Episode` = {episodeInt}";
            sql += " limit 150";
            var rdr = mdbEpisodes.ExecQuery(sql);
            while (rdr.Read())
            {
                EpisodeInfo ei = new()
                {
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString()!),
                    ShowName = rdr["ShowName"].ToString()!,
                    Season = rdr["Season"].ToString()!,
                    Episode = rdr["Episode"].ToString()!,
                    BroadcastDate = rdr["BroadcastDate"].ToString()!,
                    TvmUrl = rdr["TvmUrl"].ToString()!,
                    PlexStatus = rdr["PlexStatus"].ToString()!,
                    PlexDate = rdr["PlexDate"].ToString()!,
                    UpdateDate = rdr["UpdateDate"].ToString()![..10]
                };
                if (ei.BroadcastDate!.Length > 10) ei.BroadcastDate = ei.BroadcastDate[..10];
                if (ei.PlexDate!.Length > 10) ei.PlexDate = ei.PlexDate[..10];
                episodeInfoList.Add(ei);
            }

            AppInfo.TxtFile.Write($"Executing in Episodes Page: {sql}: found {episodeInfoList.Count} records", "", 4);
            return episodeInfoList;
        }

        public List<EpisodeInfo> GetEpisodesToAcquire(bool includeShowRss)
        {
            MariaDb mdbAcquire = new(AppInfo);
            List<EpisodeInfo> episodeInfoList = new();
            const string sqlNoShowRss =
                "select * from episodesfromtodayback where finder = 'Multi' order by `BroadcastDate`, `ShowName`";
            const string sqlWithShowRss = "select * from episodesfromtodayback order by `BroadcastDate`, `ShowName`";
            var sql = includeShowRss ? sqlWithShowRss : sqlNoShowRss;
            var rdr = mdbAcquire.ExecQuery(sql);
            while (rdr.Read())
            {
                EpisodeInfo ei = new()
                {
                    TvmShowId = int.Parse(rdr["TvmShowId"].ToString()!),
                    ShowName = rdr["ShowName"].ToString()!,
                    Season = rdr["Season"].ToString()!,
                    Episode = rdr["Episode"].ToString()!,
                    BroadcastDate = rdr["BroadcastDate"].ToString()!,
                    TvmUrl = rdr["TvmUrl"].ToString()!,
                    PlexStatus = rdr["PlexStatus"].ToString()!,
                    PlexDate = "",
                    UpdateDate = rdr["UpdateDate"].ToString()![..10]
                };
                if (ei.BroadcastDate!.Length > 10) ei.BroadcastDate = ei.BroadcastDate[..10];
                episodeInfoList.Add(ei);
            }
            return episodeInfoList;
        }
    }
    
    public class EpisodeInfo
    {
        public string? BroadcastDate;
        public string? Episode;
        public string? PlexDate;
        public string? PlexStatus;
        public string? Season;
        public string? ShowName;
        public int TvmShowId;
        public string? TvmUrl;
        public string? UpdateDate;
    }
}
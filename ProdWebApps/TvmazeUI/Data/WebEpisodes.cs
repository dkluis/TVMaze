using Common_Lib;

using DB_Lib;

using DB_Lib_EF.Entities;

namespace TvmazeUI.Data;

public class WebEpisodes
{
    public readonly AppInfo AppInfo = new("Tvmaze", "Web UI", "DbAlternate");

    public List<EpisodeInfo> GetEpisodes(string showName, string season, string episode)
    {
        MariaDb           mdbEpisodes     = new(AppInfo);
        List<EpisodeInfo> episodeInfoList = new();
        showName = showName.Replace("'", "''");
        var sql                                            = $"select * from EpisodesFullInfo where (`ShowName` like '%{showName}%' or `AltShowName` like '%{showName}%')";
        if (int.TryParse(season,  out var seasonInt)) sql  = sql + $" and `Season` = {seasonInt}";
        if (int.TryParse(episode, out var episodeInt)) sql = sql + $" and `Episode` = {episodeInt}";
        sql += " limit 150";
        var rdr = mdbEpisodes.ExecQuery(sql);

        while (rdr.Read())
        {
            var broadcastDate = DateTime.Parse(rdr["BroadcastDate"].ToString()!).ToString("yyyy-MM-dd");
            var plexDate      = !string.IsNullOrEmpty(rdr["PlexDate"].ToString()!) ? DateTime.Parse(rdr["PlexDate"].ToString()!).ToString("yyyy-MM-dd") : " ";
            var updateDate    = DateTime.Parse(rdr["UpdateDate"].ToString()!).ToString("yyyy-MM-dd");

            EpisodeInfo episodeInfo = new()
                                      {
                                          TvmShowId     = int.Parse(rdr["TvmShowId"].ToString()!),
                                          ShowName      = rdr["ShowName"].ToString()!,
                                          Season        = rdr["Season"].ToString()!,
                                          Episode       = rdr["Episode"].ToString()!,
                                          BroadcastDate = broadcastDate,
                                          TvmUrl        = rdr["TvmUrl"].ToString()!,
                                          PlexStatus    = rdr["PlexStatus"].ToString()!,
                                          PlexDate      = plexDate,
                                          UpdateDate    = updateDate,
                                      };
            episodeInfoList.Add(episodeInfo);
        }

        LogModel.Record(AppInfo.Program, "Web Episodes", $"Executing inEpisodesPage: {sql}: found {episodeInfoList.Count} records", 4);

        return episodeInfoList;
    }

    public List<EpisodeInfo> GetEpisodesToAcquire(bool includeShowRss)
    {
        MariaDb           mdbAcquire      = new(AppInfo);
        List<EpisodeInfo> episodeInfoList = new();
        const string      sqlNoShowRss    = "select * from EpisodesFromTodayBack where finder = 'Multi' order by `BroadcastDate`, `ShowName`";
        const string      sqlWithShowRss  = "select * from EpisodesFromTodayBack order by `BroadcastDate`, `ShowName`";
        var               sql             = includeShowRss ? sqlWithShowRss : sqlNoShowRss;
        var               rdr             = mdbAcquire.ExecQuery(sql);

        while (rdr.Read())
        {
            EpisodeInfo ei = new()
                             {
                                 TvmShowId     = int.Parse(rdr["TvmShowId"].ToString()!),
                                 ShowName      = rdr["ShowName"].ToString()!,
                                 Season        = rdr["Season"].ToString()!,
                                 Episode       = rdr["Episode"].ToString()!,
                                 BroadcastDate = DateTime.Parse(rdr["BroadcastDate"].ToString()!).ToString("yyyy-MM-dd"),
                                 TvmUrl        = rdr["TvmUrl"].ToString()!,
                                 PlexStatus    = rdr["PlexStatus"].ToString()!,
                                 PlexDate      = null,
                                 UpdateDate    = DateTime.Parse(rdr["UpdateDate"].ToString()!).ToString("yyyy-MM-dd"),
                             };
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
    public int     TvmShowId;
    public string? TvmUrl;
    public string? UpdateDate;
}

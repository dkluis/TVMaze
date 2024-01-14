using DB_Lib_EF.Models.MariaDB;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DB_Lib_EF.Entities;

public class ViewEntities : IDisposable
{
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Release managed resources here
            }

            // Release unmanaged resources here

            _disposed = true;
        }
    }

    ~ViewEntities()
    {
        Dispose(false);
    }

    #region GetEpisodesToAcquire

    public static Response GetEpisodesToAcquire()
    {
        var resp = new Response();

        try
        {
            using var db    = new TvMaze();
            var       today = DateOnly.FromDateTime(DateTime.Now);

            var query = from e in db.Episodes
                        join s in db.Shows on e.TvmShowId equals s.TvmShowId
                        where e.BroadcastDate.HasValue && e.BroadcastDate.Value <= today && e.PlexStatus == " " && s.TvmStatus == "Following" && s.Finder != "Skip"
                        orderby e.TvmShowId, e.SeasonEpisode
                        select new ShowEpisode
                               {
                                   TvmShowId       = e.TvmShowId,
                                   ShowName        = s.ShowName,
                                   CleanedShowName = s.ShowName,
                                   AltShowName     = s.AltShowname,
                                   ShowStatus      = s.TvmStatus,
                                   TvmEpisodeId    = e.TvmEpisodeId,
                                   TvmUrl          = e.TvmUrl,
                                   SeasonEpisode   = e.SeasonEpisode,
                                   Season          = e.Season,
                                   Episode         = e.Episode1,
                                   BroadcastDate   = e.BroadcastDate,
                                   PlexStatus      = e.PlexStatus,
                                   PlexDate        = e.PlexDate,
                                   UpdateDate      = e.UpdateDate,
                                   Finder          = s.Finder,
                               };
            var episodeToAcquire = query.ToList();
            resp.ResponseObject = episodeToAcquire;
            resp.WasSuccess     = true;
        }
        catch (DbUpdateException e) // catch specific exception
        {
            resp.Message = $"An error occurred retrieving Episode to Acquire. Error: {e.Message} {e.InnerException}";
            LogModel.Record("Exceptions", "GetEpisodesToAcquire", $"Error: {e.Message} ::: {e.InnerException}", 20);
        }

        return resp;
    }

    public class ShowEpisode
    {
        public int       TvmShowId       { get; set; }
        public string?   ShowName        { get; set; }
        public string?   CleanedShowName { get; set; }
        public string?   AltShowName     { get; set; }
        public string?   ShowStatus      { get; set; }
        public int       TvmEpisodeId    { get; set; }
        public string?   TvmUrl          { get; set; }
        public string?   SeasonEpisode   { get; set; }
        public int       Season          { get; set; }
        public int       Episode         { get; set; }
        public DateOnly? BroadcastDate   { get; set; }
        public string?   PlexStatus      { get; set; }
        public DateOnly? PlexDate        { get; set; }
        public DateOnly? UpdateDate      { get; set; }
        public string?   Finder          { get; set; }
    }

    #endregion

    #region GetShowsToRefresh

    public static Response GetShowsToRefresh()
    {
        var response         = new Response();
        var sevenDaysAgo     = DateOnly.FromDateTime(DateTime.Now.AddDays(-7));
        var thirtyOneDaysAgo = DateOnly.FromDateTime(DateTime.Now.AddDays(-31));
        var oldDate          = DateOnly.FromDateTime(new DateTime(1900, 1, 1));

        try
        {
            using var db = new TvMaze();

            var query = db.Shows
                          .Where(s => ((s.TvmStatus  != "Ended"   && s.TvmStatus    != "Skipping" && s.UpdateDate < sevenDaysAgo) ||
                                       (s.ShowStatus == "Ended"   && s.PremiereDate == oldDate    && s.UpdateDate < sevenDaysAgo) ||
                                       (s.ShowStatus == "Running" && s.UpdateDate   < sevenDaysAgo)                               ||
                                       s.UpdateDate <= thirtyOneDaysAgo))
                          .OrderBy(s => s.TvmShowId)
                          .Select(s => new ShowToRefresh()
                                       {
                                           TvmShowId    = s.TvmShowId,
                                           TvmStatus    = s.TvmStatus,
                                           PremiereDate = s.PremiereDate,
                                           UpdateDate   = s.UpdateDate,
                                           ShowName     = s.ShowName,
                                           TvmUrl       = s.TvmUrl,
                                           ShowStatus   = s.ShowStatus
                                       });
            var showsToRefresh = query.ToList();

            if (showsToRefresh == null)
            {
                response.Message    = "No shows to refresh.";
                response.WasSuccess = false;

                return response;
            }

            response.ResponseObject = showsToRefresh;
            response.WasSuccess     = true;
        }
        catch (Exception e)
        {
            response.Message = $"An error occurred retrieving Episode to Acquire. Error: {e.Message} {e.InnerException}";
            LogModel.Record("Exceptions", "GetShowsToRefresh", $"Error: {e.Message} ::: {e.InnerException}", 20);
        }

        return response;
    }

    public class ShowToRefresh
    {
        public int      TvmShowId    { get; set; }
        public string?  TvmStatus    { get; set; }
        public DateOnly PremiereDate { get; set; }
        public DateOnly UpdateDate   { get; set; }
        public string?  ShowName     { get; set; }
        public string?  TvmUrl       { get; set; }
        public string?  ShowStatus   { get; set; }
    }

    #endregion

    #region GetEpisodesFullInfo

    public static Response GetEpisodesFullInfo(bool applyOrphanedFilter = false)
    {
        var resp = new Response();

        try
        {
            using var db = new TvMaze();

            var query = db.Episodes.Join(db.Shows, e => e.TvmShowId, s => s.TvmShowId, (e,      s) => new {Episode     = e, Show       = s})
                          .Join(db.MediaTypes, es => es.Show.MediaType, m => m.MediaType1, (es, m) => new {EpisodeShow = es, MediaType = m})
                          .OrderBy(esm => esm.EpisodeShow.Episode.TvmShowId)
                          .ThenBy(esm => esm.EpisodeShow.Episode.SeasonEpisode)
                          .Select(set => new {Epi = set.EpisodeShow.Episode, Show = set.EpisodeShow.Show, MediaType = set.MediaType});

            if (applyOrphanedFilter)
            {
                query = query.Where(set => set.Show.UpdateDate != set.Epi.UpdateDate && set.Epi.PlexDate == null && set.Show.TvmStatus != "Skipping");
                var orphanedEpisodes = query.Select(esm => esm.Show.TvmShowId).Distinct().ToList();
                resp.ResponseObject = orphanedEpisodes;
                resp.WasSuccess     = true;

                return resp;
            } else
            {
                var episodeFullInfo = query.Select(esm => new EpisodeShowInfo
                                                          {
                                                              Id              = esm.Epi.Id,
                                                              TvmShowId       = esm.Epi.TvmShowId,
                                                              ShowName        = esm.Show.ShowName,
                                                              CleanedShowName = esm.Show.CleanedShowName,
                                                              TvmStatus       = esm.Show.TvmStatus,
                                                              AltShowName     = esm.Show.AltShowname,
                                                              TvmEpisodeId    = esm.Epi.TvmEpisodeId,
                                                              TvmUrl          = esm.Epi.TvmUrl,
                                                              SeasonEpisode   = esm.Epi.SeasonEpisode,
                                                              Season          = esm.Epi.Season,
                                                              Episode         = esm.Epi.Episode1,
                                                              BroadcastDate   = esm.Epi.BroadcastDate,
                                                              PlexStatus      = esm.Epi.PlexStatus,
                                                              PlexDate        = esm.Epi.PlexDate,
                                                              UpdateDate      = esm.Epi.UpdateDate,
                                                              Finder          = esm.Show.Finder,
                                                              MediaType       = esm.MediaType.MediaType1,
                                                              ShowUpdateDate  = esm.Show.UpdateDate,
                                                              AutoDelete      = esm.MediaType.AutoDelete == "Yes",
                                                          })
                                           .ToList();

                resp.ResponseObject = episodeFullInfo;
                resp.WasSuccess     = true;
            }
        }
        catch (DbUpdateException e) // catch specific exception
        {
            resp.Message = $"An error occurred retrieving Episode to Acquire. Error: {e.Message} {e.InnerException}";
            LogModel.Record("Exceptions", "GetEpisodesFullInfo", $"Error: {e.Message} ::: {e.InnerException}", 20);
        }

        return resp;
    }

    public class EpisodeShowInfo
    {
        public int       Id              { get; set; }
        public int       TvmShowId       { get; set; }
        public string?   ShowName        { get; set; }
        public string?   CleanedShowName { get; set; }
        public string?   TvmStatus       { get; set; }
        public string?   AltShowName     { get; set; }
        public int       TvmEpisodeId    { get; set; }
        public string?   TvmUrl          { get; set; }
        public string?   SeasonEpisode   { get; set; }
        public int       Season          { get; set; }
        public int       Episode         { get; set; }
        public DateOnly? BroadcastDate   { get; set; }
        public string?   PlexStatus      { get; set; }
        public DateOnly? PlexDate        { get; set; }
        public DateOnly? UpdateDate      { get; set; }
        public string?   Finder          { get; set; }
        public string?   MediaType       { get; set; }
        public DateOnly? ShowUpdateDate  { get; set; }
        public bool      AutoDelete      { get; set; }
    }

    #endregion
}

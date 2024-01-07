using DB_Lib_EF.Models.MariaDB;

using Microsoft.EntityFrameworkCore;

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

    public Response GetEpisodesToAcquire()
    {
        var resp = new Response();

        try
        {
            using var db    = new TvMaze();
            var       today = DateOnly.FromDateTime(DateTime.Now);

            var query = from e in db.Episodes
                        join s in db.Shows on e.TvmShowId equals s.TvmShowId
                        where e.BroadcastDate.HasValue && e.BroadcastDate.Value <= today
                           && e.PlexStatus    == " "
                           && s.TvmStatus     == "Following"
                           && s.Finder        != "Skip"
                        orderby e.BroadcastDate, e.TvmShowId, e.SeasonEpisode
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
            resp.WasSuccess = true;
        }
        catch (DbUpdateException e) // catch specific exception
        {
            resp.Message = $"An error occurred retrieving Episode to Acquire. Error: {e.Message} {e.InnerException}";
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
}

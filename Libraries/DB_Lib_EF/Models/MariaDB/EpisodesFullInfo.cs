namespace DB_Lib_EF.Models.MariaDB;

public class EpisodesFullInfo
{
    public int Id { get; set; }

    public int TvmShowId { get; set; }

    public string ShowName { get; set; } = null!;

    public string CleanedShowName { get; set; } = null!;

    public string TvmStatus { get; set; } = null!;

    public string AltShowName { get; set; } = null!;

    public int TvmEpisodeId { get; set; }

    public string TvmUrl { get; set; } = null!;

    public string SeasonEpisode { get; set; } = null!;

    public int Season { get; set; }

    public int Episode { get; set; }

    public DateOnly? BroadcastDate { get; set; }

    public string PlexStatus { get; set; } = null!;

    public DateOnly? PlexDate { get; set; }

    public DateOnly UpdateDate { get; set; }

    public string Finder { get; set; } = null!;

    public string? MediaType { get; set; }

    public DateOnly ShowUpdateDate { get; set; }

    public string? AutoDelete { get; set; }
}

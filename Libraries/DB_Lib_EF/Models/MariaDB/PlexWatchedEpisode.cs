using System;
using System.Collections.Generic;

namespace DB_Lib_EF.Models.MariaDB;

public partial class PlexWatchedEpisode
{
    public int Id { get; set; }

    public int? TvmShowId { get; set; }

    public int? TvmEpisodeId { get; set; }

    public string PlexShowName { get; set; } = null!;

    public int PlexSeasonNum { get; set; }

    public int PlexEpisodeNum { get; set; }

    public string PlexSeasonEpisode { get; set; } = null!;

    public string PlexWatchedDate { get; set; } = null!;

    public bool ProcessedToTvmaze { get; set; }

    public string UpdateDate { get; set; } = null!;
}

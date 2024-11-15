namespace DB_Lib_EF.Models.MariaDB;

public class ShowEpisodeCount
{
    public int ShowsTvmShowId { get; set; }

    public string ShowName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Url { get; set; }

    public string ShowStatus { get; set; } = null!;

    public long? EpisodeCount { get; set; }
}

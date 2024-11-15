namespace DB_Lib_EF.Models.MariaDB;

public class Movie
{
    public string Name { get; set; } = null!;

    public string? CleanedName { get; set; }

    public string? AltName { get; set; }

    public string? SeriesName { get; set; }

    public int MovieNumber { get; set; }

    public DateTime? FinderDate { get; set; }

    public string MediaType { get; set; } = null!;

    public bool Acquired { get; set; }

    public virtual MediaType MediaTypeNavigation { get; set; } = null!;
}

namespace DB_Lib_EF.Models.MariaDB;

public class PlexStatus
{
    public string PlexStatus1 { get; set; } = null!;

    public virtual ICollection<Episode> Episodes { get; set; } = new List<Episode>();
}

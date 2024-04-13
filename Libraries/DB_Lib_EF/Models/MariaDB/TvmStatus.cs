namespace DB_Lib_EF.Models.MariaDB;

public class TvmStatus
{
    public string TvmStatus1 { get; set; } = null!;

    public virtual ICollection<Show> Shows { get; set; } = new List<Show>();
}

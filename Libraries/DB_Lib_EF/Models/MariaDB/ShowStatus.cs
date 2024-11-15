namespace DB_Lib_EF.Models.MariaDB;

public class ShowStatus
{
    public string ShowStatus1 { get; set; } = null!;

    public virtual ICollection<Show> Shows { get; set; } = new List<Show>();
}

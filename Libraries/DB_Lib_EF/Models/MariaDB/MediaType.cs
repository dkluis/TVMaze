namespace DB_Lib_EF.Models.MariaDB;

public class MediaType
{
    public int Id { get; set; }

    public string MediaType1 { get; set; } = null!;

    public string PlexLocation { get; set; } = null!;

    public string? AutoDelete { get; set; }

    public virtual ICollection<Show> Shows { get; set; } = new List<Show>();
}

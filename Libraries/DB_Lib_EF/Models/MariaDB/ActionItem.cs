namespace DB_Lib_EF.Models.MariaDB;

public class ActionItem
{
    public int Id { get; set; }

    public string Program { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string UpdateDateTime { get; set; } = null!;
}

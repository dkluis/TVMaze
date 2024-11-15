namespace DB_Lib_EF.Models.MariaDB;

public class Configuration
{
    public int Id { get; set; }

    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;
}

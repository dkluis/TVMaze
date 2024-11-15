namespace DB_Lib_EF.Entities;

public class Response
{
    public bool    WasSuccess     { get; set; }
    public string? Message        { get; set; }
    public object? ResponseObject { get; set; }
}
